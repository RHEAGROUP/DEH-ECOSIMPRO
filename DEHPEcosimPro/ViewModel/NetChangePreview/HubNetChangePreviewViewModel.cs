// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubNetChangePreviewViewModel.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.ViewModel.NetChangePreview
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;

    using CDP4Dal;
    
    using DEHPCommon.Events;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.ObjectBrowserTreeSelectorService;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.NetChangePreview;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Events;
    using DEHPEcosimPro.ViewModel.Interfaces;

    using ReactiveUI;

    public class HubNetChangePreviewViewModel : NetChangePreviewViewModel, IHubNetChangePreviewViewModel
    {
        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:DEHPCommon.UserInterfaces.ViewModels.ObjectBrowserViewModel" /> class.
        /// </summary>
        /// <param name="hubController">The <see cref="T:DEHPCommon.HubController.Interfaces.IHubController" /></param>
        /// <param name="objectBrowserTreeSelectorService">The <see cref="T:DEHPCommon.Services.ObjectBrowserTreeSelectorService.IObjectBrowserTreeSelectorService" /></param>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        public HubNetChangePreviewViewModel(IHubController hubController, IObjectBrowserTreeSelectorService objectBrowserTreeSelectorService, IDstController dstController) : base(hubController, objectBrowserTreeSelectorService)
        {
            this.dstController = dstController;

            CDPMessageBus.Current.Listen<UpdateHubPreviewBasedOnSelectionEvent>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(this.UpdateTreeBasedOnSelection);
        }

        /// <summary>
        /// Updates the tree and filter changed things based on a selection
        /// </summary>
        /// <param name="eventArguments">The <see cref="UpdatePreviewBasedOnSelectionBaseEvent{TThing,TTarget}"/></param>
        private void UpdateTreeBasedOnSelection(UpdateHubPreviewBasedOnSelectionEvent eventArguments)
        {
            if (this.dstController.DstMapResult.Any())
            {
                this.IsBusy = true;

                if (!eventArguments.Selection.Any() && this.IsDirty)
                {
                    this.ComputeValuesWrapper();
                }

                else if (eventArguments.Selection.Any())
                {
                    this.RestoreThings();

                    foreach (var variable in eventArguments.Selection)
                    {
                        var parameters = this.dstController.ParameterNodeIds
                            .Where(v => v.Value.Equals(variable.Reference.NodeId.Identifier))
                            .Select(x => x.Key);

                        foreach (var parameterOrOverrideBase in parameters)
                        {
                            var parameterRows = this.GetRows(parameterOrOverrideBase).ToList();

                            if (!parameterRows.Any())
                            {
                                var oldElement = this.ThingsAtPreviousStateBix.FirstOrDefault(t => t.Iid == parameterOrOverrideBase.Container.Iid);

                                var elementToUpdate = this.Things.SelectMany(x => x.ContainedRows.OfType<ElementDefinitionRowViewModel>())
                                    .FirstOrDefault(x => x.Thing.Iid == parameterOrOverrideBase.Container.Iid);

                                this.UpdateRow(parameterOrOverrideBase, (ElementDefinition)oldElement, elementToUpdate);

                                this.IsDirty = true;
                            }

                            foreach (var parameterRow in parameterRows)
                            {
                                var oldElement = this.ThingsAtPreviousStateBix.FirstOrDefault(t => t.Iid == parameterRow.ContainerViewModel.Thing.Iid);
                                
                                if (parameterRow.ContainerViewModel is ElementDefinitionRowViewModel elementDefinitionRow)
                                {
                                    this.UpdateRow(parameterOrOverrideBase, (ElementDefinition)oldElement, elementDefinitionRow);
                                }

                                else if (parameterRow.ContainerViewModel is ElementUsageRowViewModel elementUsageRow)
                                {
                                    this.UpdateRow(parameterOrOverrideBase, (ElementUsage)oldElement, elementUsageRow);
                                }

                                this.IsDirty = true;
                            }
                        }
                    }
                }

                this.IsBusy = false;
            }
        }

        public bool IsDirty { get; set; }

        private void UpdateRow<TThing, TRow>(ParameterOrOverrideBase parameterOrOverrideBase, TThing oldElement, TRow elementRow) 
            where TRow : ElementBaseRowViewModel<TThing> where TThing : ElementBase
        {
            var updatedElement = (TThing)oldElement?.Clone(true);

            this.AddOrReplaceParameter(updatedElement, parameterOrOverrideBase);

            CDPMessageBus.Current.SendMessage(new HighlightEvent(elementRow.Thing), elementRow.Thing);

            elementRow.UpdateThing(updatedElement);

            elementRow.UpdateChildren();
        }

        private void RestoreThings()
        {
            var isExpanded = this.Things.First().IsExpanded;
            this.UpdateTree(true);
            this.Things.First().IsExpanded = isExpanded;
        }

        private void AddOrReplaceParameter(ElementBase updatedElement, ParameterOrOverrideBase parameterOrOverride)
        {
            if (updatedElement is ElementDefinition elementDefinition)
            {
                if (elementDefinition.Parameter.FirstOrDefault(p => p.ParameterType.Iid == parameterOrOverride.ParameterType.Iid)
                    is { } parameter)
                {
                    elementDefinition.Parameter.Remove(parameter);
                }

                elementDefinition.Parameter.Add((Parameter)parameterOrOverride);
            }

            else if (updatedElement is ElementUsage elementUsage && parameterOrOverride is ParameterOverride parameterOverride)
            {
                if (elementUsage.ParameterOverride.FirstOrDefault(p => p.ParameterType.Iid == parameterOrOverride.ParameterType.Iid)
                    is { } parameter)
                {
                    elementUsage.ParameterOverride.Remove(parameter);
                }

                elementUsage.ParameterOverride.Add(parameterOverride);
            }
        }

        private IEnumerable<IRowViewModelBase<ParameterOrOverrideBase>> GetRows(ParameterBase parameter)
        {
            var result = new List<IRowViewModelBase<ParameterOrOverrideBase>>();

            foreach (var elementDefinitionRow in this.Things.OfType<ElementDefinitionsBrowserViewModel>()
                .SelectMany(x => x.ContainedRows.OfType<ElementDefinitionRowViewModel>()))
            {
                foreach (var parameterRow in elementDefinitionRow.ContainedRows.OfType<ElementUsageRowViewModel>()
                    .SelectMany(x => x.ContainedRows.OfType<IRowViewModelBase<ParameterOrOverrideBase>>()))
                {
                    result.AddRange(VerifyRowContainsTheParameter(parameter, parameterRow));
                }

                foreach (var parameterRow in elementDefinitionRow.ContainedRows.OfType<IRowViewModelBase<ParameterOrOverrideBase>>())
                {
                    result.AddRange(VerifyRowContainsTheParameter(parameter, parameterRow));
                }
            }

            return result;
        }

        private static IEnumerable<IRowViewModelBase<ParameterOrOverrideBase>> VerifyRowContainsTheParameter(ParameterBase parameter, IRowViewModelBase<ParameterOrOverrideBase> parameterRow)
        {
            var result = new List<IRowViewModelBase<ParameterOrOverrideBase>>();

            var containerIsTheRightOne = (parameterRow.ContainerViewModel.Thing.Iid == parameter.Container.Iid ||
                                          (parameterRow.ContainerViewModel.Thing is ElementUsage elementUsage
                                           && (elementUsage.ElementDefinition.Iid == parameter.Container.Iid
                                           || elementUsage.Iid == parameter.Container.Iid)));

            var parameterIsTheRightOne = (parameterRow.Thing.Iid == parameter.Iid ||
                                          (parameter.Iid == Guid.Empty
                                           && parameter.ParameterType.Iid == parameterRow.Thing.ParameterType.Iid));

            if (containerIsTheRightOne && parameterIsTheRightOne)
            {
                result.Add(parameterRow);
            }

            return result;
        }

        /// <summary>
        /// Updates the tree
        /// </summary>
        /// <param name="shouldReset">A value indicating whether the tree should remove the element in preview</param>
        public override void UpdateTree(bool shouldReset)
        {
            if (shouldReset)
            {
                this.Reload();
            }
            else
            {
                this.ComputeValuesWrapper();
            }
        }

        private void ComputeValuesWrapper()
        {
            this.IsBusy = true;
            this.ThingsAtPreviousStateBix.Clear();
            var isExpanded = this.Things.First().IsExpanded;
            this.ComputeValues();
            this.Things.First().IsExpanded = isExpanded;
            this.IsDirty = false;
            this.IsBusy = false;
        }

        /// <summary>
        /// Computes the old values for each <see cref="P:DEHPCommon.UserInterfaces.ViewModels.ObjectBrowserViewModel.Things" />
        /// </summary>
        public override void ComputeValues()
        {
            foreach (var parameterOverride in this.dstController.DstMapResult.OfType<ElementUsage>()
                .SelectMany(x => x.ParameterOverride))
            {
                var parameterRows = this.GetRows(parameterOverride).ToList();

                foreach (var parameterRow in parameterRows)
                {
                    if (parameterRow.ContainerViewModel is ElementUsageRowViewModel elementUsageRow)
                    {
                        this.UpdateRow(parameterOverride, (ElementUsage)elementUsageRow.ContainerViewModel.Thing, elementUsageRow);
                    }

                    this.ThingsAtPreviousStateBix.Add(parameterRow.ContainerViewModel.Thing.Clone(true));
                }
            }

            foreach (var parameter in this.dstController.DstMapResult.OfType<ElementDefinition>()
                .SelectMany(x => x.Parameter))
            {
                var elementRow = this.VerifyElementIsInTheTree(parameter);

                if (parameter.Iid == Guid.Empty)
                {
                    this.UpdateRow(parameter, (ElementDefinition)parameter.Container, elementRow);
                    continue;
                }

                var parameterRows = this.GetRows(parameter).ToList();
                
                foreach (var parameterRow in parameterRows)
                {
                    if (parameterRow.ContainerViewModel is ElementDefinitionRowViewModel elementDefinitionRow)
                    {
                        this.UpdateRow(parameter, (ElementDefinition)parameterRow.ContainerViewModel.Thing, elementDefinitionRow);
                    }

                    else if (parameterRow.ContainerViewModel is ElementUsageRowViewModel elementUsageRow)
                    {
                        this.UpdateRow(parameter, (ElementUsage)parameterRow.ContainerViewModel.Thing, elementUsageRow);
                    }

                    this.ThingsAtPreviousStateBix.Add(parameterRow.ContainerViewModel.Thing.Clone(true));
                }
            }
        }
        
        private ElementDefinitionRowViewModel VerifyElementIsInTheTree(Thing parameterOrOverrideBase)
        {
            var iterationRow =
                this.Things.OfType<ElementDefinitionsBrowserViewModel>().FirstOrDefault();

            var elementDefinitionRow = iterationRow.ContainedRows.OfType<ElementDefinitionRowViewModel>()
                .FirstOrDefault(e => e.Thing.Iid == parameterOrOverrideBase.Container.Iid
                                     && e.Thing.Name == ((INamedThing) parameterOrOverrideBase.Container).Name);

            if (elementDefinitionRow is null)
            {
                elementDefinitionRow = new ElementDefinitionRowViewModel((ElementDefinition) parameterOrOverrideBase.Container,
                    this.HubController.CurrentDomainOfExpertise, this.HubController.Session, iterationRow);

                iterationRow.ContainedRows.Add(elementDefinitionRow);
            }

            return elementDefinitionRow;
        }

        public List<Thing> ThingsAtPreviousStateBix { get; set; } = new List<Thing>();

        /// <summary>
        /// Not available for the net change preview panel
        /// </summary>
        public override void PopulateContextMenu()
        {
            this.ContextMenu.Clear();
        }
    }
}
