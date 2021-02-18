﻿// --------------------------------------------------------------------------------------------------------------------
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
                .RequiredRdls.OfType<ModelReferenceDataLibrary>().First();
        
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
            if (this.hubController.OpenIteration.Element
                .FirstOrDefault(x => x.Name == this.dstElementName) is { } element)
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
        /// Updates the parameters overrides from the selected <see cref="ElementUsage"/>s
        /// </summary>
        /// <param name="variable">The current <see cref="VariableRowViewModel"/></param>
        private void UpdateValueSetsFromElementUsage(VariableRowViewModel variable)
        {
            var hasAtLeastOneUpdatedOverride = false;

            foreach (var elementUsage in variable.SelectedElementUsages)
            {
                if (variable.SelectedParameter is {} parameter 
                    && elementUsage.ParameterOverride
                        .FirstOrDefault(x => x.Parameter == parameter) is {} parameterOverride)
                {
                    this.UpdateValueSet(variable, parameterOverride);
                    this.AddToExternalIdentifierMap(parameterOverride.Iid, this.dstParameterName);
                    hasAtLeastOneUpdatedOverride = true;
                }

                if (hasAtLeastOneUpdatedOverride)
                {
                    this.AddToExternalIdentifierMap(elementUsage.Iid, this.dstElementName);
                }
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
                        x.ParameterType = variable.SelectedParameterType;
                        x.Owner = this.owner;
                        x.Container = variable.SelectedElementDefinition;

                        x.ValueSet.Add(this.Bake<ParameterValueSet>(set =>
                        {
                            set.Computed = new ValueArray<string>();
                            set.Formula = new ValueArray<string>(new[] { "-", "-" });
                            set.Manual = new ValueArray<string>(new[] { "-", "-" });
                            set.Reference = new ValueArray<string>(new[] { "-", "-" });
                            set.Published = new ValueArray<string>(new[] { "-", "-" });
                        }));
                    });

                    variable.SelectedElementDefinition.Parameter.Add(variable.SelectedParameter);
                }
            }
            
            this.UpdateValueSet(variable, variable.SelectedParameter);
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

            if (parameter.ParameterType is SampledFunctionParameterType sampledFunctionParameterType
                && sampledFunctionParameterType.DependentParameterType.SingleOrDefault()?.ParameterType is ScalarParameterType 
                && sampledFunctionParameterType.IndependentParameterType.SingleOrDefault()?.ParameterType is { } independantParameterType )
            {
                var values = new List<string>();

                if ((independantParameterType is TextParameterType
                    || independantParameterType is QuantityKind) && dependantParameterType is TextParameterType
                                                                         || ependantParameterType is QuantityKind)
                {)
                {
                    foreach (var value in variable.SelectedValues)
                    {
                        values.Add($"{variable.SelectedValues.IndexOf(value)}");
                        values.Add(FormattableString.Invariant($"{value.Value}"));
                    }
                }
                else
                {
                    foreach (var value in variable.SelectedValues)
                    {
                        values.Add($"{value.TimeDelta}");
                        values.Add(FormattableString.Invariant($"{value.Value}"));
                    }
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
