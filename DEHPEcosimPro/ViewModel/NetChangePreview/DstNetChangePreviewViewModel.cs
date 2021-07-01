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

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;

    using CDP4Dal;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Events;
    using DEHPEcosimPro.ViewModel.Interfaces;
    using DEHPEcosimPro.ViewModel.Rows;

    using DevExpress.XtraRichEdit.Commands;

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
        /// The command for the context menu that allows to deselect all selectable <see cref="ElementBase"/> for transfer.
        /// It executes <see cref="SelectDeselectAllForTransfer"/>
        /// </summary>
        public ReactiveCommand<object> DeselectAllCommand { get; set; }

        /// <summary>
        /// The command for the context menu that allows to select all selectable <see cref="ElementBase"/> for transfer.
        /// It executes <see cref="SelectDeselectAllForTransfer"/>
        /// </summary>
        public ReactiveCommand<object> SelectAllCommand { get; set; }

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
            this.InitializeCommandsAndObservables();
        }

        /// <summary>
        /// Initializes this view model commands and observable
        /// </summary>
        private void InitializeCommandsAndObservables()
        {
            CDPMessageBus.Current.Listen<UpdateDstVariableTreeEvent>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => this.UpdateTree(x.Reset));

            CDPMessageBus.Current.Listen<UpdateDstPreviewBasedOnSelectionEvent>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => this.UpdateTreeBasedOnSelectionHandler(x.Selection.ToList()));
            
            this.SelectedThings.BeforeItemsAdded.Subscribe(this.WhenItemSelectedChanges);
            this.SelectedThings.BeforeItemsRemoved.Subscribe(this.WhenItemSelectedChanges);

            this.SelectAllCommand = ReactiveCommand.Create();
            this.SelectAllCommand.Subscribe(_ => this.SelectDeselectAllForTransfer());

            this.DeselectAllCommand = ReactiveCommand.Create();
            this.DeselectAllCommand.Subscribe(_ => this.SelectDeselectAllForTransfer(false));
        }
        
        /// <summary>
        /// Occurs when the <see cref="DstNetChangePreviewViewModel.SelectedThings"/> gets a new element added or removed
        /// </summary>
        /// <param name="row">The <see cref="object"/> row that was added or removed</param>
        private void WhenItemSelectedChanges(object row)
        {
            if (!(row is VariableRowViewModel rowViewModel))
            {
                return;
            }
            
            var mappedElement = this.DstController.HubMapResult.FirstOrDefault(x => MappedElementMatching(x, rowViewModel));

            if (mappedElement is null)
            {
                return;
            }

            this.AddOrRemoveToSelectedThingsToTransfer(!rowViewModel.IsSelectedForTransfer, mappedElement, rowViewModel);
        }

        /// <summary>
        /// Verify that the <paramref name="mappedElementRowViewModel"/> is based on the <see cref="rowViewModel"/>
        /// </summary>
        /// <param name="mappedElementRowViewModel">The <see cref="MappedElementDefinitionRowViewModel"/></param>
        /// <param name="rowViewModel">The <see cref="VariableRowViewModel"/></param>
        /// <returns>An assert</returns>
        private static bool MappedElementMatching(MappedElementDefinitionRowViewModel mappedElementRowViewModel, VariableRowViewModel rowViewModel) 
            => mappedElementRowViewModel.SelectedVariable.Reference.NodeId.Identifier == rowViewModel.Reference.NodeId.Identifier;

        /// <summary>
        /// Adds or removes the <paramref name="mappedElement"/> to/from the <see cref="IDstController.SelectedHubMapResultToTransfer"/>
        /// </summary>
        /// <param name="shouldSelect">A value indicating whether the <paramref name="mappedElement"/> should be added or removed</param>
        /// <param name="mappedElement">The <see cref="MappedElementDefinitionRowViewModel"/></param>
        /// <param name="rowViewModel">The <see cref="VariableRowViewModel"/></param>
        private void AddOrRemoveToSelectedThingsToTransfer(bool shouldSelect, MappedElementDefinitionRowViewModel mappedElement, VariableRowViewModel rowViewModel = null)
        {
            rowViewModel ??= this.Variables.FirstOrDefault(x => x.Reference.NodeId.Identifier == mappedElement.SelectedVariable.Reference.NodeId.Identifier);

            if (rowViewModel is null)
            {
                return;
            }

            rowViewModel.IsSelectedForTransfer = shouldSelect;

            if (this.DstController.SelectedHubMapResultToTransfer.FirstOrDefault(x => MappedElementMatching(x, rowViewModel)) is { } element)
            {
                this.DstController.SelectedHubMapResultToTransfer.Remove(element);
            }

            if (rowViewModel.IsSelectedForTransfer)
            {
                this.DstController.SelectedHubMapResultToTransfer.Add(mappedElement);
            }
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
                variable.IsSelectedForTransfer = false;
                variable.ActualValue = this.DstController.ReadNode(variable.Reference);
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

        /// <summary>
        /// Populates the context menu
        /// </summary>
        public override void PopulateContextMenu()
        {
            this.ContextMenu.Clear();

            this.ContextMenu.Add(
                new ContextMenuItemViewModel("Select all for transfer", "", this.SelectAllCommand, MenuItemKind.Copy, ClassKind.NotThing));

            this.ContextMenu.Add(
                new ContextMenuItemViewModel("Deselect all for transfer", "", this.DeselectAllCommand, MenuItemKind.Delete, ClassKind.NotThing));
        }

        /// <summary>
        /// Executes the <see cref="SelectAllCommand"/> and the <see cref="DeselectAllCommand"/>
        /// </summary>
        /// <param name="areSelected">A value indicating whether the elements are to be selected</param>
        public void SelectDeselectAllForTransfer(bool areSelected = true)
        {
            foreach (var element in this.DstController.HubMapResult)
            {
                this.AddOrRemoveToSelectedThingsToTransfer(areSelected, element);
            }
        }
    }
}
