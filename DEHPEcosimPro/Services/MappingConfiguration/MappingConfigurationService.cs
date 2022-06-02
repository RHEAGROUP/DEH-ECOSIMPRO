// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappingConfigurationService.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Services.MappingConfiguration
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.Extensions;
    using CDP4Common.SiteDirectoryData;

    using CDP4Dal.Operations;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel.Rows;

    using Newtonsoft.Json;

    using NLog;

    /// <summary>
    /// The <see cref="MappingConfigurationService"/> takes care of handling all operation
    /// related to saving and loading configured mapping.
    /// </summary>
    public class MappingConfigurationService : IMappingConfigurationService
    {
        /// <summary>
        /// Gets the current class logger
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        
        /// <summary>
        /// The <see cref="IStatusBarControlViewModel"/>
        /// </summary>
        private readonly IStatusBarControlViewModel statusBar;
        
        /// <summary>
        /// Backing field for <see cref="ExternalIdentifierMap"/>
        /// </summary>
        private ExternalIdentifierMap externalIdentifierMap;

        /// <summary>
        /// Get a value indicating wheter the current <see cref="ExternalIdentifierMap" /> is the default one
        /// </summary>
        public bool IsTheCurrentIdentifierMapTemporary => this.ExternalIdentifierMap.Iid == Guid.Empty
                                                          && string.IsNullOrWhiteSpace(this.ExternalIdentifierMap.Name);

        /// <summary>
        /// Gets or sets the <see cref="ExternalIdentifierMap"/>
        /// </summary>
        public ExternalIdentifierMap ExternalIdentifierMap
        {
            get => this.externalIdentifierMap;
            set
            {
                this.externalIdentifierMap = value;
                this.ParseIdCorrespondence();
            }
        }
        
        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The collection of id correspondence as tuple
        /// (<see cref="Guid"/> InternalId, <see cref="ExternalIdentifier"/> externalIdentifier, <see cref="Guid"/> Iid)
        /// including the deserialized external identifier
        /// </summary>
        private readonly List<(Guid InternalId, ExternalIdentifier ExternalIdentifier, Guid Iid)> correspondences 
            = new List<(Guid InternalId, ExternalIdentifier ExternalIdentifier, Guid Iid)>();
        
        /// <summary>
        /// Initializes a new <see cref="MappingConfigurationService"/>
        /// </summary>
        /// <param name="statusBarControl">The <see cref="IStatusBarControlViewModel"/></param>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        public MappingConfigurationService(IStatusBarControlViewModel statusBarControl, IHubController hubController)
        {
            this.statusBar = statusBarControl;
            this.hubController = hubController;

            this.ExternalIdentifierMap = new ExternalIdentifierMap();
        }

        /// <summary>
        /// Selects the values as defined in the <see cref="ExternalIdentifierMap"/> for the mapping
        /// </summary>
        public void SelectValues(IEnumerable<VariableRowViewModel> variableRowViewModels)
        {
            foreach (var rowViewModel in variableRowViewModels)
            {
                var timeTaggedValueRowViewModels = this.correspondences
                    .Where(x => 
                        x.ExternalIdentifier.Identifier.Equals(rowViewModel.Reference.NodeId.Identifier) 
                        && x.ExternalIdentifier.ValueIndex is { })
                    .DistinctBy(x => x.ExternalIdentifier.ValueIndex)
                    .Select(x => rowViewModel.Values.FirstOrDefault(
                        v => Math.Abs(v.TimeStep - x.ExternalIdentifier.ValueIndex.GetValueOrDefault()) <= 0 ))
                    .Where(x => x is {})
                    .ToList();

                if (!timeTaggedValueRowViewModels.Any())
                {
                    continue;
                }

                rowViewModel.SelectedValues.Clear();
                rowViewModel.SelectedValues.AddRange(timeTaggedValueRowViewModels);
            }

            this.statusBar.Append($"Loading of the selected values from saved configuration done!");
        }

        /// <summary>
        /// Parses the <see cref="ExternalIdentifierMap"/> correspondences and adds it to the <see cref="correspondences"/> collection
        /// </summary>
        private void ParseIdCorrespondence()
        {
            this.correspondences.Clear();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            this.correspondences.AddRange(this.ExternalIdentifierMap.Correspondence.Select(x =>
            (
                x.InternalThing, JsonConvert.DeserializeObject<ExternalIdentifier>(x.ExternalId ?? string.Empty), x.Iid
            )));

            stopwatch.Stop();
            this.logger.Debug($"{this.correspondences.Count} ExternalIdentifiers deserialized in {stopwatch.ElapsedMilliseconds} ms");
        }

        /// <summary>
        /// Loads the mapping configuration and generates the map result respectively
        /// </summary> 
        /// <param name="variables">The collection of <see cref="VariableRowViewModel"/></param>
        /// <returns>A collection of <see cref="VariableRowViewModel"/></returns>
        public List<MappedElementDefinitionRowViewModel> LoadMappingFromHubToDst(IList<VariableRowViewModel> variables)
            => this.LoadMapping(this.MapElementsFromTheExternalIdentifierMapToDst, variables);

        /// <summary>
        /// Loads the mapping configuration and generates the map result respectively
        /// </summary>
        /// <param name="variables">The collection of <see cref="VariableRowViewModel"/></param>
        /// <returns>A collection of <see cref="VariableRowViewModel"/></returns>
        public List<VariableRowViewModel> LoadMappingFromDstToHub(IList<VariableRowViewModel> variables)
            => this.LoadMapping(this.MapElementsFromTheExternalIdentifierMapToHub, variables);

        /// <summary>
        /// Calls the specify load mapping function <param name="loadMappingFunction"></param>
        /// </summary>
        /// <typeparam name="TViewModel">The type of row view model to return depending on the mapping direction</typeparam>
        /// <param name="loadMappingFunction">The specific load mapping <see cref="Func{TInput,TResult}"/></param>
        /// <param name="variables">The collection of <see cref="VariableRowViewModel"/></param>
        /// <returns>A collection of <typeparamref name="TViewModel"/></returns>
        private List<TViewModel> LoadMapping<TViewModel>(Func<IList<VariableRowViewModel>, List<TViewModel>> loadMappingFunction, IList<VariableRowViewModel> variables)
        {
            this.logger.Debug($"Loading the mapping configuration in progress");

            if (this.ExternalIdentifierMap != null && this.ExternalIdentifierMap.Iid != Guid.Empty
                                                   && this.ExternalIdentifierMap.Correspondence.Any())
            {
                return loadMappingFunction(variables);
            }

            this.logger.Debug($"The mapping configuration doesn't contain any mapping", StatusBarMessageSeverity.Warning);
            return default;
        }

        /// <summary>
        /// Maps the <see cref="VariableRowViewModel"/>s defined in the <see cref="ExternalIdentifierMap"/>
        /// </summary>
        /// <param name="variableRowViewModels">The collection of <see cref="VariableRowViewModel"/></param>
        /// <returns>A collection of <see cref="VariableRowViewModel"/></returns>
        private List<MappedElementDefinitionRowViewModel> MapElementsFromTheExternalIdentifierMapToDst(IList<VariableRowViewModel> variableRowViewModels)
        {
            var mappedVariables = new List<MappedElementDefinitionRowViewModel>();

            foreach (var idCorrespondences in
                this.correspondences.Where(x => x.ExternalIdentifier.MappingDirection == MappingDirection.FromHubToDst)
                    .GroupBy(x => x.ExternalIdentifier.Identifier))
            {
                if (variableRowViewModels.FirstOrDefault(rowViewModel =>
                    rowViewModel.Reference.NodeId.Identifier.Equals(idCorrespondences.Key)) is not { } element)
                {
                    continue;
                }
                
                foreach (var (internalId, externalIdentifier, idCorrespondenceId) in idCorrespondences)
                {
                    if (!this.hubController.GetThingById(internalId, this.hubController.OpenIteration, out ParameterValueSetBase valueSet))
                    {
                        continue;
                    }

                    if (!int.TryParse($"{externalIdentifier.ValueIndex}", out var index))
                    {
                        continue;
                    }

                    var mappedElement = new MappedElementDefinitionRowViewModel(valueSet, index, externalIdentifier.ParameterSwitchKind)
                    {
                        SelectedVariable = element
                    };

                    mappedVariables.Add(mappedElement);
                }
            }

            return mappedVariables;
        }
        
        /// <summary>
        /// Maps the <see cref="VariableRowViewModel"/>s defined in the <see cref="ExternalIdentifierMap"/>
        /// </summary>
        /// <param name="variableRowViewModels">The collection of <see cref="VariableRowViewModel"/></param>
        /// <returns>A collection of <see cref="VariableRowViewModel"/></returns>
        private List<VariableRowViewModel> MapElementsFromTheExternalIdentifierMapToHub(IList<VariableRowViewModel> variableRowViewModels)
        {
            var mappedVariables = new List<VariableRowViewModel>();

            foreach (var idCorrespondences in 
                this.correspondences.Where(x => x.ExternalIdentifier.MappingDirection == MappingDirection.FromDstToHub)
                    .GroupBy(x => x.ExternalIdentifier.Identifier))
            {
                if (variableRowViewModels.FirstOrDefault(rowViewModel =>
                    rowViewModel.Reference.NodeId.Identifier.Equals(idCorrespondences.Key)) is not { } element)
                {
                    continue;
                }

                this.LoadsCorrespondances(element, idCorrespondences);
                element.MappingConfigurations.AddRange(this.ExternalIdentifierMap.Correspondence.Where(x => idCorrespondences.Any(c => c.Iid == x.Iid)).ToList());
                
                if (element.SelectedParameter is { ParameterType: QuantityKind quantityKind } selectedParameter)
                {
                    var scaleIid = idCorrespondences.FirstOrDefault(x =>
                        quantityKind.AllPossibleScale.Any(scale => scale.Iid == x.InternalId)).InternalId;

                    element.SelectedScale = quantityKind.AllPossibleScale.FirstOrDefault(x => x.Iid == scaleIid) ?? selectedParameter.Scale;
                }

                mappedVariables.Add(element);
            }
            
            return mappedVariables;
        }

        /// <summary>
        ///  Loads referenced <see cref="Thing"/>s
        /// </summary>
        /// <param name="element">The <see cref="VariableRowViewModel"/></param>
        /// <param name="idCorrespondences">The collection of <see cref="IdCorrespondence"/></param>
        private void LoadsCorrespondances(VariableRowViewModel element, IEnumerable<(Guid InternalId, ExternalIdentifier ExternalIdentifier, Guid Iid)> idCorrespondences)
        {
            foreach (var idCorrespondence in idCorrespondences)
            {
                if (!this.hubController.GetThingById(idCorrespondence.InternalId, this.hubController.OpenIteration, out Thing thing))
                {
                    continue;
                }

                Action action = thing switch
                {
                    ElementDefinition elementDefinition => () => element.SelectedElementDefinition = elementDefinition.Clone(true),
                    ElementUsage elementUsage => () => element.SelectedElementUsages.Add(elementUsage.Clone(true)),
                    Parameter parameter => () => element.SelectedParameter = parameter.Clone(true),
                    Option option => () => element.SelectedOption = option.Clone(false),
                    ActualFiniteState state => () => element.SelectedActualFiniteState = state.Clone(false),
                    _ => null
                };

                if (element.SelectedParameter is { } selectedParameter)
                {
                    Application.Current.Dispatcher.Invoke(() => element.SelectedParameterType = selectedParameter.ParameterType);
                }

                action?.Invoke();
            }
        }
        
        /// <summary>
        /// Updates the configured mapping, registering the <see cref="ExternalIdentifierMap"/> and its <see cref="IdCorrespondence"/>
        /// to a <see name="IThingTransaction"/>
        /// </summary>
        /// <param name="transaction">The <see cref="IThingTransaction"/></param>
        /// <param name="iterationClone">The <see cref="Iteration"/> clone</param>
        public void PersistExternalIdentifierMap(IThingTransaction transaction, Iteration iterationClone)
        {
            if (this.IsTheCurrentIdentifierMapTemporary)
            {
                this.logger.Error($"The current mapping with {this.ExternalIdentifierMap.Correspondence.Count} correspondences will not be saved as it is temporary");
                return;
            }

            if (this.ExternalIdentifierMap.Iid == Guid.Empty)
            {
                this.ExternalIdentifierMap = this.ExternalIdentifierMap.Clone(true);
                this.ExternalIdentifierMap.Iid = Guid.NewGuid();
                iterationClone.ExternalIdentifierMap.Add(this.ExternalIdentifierMap);
            }

            foreach (var correspondence in this.ExternalIdentifierMap.Correspondence)
            {
                if (correspondence.Iid == Guid.Empty)
                {
                    correspondence.Iid = Guid.NewGuid();
                    transaction.Create(correspondence);
                }
                else
                {
                    transaction.CreateOrUpdate(correspondence);
                }
            }

            transaction.CreateOrUpdate(this.ExternalIdentifierMap);

            this.statusBar.Append("Mapping configuration processed");
        }

        /// <summary>
        /// Creates the <see cref="ExternalIdentifierMap" />
        /// </summary>
        /// <param name="newName">The model name to use for creating the new <see cref="ExternalIdentifierMap" /></param>
        /// <param name="addTheTemporyMapping">a value indicating whether the current temporary should be transfered to new one</param>
        /// <returns>A newly created <see cref="ExternalIdentifierMap" /></returns>
        public ExternalIdentifierMap CreateExternalIdentifierMap(string newName, bool addTheTemporyMapping)
        {
            var newExternalIdentifierMap = new ExternalIdentifierMap
            {
                Name = newName,
                ExternalToolName = DstController.ThisToolName,
                ExternalModelName = newName,
                Owner = this.hubController.CurrentDomainOfExpertise
            };

            if (addTheTemporyMapping)
            {
                newExternalIdentifierMap.Correspondence.AddRange(this.ExternalIdentifierMap.Correspondence);
            }

            return newExternalIdentifierMap;
        }

        /// <summary>
        /// Adds one correspondance to the <see cref="ExternalIdentifierMap"/>
        /// </summary>
        /// <param name="internalId">The thing that <see cref="externalId"/> corresponds to</param>
        /// <param name="valueIndex">The value index</param>
        /// <param name="externalId">The external thing that <see cref="internalId"/> corresponds to</param>
        /// <param name="mappingDirection">The <see cref="MappingDirection"/> the mapping belongs</param>
        public void AddToExternalIdentifierMap(Guid internalId, double valueIndex, object externalId, MappingDirection mappingDirection)
        {
            this.AddToExternalIdentifierMap(internalId, new ExternalIdentifier
            {
                Identifier = externalId, MappingDirection = mappingDirection, ValueIndex = valueIndex
            });
        }

        /// <summary>
        /// Adds one correspondance to the <see cref="ExternalIdentifierMap"/>
        /// </summary>
        /// <param name="mappedElement">The <see cref="mappedElement"/></param>
        public void AddToExternalIdentifierMap(MappedElementDefinitionRowViewModel mappedElement)
        {
            var (index, switchKind) = mappedElement.SelectedValue.GetValueIndexAndParameterSwitchKind();

            this.AddToExternalIdentifierMap(((Thing)mappedElement.SelectedValue.Container).Iid, new ExternalIdentifier
            {
                Identifier = mappedElement.SelectedVariable.Reference.NodeId.Identifier,
                MappingDirection = MappingDirection.FromHubToDst, ValueIndex = index, ParameterSwitchKind = switchKind
            });
        }

        /// <summary>
        /// Adds one correspondance to the <see cref="ExternalIdentifierMap"/>
        /// </summary>
        /// <param name="internalId">The thing that <see cref="externalId"/> corresponds to</param>
        /// <param name="externalId">The external thing that <see cref="internalId"/> corresponds to</param>
        /// <param name="mappingDirection">The <see cref="MappingDirection"/> the mapping belongs</param>
        public void AddToExternalIdentifierMap(Guid internalId, object externalId, MappingDirection mappingDirection)
        {
            this.AddToExternalIdentifierMap(internalId, new ExternalIdentifier
            {
                Identifier = externalId, MappingDirection = mappingDirection
            });
        }

        /// <summary>
        /// Adds as many correspondence as <paramref name="parameterVariable"/> values
        /// </summary>
        /// <param name="parameterVariable">The <see cref="Dictionary{T,T}"/> of mapped <see cref="ParameterOrOverrideBase"/> / <see cref="VariableRowViewModel"/></param>
        public void AddToExternalIdentifierMap(Dictionary<ParameterOrOverrideBase, VariableRowViewModel> parameterVariable)
        {
            var oldCount = this.ExternalIdentifierMap.Correspondence.Count;

            foreach (var variable in parameterVariable)
            {
                foreach (var timeTaggedValue in variable.Value.SelectedValues)
                {
                    this.AddToExternalIdentifierMap(Guid.Empty, timeTaggedValue.TimeStep, variable.Value.Reference.NodeId.Identifier, MappingDirection.FromDstToHub);
                }

                this.AddToExternalIdentifierMap(variable.Key.Iid, new ExternalIdentifier() { Identifier = variable.Value.Reference.NodeId.Identifier });

                this.AddToExternalIdentifierMap(variable.Value);

                if (variable.Key.GetContainerOfType<ElementUsage>() is { } elementUsage)
                {
                    this.AddToExternalIdentifierMap(elementUsage.Iid, new ExternalIdentifier() { Identifier = variable.Value.Reference.NodeId.Identifier });
                }

                else if (variable.Key.GetContainerOfType<ElementDefinition>() is { } elementDefinition)
                {
                    this.AddToExternalIdentifierMap(elementDefinition.Iid, new ExternalIdentifier() { Identifier = variable.Value.Reference.NodeId.Identifier });
                }
            }

            this.logger.Debug($"{this.ExternalIdentifierMap.Correspondence.Count-oldCount} correspondences added to the ExternalIdentifierMap");
        }

        /// <summary>
        /// Adds as many correspondence as <paramref name="variable" /> values
        /// </summary>
        /// <param name="variable">The <see cref="VariableRowViewModel" />
        /// </param>
        private void AddToExternalIdentifierMap(VariableRowViewModel variable)
        {
            if (variable.SelectedActualFiniteState != null)
            {
                this.AddToExternalIdentifierMap(variable.SelectedActualFiniteState.Iid, new ExternalIdentifier
                {
                    Identifier = variable.Reference.NodeId.Identifier
                });
            }

            if (variable.SelectedOption != null)
            {
                this.AddToExternalIdentifierMap(variable.SelectedOption.Iid, new ExternalIdentifier
                {
                    Identifier = variable.Reference.NodeId.Identifier
                });
            }

            if (variable.SelectedScale != null)
            {
                this.AddToExternalIdentifierMap(variable.SelectedScale.Iid, new ExternalIdentifier
                {
                    Identifier = variable.Reference.NodeId.Identifier
                });
            }
        }

        /// <summary>
        /// Adds one correspondence to the <see cref="ExternalIdentifierMap"/>
        /// </summary>
        /// <param name="internalId">The thing that <paramref name="externalIdentifier"/> corresponds to</param>
        /// <param name="externalIdentifier">The external thing that <see cref="internalId"/> corresponds to</param>
        public void AddToExternalIdentifierMap(Guid internalId, ExternalIdentifier externalIdentifier)
        {
            var (_, _, correspondenceIid) = this.correspondences.FirstOrDefault(x =>
                x.InternalId == internalId
                && externalIdentifier.Identifier.Equals(x.ExternalIdentifier.Identifier)
                && externalIdentifier.MappingDirection == x.ExternalIdentifier.MappingDirection);

            if (correspondenceIid != Guid.Empty
                && this.ExternalIdentifierMap.Correspondence.FirstOrDefault(x => x.Iid == correspondenceIid)
                is { } correspondence)
            {
                correspondence.InternalThing = internalId;
                correspondence.ExternalId = JsonConvert.SerializeObject(externalIdentifier);
                return;
            }

            this.ExternalIdentifierMap.Correspondence.Add(new IdCorrespondence()
            {
                ExternalId = JsonConvert.SerializeObject(externalIdentifier),
                InternalThing = internalId
            });
        }

        /// <summary>
        /// Refreshes the <see cref="ExternalIdentifierMap"/> usually done after a session write
        /// </summary>
        public void RefreshExternalIdentifierMap()
        {
            if (this.IsTheCurrentIdentifierMapTemporary)
            {
                return;
            }

            this.hubController.GetThingById(this.ExternalIdentifierMap.Iid, this.hubController.OpenIteration, out ExternalIdentifierMap map);
            this.ExternalIdentifierMap = map.Clone(true);
        }
    }
}
