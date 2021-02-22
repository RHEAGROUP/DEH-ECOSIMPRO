﻿// --------------------------------------------------------------------------------------------------------------------
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
    using System.Linq;
    using System.Reactive.Linq;

    using CDP4Common.EngineeringModelData;

    using CDP4Dal;

    using DEHPCommon.Events;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.ObjectBrowserTreeSelectorService;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.NetChangePreview;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Events;
    using DEHPEcosimPro.ViewModel.Interfaces;

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
            foreach (var variable in eventArguments.Selection)
            {
                
            }
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
                this.ThingsAtPreviousState.Clear();
                this.ThingsAtPreviousState.AddRange(this.Things);
                this.ComputeValues();
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
                    var elementToUpdate = iterationRow.ContainedRows.OfType<ElementDefinitionRowViewModel>()
                        .FirstOrDefault(x => x.Thing.Iid == thing.Iid);

                    if (elementToUpdate is {})
                    {
                        thing.Parameter.AddRange(elementToUpdate.Thing.Parameter.Where(x => thing.Parameter.All(p => p.Iid != x.Iid)));

                        foreach (var parameterOrOverrideBaseRowViewModel in elementToUpdate.ContainedRows.OfType<ParameterOrOverrideBaseRowViewModel>())
                        {
                            parameterOrOverrideBaseRowViewModel.SetProperties();
                        }

                        CDPMessageBus.Current.SendMessage(new HighlightEvent(elementToUpdate.Thing), elementToUpdate.Thing);
                        elementToUpdate.ExpandAllRows();

                        if (elementToUpdate.Thing.Original is null)
                        {
                            elementToUpdate.UpdateThing(thing);
                        }
                        else
                        {
                            elementToUpdate.Thing.Parameter.Clear();
                            elementToUpdate.Thing.Parameter.AddRange(thing.Parameter);
                        }

                        elementToUpdate.UpdateChildren();
                    }
                    else
                    {
                        iterationRow.ContainedRows.Add(new ElementDefinitionRowViewModel(thing, this.HubController.CurrentDomainOfExpertise, this.HubController.Session, iterationRow));
                        CDPMessageBus.Current.SendMessage(new HighlightEvent(thing), thing);
                    }

                    foreach (var elementUsage in thing.ContainedElement)
                    {
                        var elementUsageToUpdate = iterationRow.ContainedRows.OfType<ElementDefinitionRowViewModel>()
                            .SelectMany(x => x.ContainedRows.OfType<ElementUsageRowViewModel>())
                            .FirstOrDefault(x => x.Thing.Iid == elementUsage.Iid);

                        if (elementUsageToUpdate is null)
                        {
                            continue;
                        }

                        if (!elementUsage.ParameterOverride.All(p => elementUsageToUpdate.Thing.ParameterOverride.Any(x => x.Iid == p.Iid)))
                        {
                            elementUsage.ParameterOverride.AddRange(elementUsageToUpdate.Thing.ParameterOverride.Where(x => thing.Parameter.All(p => p.Iid != x.Iid)));
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

        /// <summary>
        /// Not available for the net change preview panel
        /// </summary>
        public override void PopulateContextMenu()
        {
            this.ContextMenu.Clear();
        }
    }
}
