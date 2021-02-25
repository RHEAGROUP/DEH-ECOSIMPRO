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

    using CDP4JsonSerializer.JsonConverter;

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
    using DEHPEcosimPro.ViewModel.Rows;

    using DevExpress.Data;
    using DevExpress.Mvvm.Native;
    using DevExpress.Xpf.Reports.UserDesigner.Native;

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

                if (!eventArguments.Selection.Any())
                {
                    if (this.ThingsAtPreviousStateBix.Any())
                    {
                        this.RestoreThings();
                    }
                    else
                    {
                        this.ComputeValues();
                    }
                }
                else
                {
                    this.RestoreThings();

                    foreach (var variable in eventArguments.Selection)
                    {
                        var parameters = this.dstController.ParameterNodeIds
                            .Where(v => v.Value.Equals(variable.Reference.NodeId.Identifier))
                            .Select(x => x.Key);

                        foreach (var parameterOrOverrideBase in parameters)
                        {
                            var parameterRows = this.GetRows(parameterOrOverrideBase);

                            foreach (var parameterRow in parameterRows)
                            {
                                var oldElement = this.ThingsAtPreviousStateBix.FirstOrDefault(t => t.Iid == parameterRow.ContainerViewModel.Thing.Iid);
                                
                                if (parameterRow.ContainerViewModel is ElementDefinitionRowViewModel elementDefinitionRow)
                                {
                                    var updatedElement = ((ElementDefinition)oldElement).Clone(true);

                                    this.AddOrReplaceParameter(updatedElement, parameterRow);

                                    CDPMessageBus.Current.SendMessage(new HighlightEvent(elementDefinitionRow.Thing), elementDefinitionRow.Thing);

                                    elementDefinitionRow.UpdateThing(updatedElement);

                                    elementDefinitionRow.UpdateChildren();
                                }
                            }
                        }
                    }
                }

                this.IsBusy = false;
            }
        }

        private void RestoreThings()
        {
            var isExpanded = this.Things.First().IsExpanded;
            this.UpdateTree(true);
            this.Things.First().IsExpanded = isExpanded;
        }

        private void AddOrReplaceParameter(ElementBase updatedElement, IRowViewModelBase<ParameterOrOverrideBase> parameterRow)
        {
            if (updatedElement is ElementDefinition elementDefinition)
            {
                if (elementDefinition.Parameter.FirstOrDefault(p => p.ParameterType.Iid == parameterRow.Thing.ParameterType.Iid)
                    is { } parameter)
                {
                    elementDefinition.Parameter.Remove(parameter);
                }

                elementDefinition.Parameter.Add((Parameter) parameterRow.Thing);
            }

            else if (updatedElement is ElementUsage elementUsage)
            {
                if (elementUsage.ParameterOverride.FirstOrDefault(p => p.ParameterType.Iid == parameterRow.Thing.ParameterType.Iid)
                    is { } parameter)
                {
                    elementUsage.ParameterOverride.Remove(parameter);
                }

                elementUsage.ParameterOverride.Add((ParameterOverride)parameterRow.Thing);
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
                this.IsBusy = true;
                this.ThingsAtPreviousStateBix.Clear();
                var isExpanded = this.Things.First().IsExpanded;
                this.ComputeValues();
                this.Things.First().IsExpanded = isExpanded;
                this.IsBusy = false;
            }
        }

        /// <summary>
        /// Computes the old values for each <see cref="P:DEHPCommon.UserInterfaces.ViewModels.ObjectBrowserViewModel.Things" />
        /// </summary>
        public override void ComputeValues()
        {
            foreach (var iterationRow in this.Things.OfType<ElementDefinitionsBrowserViewModel>())
            {
                foreach (var thing in this.dstController.DstMapResult)
                {
                    if (thing is ElementDefinition elementDefinition)
                    {
                        var elementToUpdate = iterationRow.ContainedRows.OfType<ElementDefinitionRowViewModel>()
                            .FirstOrDefault(x => x.Thing.Iid == thing.Iid);

                        if (elementToUpdate is {})
                        {
                            this.ThingsAtPreviousStateBix.Add(elementToUpdate.Thing.Clone(true));

                            elementDefinition.Parameter.AddRange(elementToUpdate.Thing
                                .Parameter.Where(x => elementDefinition.Parameter.All(p => p.Iid != x.Iid)));

                            foreach (var parameterOrOverrideBaseRowViewModel in elementToUpdate.ContainedRows.OfType<ParameterOrOverrideBaseRowViewModel>())
                            {
                                parameterOrOverrideBaseRowViewModel.SetProperties();
                            }

                            CDPMessageBus.Current.SendMessage(new HighlightEvent(elementToUpdate.Thing), elementToUpdate.Thing);
                            elementToUpdate.ExpandAllRows();

                            if (elementToUpdate.Thing.Original is null)
                            {
                                elementToUpdate.UpdateThing(elementDefinition);
                            }
                            else
                            {
                                elementToUpdate.Thing.Parameter.Clear();
                                elementToUpdate.Thing.Parameter.AddRange(elementDefinition.Parameter);
                            }

                            elementToUpdate.UpdateChildren();
                        }
                        else
                        {
                            iterationRow.ContainedRows.Add(new ElementDefinitionRowViewModel(elementDefinition, this.HubController.CurrentDomainOfExpertise, this.HubController.Session, iterationRow));
                            CDPMessageBus.Current.SendMessage(new HighlightEvent(thing), thing);
                        }
                    }
                    else if (thing is ElementUsage elementUsage)
                    {
                        var elementUsageToUpdate = iterationRow.ContainedRows.OfType<ElementDefinitionRowViewModel>()
                            .SelectMany(x => x.ContainedRows.OfType<ElementUsageRowViewModel>())
                            .FirstOrDefault(x => x.Thing.Iid == elementUsage.Iid);

                        if (elementUsageToUpdate is null)
                        {
                            continue;
                        }

                        if (!elementUsage.ParameterOverride.All(p => elementUsageToUpdate.Thing
                            .ParameterOverride.Any(x => x.Iid == p.Iid)))
                        {
                            elementUsage.ParameterOverride.AddRange(elementUsageToUpdate.Thing.ParameterOverride
                                .Where(x => elementUsage.ParameterOverride.All(p => p.Iid != x.Iid)));
                        }

                        foreach (var parameterOrOverrideBaseRowViewModel in elementUsageToUpdate.ContainedRows.OfType<ParameterOrOverrideBaseRowViewModel>())
                        {
                            parameterOrOverrideBaseRowViewModel.SetProperties();
                        }

                        CDPMessageBus.Current.SendMessage(new ElementUsageHighlightEvent(elementUsageToUpdate.Thing.ElementDefinition), elementUsageToUpdate.Thing);

                        elementUsageToUpdate.ExpandAllRows();

                        if (elementUsageToUpdate.Thing.Original is null)
                        {
                            elementUsageToUpdate.UpdateThing(elementUsage);
                        }
                        else
                        {
                            elementUsageToUpdate.Thing.ParameterOverride.Clear();
                            elementUsageToUpdate.Thing.ParameterOverride.AddRange(elementUsage.ParameterOverride);
                        }

                        elementUsageToUpdate.UpdateChildren();
                    }
                }
            }
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
