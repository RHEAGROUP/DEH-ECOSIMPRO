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

    using DEHPCommon;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.MappingRules.Core;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel.Rows;

    using NLog;

    /// <summary>
    /// The <see cref="EcosimProElementToElementDefinitionRule"/> is a <see cref="IMappingRule"/> for the <see cref="MappingEngine"/>
    /// That takes a <see cref="List{T}"/> of <see cref="VariableRowViewModel"/> as input and outputs a E-TM-10-25 <see cref="ElementDefinition"/>
    /// </summary>
    public class EcosimProElementToElementDefinitionRule : MappingRule<List<VariableRowViewModel>, IEnumerable<ElementDefinition>>
    {
        /// <summary>
        /// The current class logger
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController = AppContainer.Container.Resolve<IHubController>();

        /// <summary>
        /// Gets the <see cref="idCorrespondences"/>
        /// </summary>
        private List<IdCorrespondence> idCorrespondences;

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
        public override IEnumerable<ElementDefinition> Transform(List<VariableRowViewModel> input)
        {
            try
            {
                this.idCorrespondences = AppContainer.Container.Resolve<IDstController>().IdCorrespondences;

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
                            if (input.FirstOrDefault(x => x.SelectedElementDefinition?.Name == this.dstElementName)
                                is { } existingElement)
                            {
                                variable.SelectedElementDefinition = existingElement.SelectedElementDefinition;
                            }
                            else
                            {
                                variable.SelectedElementDefinition = this.Bake<ElementDefinition>(x =>
                                {
                                    x.Name = this.dstElementName;
                                    x.ShortName = this.dstElementName;
                                    x.Owner = this.owner;
                                    x.Container = this.hubController.OpenIteration;
                                });
                            }
                            
                        }

                        this.AddsValueSetToTheSelectectedParameter(variable);
                        this.AddToExternalIdentifierMap(variable.SelectedElementDefinition.Iid, this.dstElementName);
                    }
                }

                return input.Select(x => x.SelectedElementDefinition);
            }
            catch (Exception exception)
            {
                Logger.Error(exception);
                ExceptionDispatchInfo.Capture(exception).Throw();
                return default;
            }
        }

        /// <summary>
        /// Updates the parameters overrides from the selected <see cref="ElementUsage"/>s
        /// </summary>
        /// <param name="variable">The current <see cref="VariableRowViewModel"/></param>
        private void UpdateValueSetsFromElementUsage(VariableRowViewModel variable)
        {
            foreach (var elementUsage in variable.SelectedElementUsages)
            {
                foreach (var parameter in elementUsage.ParameterOverride
                    .Where(x => x.ParameterType is CompoundParameterType parameterType 
                                && parameterType.Component.Count == 2 
                                && parameterType.Component.SingleOrDefault(c => c.ParameterType is DateTimeParameterType) != null))
                {
                    this.UpdateValueSet(variable, parameter);
                    this.AddToExternalIdentifierMap(parameter.Iid, this.dstParameterName);
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
                if (variable.SelectedParameterType is null)
                {
                    if (this.hubController.GetSiteDirectory().AvailableReferenceDataLibraries()
                        .SelectMany(x => x.QueryParameterTypesFromChainOfRdls())
                        .FirstOrDefault(x => x.Name == "TimeTaggedValue") is CompoundParameterType parameterType)
                    {
                        variable.SelectedParameterType = parameterType;
                    }
                    else
                    {
                        variable.SelectedParameterType = this.CreateCompoundParameterTypeForEcosimTimetaggedValues();
                    }
                }

                variable.SelectedParameter = this.Bake<Parameter>(x =>
                {
                    x.ParameterType = variable.SelectedParameterType;
                    x.Owner = this.owner;
                    x.Container = this.hubController.OpenIteration;
                });
                
                var valueSet = this.Bake<ParameterValueSet>(x =>
                {
                    x.Container = variable.SelectedParameter;
                });

                variable.SelectedParameter.ValueSet.Add(valueSet);
                variable.SelectedElementDefinition.Parameter.Add(variable.SelectedParameter);
            }
            
            this.UpdateValueSet(variable, variable.SelectedParameter);
        }

        /// <summary>
        /// Creates the <see cref="CompoundParameterType"/> for time tagged values
        /// </summary>
        /// <returns>A <see cref="CompoundParameterType"/></returns>
        private CompoundParameterType CreateCompoundParameterTypeForEcosimTimetaggedValues()
        {
            return this.Bake<CompoundParameterType>(x =>
            {
                x.ShortName = this.dstParameterName;
                x.Name = this.dstParameterName;
                x.Symbol = "ttv";

                x.Component.Add(this.Bake<ParameterTypeComponent>(
                    p =>
                    {
                        p.ShortName = "TimeStamp";
                        p.ParameterType = this.Bake<DateTimeParameterType>();
                    }));

                x.Component.Add(this.Bake<ParameterTypeComponent>(
                    p =>
                    {
                        p.ShortName = "Value";
                        p.ParameterType = this.Bake<SimpleQuantityKind>();
                    }));
            });
        }

        /// <summary>
        /// Initializes a new <see cref="Thing"/> of type <typeparamref name="TThing"/>
        /// </summary>
        /// <typeparam name="TThing">The <see cref="Type"/> from which the constructor is invoked</typeparam>
        /// <returns>A <typeparamref name="TThing"/> instance</returns>
        private TThing Bake<TThing>(Action<TThing> initialize = null) where TThing : Thing, new()
        {
            var tThingInstance = Activator.CreateInstance(typeof(TThing), Guid.NewGuid(), this.hubController.Session.Assembler.Cache, new Uri(this.hubController.Session.DataSourceUri)) as TThing;
            initialize?.Invoke(tThingInstance);
            return tThingInstance;
        }

        /// <summary>
        /// Updates the correct value set
        /// </summary>
        /// <param name="variable">The <see cref="VariableRowViewModel"/></param>
        /// <param name="parameter">The <see cref="Parameter"/></param>
        private void UpdateValueSet(VariableRowViewModel variable, ParameterBase parameter)
        {
            IValueSet valueSet;

            if (parameter.StateDependence != null && variable.SelectedActualFiniteState is { } actualFiniteState)
            {
                valueSet = parameter.ValueSets.Last(x => x.ActualState == actualFiniteState);
            }
            else
            {
                switch (parameter)
                {
                    case ParameterOverride parameterOverride:
                        valueSet = this.Bake<ParameterOverrideValueSet>();
                        parameterOverride.ValueSet.Add((ParameterOverrideValueSet)valueSet);
                        break;
                    case ParameterSubscription parameterSubscription:
                        valueSet = this.Bake<ParameterSubscriptionValueSet>();
                        parameterSubscription.ValueSet.Add((ParameterSubscriptionValueSet)valueSet);
                        break;
                    case Parameter parameterBase:
                        valueSet = this.Bake<ParameterValueSet>();
                        parameterBase.ValueSet.Add((ParameterValueSet)valueSet);
                        break;
                    default:
                        return;
                }
            }

            this.UpdateValueSet(variable, parameter, (ParameterValueSetBase)valueSet);
        }

        /// <summary>
        /// Updates the specified value set
        /// </summary>
        /// <param name="variable">The <see cref="VariableRowViewModel"/></param>
        /// <param name="parameter">The <see cref="Thing"/> <see cref="Parameter"/> or <see cref="ParameterOverride"/></param>
        /// <param name="valueSet">The <see cref="ParameterValueSetBase"/></param>
        private void UpdateValueSet(VariableRowViewModel variable, Thing parameter, ParameterValueSetBase valueSet)
        {
            valueSet.Computed = new ValueArray<string>(
                variable.SelectedValues.Select(
                    x => FormattableString.Invariant($"{x.Value}")));

            valueSet.ValueSwitch = ParameterSwitchKind.COMPUTED;

            this.AddToExternalIdentifierMap(parameter.Iid, this.dstParameterName);
        }

        /// <summary>
        /// Adds one correspondance to the <see cref="idCorrespondences"/>
        /// </summary>
        /// <param name="internalId">The thing that <see cref="externalId"/> corresponds to</param>
        /// <param name="externalId">The external thing that <see cref="internalId"/> corresponds to</param>
        private void AddToExternalIdentifierMap(Guid internalId, string externalId)
        {
            this.idCorrespondences.Add(this.Bake<IdCorrespondence>(x =>
            {
                x.ExternalId = externalId;
                x.InternalThing = internalId;
            }));
        }
    }
}
