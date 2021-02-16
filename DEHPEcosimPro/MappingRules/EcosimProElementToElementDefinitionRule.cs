// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EcosimProElementToElementDefinitionRule.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2021 RHEA System S.A.
// 
//    Author: Sam Gerené, Alex Vorobiev, Alexander van Delft, Nathanael Smiechowski.
// 
//    This file is part of DEHPEcosimPro
// 
//    The DEHPEcosimPro is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 3 of the License, or (at your option) any later version.
// 
//    The DEHPEcosimPro is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
// 
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program; if not, write to the Free Software Foundation,
//    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DEHPEcosimPro.MappingRules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using System.Runtime.ExceptionServices;

    using Autofac;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using CDP4Dal.Operations;

    using DEHPCommon;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.MappingRules.Core;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel.Rows;

    using DevExpress.Xpf.Reports.UserDesigner.Native;

    using NLog;

    /// <summary>
    /// The <see cref="EcosimProElementToElementDefinitionRule"/> is a <see cref="IMappingRule"/> for the <see cref="MappingEngine"/>
    /// That takes a <see cref="List{T}"/> of <see cref="VariableRowViewModel"/> as input and outputs a E-TM-10-25 <see cref="ElementDefinition"/>
    /// </summary>
    public class EcosimProElementToElementDefinitionRule : MappingRule<List<VariableRowViewModel>, List<ElementDefinition>>
    {
        /// <summary>
        /// Gets the dependent parameter type name for one <see cref="SampledFunctionParameterType"/>
        /// </summary>
        private const string SampledFunctionParameterTypeValueMemberName = "Value";

        /// <summary>
        /// Gets the independent parameter type name for one <see cref="SampledFunctionParameterType"/>
        /// </summary>
        private const string SampledFunctionParameterTypeTimestampMemberName = "Timestamp";

        /// <summary>
        /// The current class logger
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController = AppContainer.Container.Resolve<IHubController>();

        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private IDstController dstController;

        /// <summary>
        /// Gets the <see cref="ModelReferenceDataLibrary"/> of the current <see cref="EngineeringModel"/>
        /// </summary>
        private ModelReferenceDataLibrary ReferenceDataLibrary =>
            this.hubController.OpenIteration.GetContainerOfType<EngineeringModel>()
                .RequiredRdls
                .OfType<ModelReferenceDataLibrary>().First();
        
        /// <summary>
        /// The current <see cref="DomainOfExpertise"/>
        /// </summary>
        private DomainOfExpertise owner;

        /// <summary>
        /// Holds the current processing <see cref="VariableRowViewModel"/> element name
        /// </summary>
        private string dstElementName;

        /// <summary>
        /// Holds the current processing <see cref="VariableRowViewModel"/> parameter name
        /// </summary>
        private string dstParameterName;
        
        /// <summary>
        /// Transforms a <see cref="List{T}"/> of <see cref="VariableRowViewModel"/> into an <see cref="ElementDefinition"/>
        /// </summary>
        /// <param name="input">The <see cref="List{T}"/> of <see cref="VariableRowViewModel"/> to transform</param>
        /// <returns>An <see cref="ElementDefinition"/></returns>
        public override List<ElementDefinition> Transform(List<VariableRowViewModel> input)
        {
            try
            {
                this.dstController = AppContainer.Container.Resolve<IDstController>();

                this.owner = this.hubController.CurrentDomainOfExpertise;

                foreach (var variable in input.ToList())
                {
                    this.dstElementName = variable.ElementName;
                    this.dstParameterName = variable.ParameterName;

                    if (variable.SelectedElementUsages.Any())
                    {
                        this.UpdateValueSetsFromElementUsage(variable);
                    }
                    else
                    {
                        if (variable.SelectedElementDefinition is null)
                        {
                            var existingElement = input.FirstOrDefault(x => x.SelectedElementDefinition?.Name == this.dstElementName)?.SelectedElementDefinition;

                            variable.SelectedElementDefinition = existingElement ?? this.CreateElementDefinition();
                        }

                        this.AddsValueSetToTheSelectectedParameter(variable);

                        this.AddToExternalIdentifierMap(variable.SelectedElementDefinition.Iid, this.dstElementName);
                    }
                }

                return input.Select(x => x.SelectedElementDefinition).ToList();
            }
            catch (Exception exception)
            {
                Logger.Error(exception);
                ExceptionDispatchInfo.Capture(exception).Throw();
                return default;
            }
        }

        /// <summary>
        /// Creates an <see cref="ElementDefinition"/> if it does not exist yet
        /// </summary>
        /// <returns>An <see cref="ElementDefinition"/></returns>
        private ElementDefinition CreateElementDefinition()
        {
            if (this.hubController.OpenIteration.Element.FirstOrDefault(x => x.Name == this.dstElementName) is { } element)
            {
                return element;
            }

            return this.Bake<ElementDefinition>(x =>
            {
                x.Name = this.dstElementName;
                x.ShortName = this.dstElementName;
                x.Owner = this.owner;
                x.Container = this.hubController.OpenIteration;
            });
        }

        /// <summary>
        /// Gets the possible usable scales for the VALUE
        /// </summary>
        private List<MeasurementScale> GetMeasurementScales()
        {
            return this.ReferenceDataLibrary.QueryMeasurementScalesFromChainOfRdls().Where(x => x.NumberSet == NumberSetKind.REAL_NUMBER_SET).ToList();
        }

        /// <summary>
        /// Updates the parameters overrides from the selected <see cref="ElementUsage"/>s
        /// </summary>
        /// <param name="variable">The current <see cref="VariableRowViewModel"/></param>
        private void UpdateValueSetsFromElementUsage(VariableRowViewModel variable)
        {
            foreach (var elementUsage in variable.SelectedElementUsages)
            {
                ParameterOverride parameterOverride;

                if (variable.SelectedParameter is {} parameter)
                {
                    if (elementUsage.ParameterOverride.FirstOrDefault(x => x.Parameter == parameter) is {} existingOverride)
                    {
                        parameterOverride = existingOverride;
                    }
                    else
                    {
                        parameterOverride = this.Bake<ParameterOverride>(x =>
                        {
                            x.Parameter = parameter;
                            x.ParameterType = parameter.ParameterType;
                            x.StateDependence = parameter.StateDependence;
                            x.IsOptionDependent = parameter.IsOptionDependent;
                            x.Owner = this.owner;
                        });
                    }

                    elementUsage.ParameterOverride.Add(parameterOverride);
                }
                else
                {
                    parameterOverride = elementUsage.ParameterOverride.FirstOrDefault(x => x.ParameterType.Name == this.dstParameterName);

                    if (parameterOverride is null && 
                        elementUsage.ElementDefinition.Parameter.FirstOrDefault(
                            x => x.ParameterType.Name == this.dstParameterName) is { } parameterToOverride)
                    {
                        parameterOverride = this.Bake<ParameterOverride>(x =>
                        {
                            x.Parameter = parameterToOverride;
                            x.Owner = this.owner;
                            x.Container = elementUsage;
                        });
                    }
                }

                if (parameterOverride != null)
                {
                    this.UpdateValueSet(variable, parameterOverride);
                    this.AddToExternalIdentifierMap(parameterOverride.Iid, this.dstParameterName);
                }

                this.AddToExternalIdentifierMap(elementUsage.Iid, this.dstElementName);
            }
        }

        /// <summary>
        /// Adds the selected values to the corresponding valueset of the destination parameter
        /// </summary>
        /// <param name="variable">The input variable</param>
        private void AddsValueSetToTheSelectectedParameter(VariableRowViewModel variable)
        {
            if (variable.SelectedParameter is null)
            {
                if (variable.SelectedElementDefinition.Parameter.FirstOrDefault(x => x.ParameterType.Name == this.dstParameterName) 
                    is {} parameter)
                {
                    variable.SelectedParameter = parameter;
                }

                else
                {
                    variable.SelectedParameter = this.Bake<Parameter>(x =>
                    {
                        x.ParameterType = this.GetSampledFunctionParameterType();
                        x.Owner = this.owner;
                        x.Container = variable.SelectedElementDefinition;

                        x.ValueSet.Add(this.Bake<ParameterValueSet>(set =>
                        {
                            set.Computed = new ValueArray<string>();
                            set.Formula = new ValueArray<string>(new[] { "-" });
                            set.Manual = new ValueArray<string>(new[] { "-" });
                            set.Reference = new ValueArray<string>(new[] { "-" });
                            set.Published = new ValueArray<string>(new[] { "-" });
                        }));
                    });

                    variable.SelectedElementDefinition.Parameter.Add(variable.SelectedParameter);
                }
            }
            
            this.UpdateValueSet(variable, variable.SelectedParameter);
        }

        /// <summary>
        /// Gets the existing or newly created <see cref="SampledFunctionParameterType"/>
        /// </summary>
        /// <returns>A <see cref="ParameterType"/></returns>
        private ParameterType GetSampledFunctionParameterType()
        {
            return this.ReferenceDataLibrary.ParameterType.FirstOrDefault(
                    x => x.Name == this.dstParameterName && x is SampledFunctionParameterType) ??
                this.CreateSampledFunctionParameterType();
        }

        /// <summary>
        /// Create a <see cref="SampledFunctionParameterType"/>
        /// </summary>
        /// <returns>A <see cref="SampledFunctionParameterType"/></returns>
        private SampledFunctionParameterType CreateSampledFunctionParameterType()
        {
            var parameterType = this.Bake<SampledFunctionParameterType>(x =>
            {
                x.Name = this.dstParameterName;
                
                x.ShortName = this.dstParameterName
                    .Replace('.', '_')
                    .Replace("'", "");

                x.Iid = Guid.NewGuid();
                x.Container = this.ReferenceDataLibrary;

                x.InterpolationPeriod = new ValueArray<string>(
                    new List<string>() { "0", string.Empty });

                x.Symbol = this.dstParameterName;
            });

            var timeAssignment = this.Bake<IndependentParameterTypeAssignment>(x =>
            {
                x.ParameterType = this.CreateParameterType<DateTimeParameterType>(SampledFunctionParameterTypeTimestampMemberName);
                x.Iid = Guid.NewGuid();
            });

            parameterType.IndependentParameterType.Add(timeAssignment);

            var valueAssignement = this.Bake<DependentParameterTypeAssignment>(x =>
            {
                x.MeasurementScale = this.GetMeasurementScales().FirstOrDefault();
                x.Iid = Guid.NewGuid();
                x.ParameterType = this.CreateParameterType<SimpleQuantityKind>(SampledFunctionParameterTypeValueMemberName, this.GetMeasurementScales());
            });

            parameterType.DependentParameterType.Add(valueAssignement);

            var clone = this.ReferenceDataLibrary.Clone(false);
            var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(clone), clone);
            clone.ParameterType.Add(parameterType);
            transaction.CreateOrUpdate(clone);

            transaction.Create(timeAssignment);
            transaction.Create(valueAssignement);
            transaction.Create(parameterType);

            this.hubController.Write(transaction);
            this.ReferenceDataLibrary.ParameterType.Add(parameterType);

            return parameterType;
        }
        
        /// <summary>
        /// Gets or creates the parameter type used in the 
        /// </summary>
        /// <typeparam name="TParameter">The type of <see cref="ParameterType"/> to return</typeparam>
        /// <param name="name">The name of the parameterType</param>
        /// <param name="measurementScales">A optionnal list of possible scales</param>
        /// <returns>A <see cref="TParameter"/></returns>
        private TParameter CreateParameterType<TParameter>(string name, List<MeasurementScale> measurementScales = default) where TParameter : ParameterType, new()
        {
            var parameterType = this.ReferenceDataLibrary.AggregatedReferenceDataLibrary
                .Select(x => x.ParameterType
                    .OfType<TParameter>()
                    .FirstOrDefault(p => p.Name == name))
                .FirstOrDefault();

            if (parameterType is null)
            {
                parameterType = this.Bake<TParameter>(x =>
                {
                    x.Iid = Guid.NewGuid();
                    x.Name = name;
                    x.ShortName = name;
                    x.Symbol = string.Concat(name.Take(3));
                    x.Container = this.ReferenceDataLibrary;
                });

                if (parameterType is QuantityKind quantityKind && measurementScales?.Any() is true)
                {
                    quantityKind.PossibleScale = measurementScales;
                    quantityKind.DefaultScale = measurementScales.First();
                }

                var clone = this.ReferenceDataLibrary.Clone(false);
                var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(clone), clone);
                clone.ParameterType.Add(parameterType);
                transaction.CreateOrUpdate(clone);
                transaction.CreateOrUpdate(parameterType);

                this.hubController.Write(transaction);
                this.ReferenceDataLibrary.ParameterType.Add(parameterType);
            }

            return parameterType;
        }

        /// <summary>
        /// Initializes a new <see cref="Thing"/> of type <typeparamref name="TThing"/>
        /// </summary>
        /// <typeparam name="TThing">The <see cref="Type"/> from which the constructor is invoked</typeparam>
        /// <returns>A <typeparamref name="TThing"/> instance</returns>
        private TThing Bake<TThing>(Action<TThing> initialize = null) where TThing : Thing, new()
        {
            var tThingInstance = Activator.CreateInstance(typeof(TThing), Guid.Empty, this.hubController.Session.Assembler.Cache, new Uri(this.hubController.Session.DataSourceUri)) as TThing;
            initialize?.Invoke(tThingInstance);
            return tThingInstance;
        }

        /// <summary>
        /// Updates the specified value set
        /// </summary>
        /// <param name="variable">The <see cref="VariableRowViewModel"/></param>
        /// <param name="parameter">The <see cref="Thing"/> <see cref="Parameter"/> or <see cref="ParameterOverride"/></param>
        private void UpdateValueSet(VariableRowViewModel variable, ParameterBase parameter)
        {
            var valueSet = (ParameterValueSetBase)parameter.QueryParameterBaseValueSet(variable.SelectedOption, variable.SelectedActualFiniteState);

            if (parameter.ParameterType is SampledFunctionParameterType)
            {
                var values = new List<string>();

                foreach (var value in variable.SelectedValues)
                {
                    values.Add($"{value.TimeStamp:s}");
                    values.Add(FormattableString.Invariant($"{value.Value}"));
                }

                valueSet.Computed = new ValueArray<string>(values);
            }
            else
            {
                valueSet.Computed = new ValueArray<string>(new[] { FormattableString.Invariant($"{variable.SelectedValues[0].Value}") });
            }

            valueSet.ValueSwitch = ParameterSwitchKind.COMPUTED;

            this.AddToExternalIdentifierMap(parameter.Iid, this.dstParameterName);
        }

        /// <summary>
        /// Adds one correspondance to the <see cref="IHubController.IdCorrespondences"/>
        /// </summary>
        /// <param name="internalId">The thing that <see cref="externalId"/> corresponds to</param>
        /// <param name="externalId">The external thing that <see cref="internalId"/> corresponds to</param>
        private void AddToExternalIdentifierMap(Guid internalId, string externalId)
            => this.dstController.AddToExternalIdentifierMap(internalId, externalId);
    }
}
