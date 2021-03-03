// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstNetChangePreviewViewModel.cs" company="RHEA System S.A.">
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

    using CDP4Common.EngineeringModelData;

    using CDP4Dal;

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Events;
    using DEHPEcosimPro.ViewModel.Interfaces;
    using DEHPEcosimPro.ViewModel.Rows;

    using ReactiveUI;

    /// <summary>
    /// View model for this dst net change preview panel
    /// </summary>
    public class DstNetChangePreviewViewModel : DstVariablesControlViewModel, IDstNetChangePreviewViewModel
    {
        /// <summary>
        /// Gets or sets a value indicating that the tree in the case that
        /// <see cref="DstController.DstMapResult"/> is not empty and the tree is not showing all changes
        /// </summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// Initializes a new <see cref="DstNetChangePreviewViewModel"/>
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="navigationService">The <see cref="INavigationService"/></param>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel"/></param>
        public DstNetChangePreviewViewModel(IDstController dstController, INavigationService navigationService,
            IHubController hubController, IStatusBarControlViewModel statusBar) : base(dstController, navigationService, hubController, statusBar)
        {
            CDPMessageBus.Current.Listen<UpdateDstVariableTreeEvent>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => this.UpdateTree(x.Reset));

            CDPMessageBus.Current.Listen<UpdateDstPreviewBasedOnSelectionEvent>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => this.UpdateTreeBasedOnSelectionHandler(x.Selection.ToList()));
        }

        /// <summary>
        /// Updates the tree and filter changed things based on a selection
        /// </summary>
        /// <param name="selection">The collection of selected <see cref="ElementDefinitionRowViewModel"/> </param>
        private void UpdateTreeBasedOnSelectionHandler(IReadOnlyList<ElementDefinitionRowViewModel> selection)
        {
            if (this.DstController.HubMapResult.Any())
            {
                this.IsBusy = true;

                if (!selection.Any() && this.IsDirty)
                {
                    this.ComputeValuesWrapper();
                }

                else if (selection.Any())
                {
                    this.UpdateTreeBasedOnSelection(selection);
                }

                this.IsBusy = false;
            }
        }
        
        /// <summary>
        /// Updates the trees with the selection 
        /// </summary>
        /// <param name="selection">The collection of selected <see cref="VariableRowViewModel"/> </param>
        private void UpdateTreeBasedOnSelection(IEnumerable<ElementDefinitionRowViewModel> selection)
        {
            this.UpdateTree(true);
            
            var mappedElements = this.DstController.HubMapResult
                .Where(x => 
                    selection.Any(e => e.ContainedRows
                        .OfType<IRowViewModelBase<ParameterOrOverrideBase>>()
                        .Any(p => p.Thing.Iid == x.SelectedParameter.Iid)));

            foreach (var mappedElement in mappedElements)
            {
                if (mappedElement is { })
                {
                    this.UpdateVariableRow(mappedElement);
                }
            }

            this.IsDirty = true;
        }

        /// <summary>
        /// Updates the tree
        /// </summary>
        /// <param name="shouldReset">A value indicating whether the tree should remove the element in preview</param>
        public void UpdateTree(bool shouldReset)
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

        /// <summary>
        /// Calls the <see cref="ComputeValues"/> with some household
        /// </summary>
        private void ComputeValuesWrapper()
        {
            this.IsBusy = true;
            this.ComputeValues();
            this.IsDirty = false;
            this.IsBusy = false;
        }

        /// <summary>
        /// Resets the variable tree
        /// </summary>
        private void Reload()
        {
            foreach (var variable in this.Variables)
            {
                variable.ShouldListenToChangeMessage = true;
                CDPMessageBus.Current.SendMessage(new DstHighlightEvent(variable.Reference.NodeId.Identifier, false));
            }
        }

        /// <summary>
        /// Computes the old values for each <see cref="P:DEHPCommon.UserInterfaces.ViewModels.ObjectBrowserViewModel.Things" />
        /// </summary>
        public void ComputeValues()
        {
            foreach (var mappedElement in this.DstController.HubMapResult)
            {
                this.UpdateVariableRow(mappedElement);
            }
        }

        /// <summary>
        /// Updates the the corresponding variable according mapped by the <paramref name="mappedElement"/>
        /// </summary>
        /// <param name="mappedElement">The source <see cref="MappedElementDefinitionRowViewModel"/></param>
        private void UpdateVariableRow(MappedElementDefinitionRowViewModel mappedElement)
        {
            var variableChanged = this.Variables.FirstOrDefault(
                x => x.Reference.NodeId.Identifier == mappedElement.SelectedVariable.Reference.NodeId.Identifier);

            if (variableChanged is null)
            {
                return;
            }

            CDPMessageBus.Current.SendMessage(new DstHighlightEvent(variableChanged.Reference.NodeId.Identifier));
            variableChanged.ActualValue = mappedElement.SelectedValue.Value;
            variableChanged.ShouldListenToChangeMessage = false;
        }
    }
}
