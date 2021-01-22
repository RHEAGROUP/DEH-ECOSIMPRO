// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstVariablesControlViewModel.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2020 RHEA System S.A.
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

namespace DEHPEcosimPro.ViewModel
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Windows.Input;

    using Autofac;

    using CDP4Common.CommonData;

    using DEHPCommon;
    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel.Dialogs.Interfaces;
    using DEHPEcosimPro.ViewModel.Interfaces;
    using DEHPEcosimPro.ViewModel.Rows;
    using DEHPEcosimPro.Views.Dialogs;

    using Opc.Ua;

    using ReactiveUI;

    /// <summary>
    /// The <see cref="DstVariablesControlViewModel"/> is the view model for displaying OPC references
    /// </summary>
    public class DstVariablesControlViewModel : ReactiveObject, IDstVariablesControlViewModel, IHaveContextMenuViewModel
    {
        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel"/>
        /// </summary>
        private readonly IStatusBarControlViewModel statusBar;

        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="INavigationService"/>
        /// </summary>
        private readonly INavigationService navigationService;

        /// <summary>
        /// Backing field for <see cref="IsBusy"/>
        /// </summary>
        private bool isBusy;

        /// <summary>
        /// Gets or sets the assert indicating whether the view is busy
        /// </summary>
        public bool IsBusy
        {
            get => this.isBusy;
            set => this.RaiseAndSetIfChanged(ref this.isBusy, value);
        }
        
        /// <summary>
        /// Backing field for <see cref="SelectedThing"/>
        /// </summary>
        private VariableRowViewModel selectedThing;

        /// <summary>
        /// Gets or sets the selected row that represents a <see cref="ReferenceDescription"/>
        /// </summary>
        public VariableRowViewModel SelectedThing
        {
            get => this.selectedThing;
            set => this.RaiseAndSetIfChanged(ref this.selectedThing, value);
        }

        /// <summary>
        /// Gets or sets the selected row that represents a <see cref="ReferenceDescription"/>
        /// </summary>
        public ReactiveList<VariableRowViewModel> SelectedThings { get; set; } = new ReactiveList<VariableRowViewModel>();

        /// <summary>
        /// Gets the Context Menu for this browser
        /// </summary>
        public ReactiveList<ContextMenuItemViewModel> ContextMenu { get; } = new ReactiveList<ContextMenuItemViewModel>();

        /// <summary>
        /// Gets the collection of <see cref="VariableRowViewModel"/>
        /// </summary>
        public ReactiveList<VariableRowViewModel> Variables { get; } = new ReactiveList<VariableRowViewModel>();
        
        /// <summary>
        /// Gets the command that allows to map the selected things
        /// </summary>
        public ReactiveCommand<object> MapCommand { get; set; }

        /// <summary>
        /// Initializes a new <see cref="DstVariablesControlViewModel"/>
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="navigationService">The <see cref="INavigationService"/></param>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel"/></param>
        public DstVariablesControlViewModel(IDstController dstController, INavigationService navigationService, 
            IHubController hubController, IStatusBarControlViewModel statusBar)
        {
            this.dstController = dstController;
            this.navigationService = navigationService;
            this.hubController = hubController;
            this.statusBar = statusBar;

            this.WhenAnyValue(x => x.dstController.IsSessionOpen)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateProperties());

            this.WhenAnyValue(vm => vm.SelectedThing, vm => vm.SelectedThings.Changed)
                .Subscribe(_ => this.PopulateContextMenu());

            this.InitializeCommands();
        }

        /// <summary>
        /// Initializes the <see cref="ICommand"/> of this view model
        /// </summary>
        public void InitializeCommands()
        {
            var canMap = this.WhenAny(
                vm => vm.SelectedThing,
                vm => vm.SelectedThings.CountChanged,
                vm => vm.hubController.OpenIteration,
                vm => vm.dstController.MappingDirection,
                (selected, selection, iteration, mappingDirection) =>
                    iteration.Value != null && (selected.Value != null || this.SelectedThings.Any()) && mappingDirection.Value is MappingDirection.FromDstToHub).ObserveOn(RxApp.MainThreadScheduler);

            this.MapCommand = ReactiveCommand.Create(canMap);
            this.MapCommand.Subscribe(_ => this.MapCommandExecute());
        }

        /// <summary>
        /// Executes the <see cref="MapCommand"/>
        /// </summary>
        private void MapCommandExecute()
        {
            var viewModel = AppContainer.Container.Resolve<IMappingConfigurationDialogViewModel>();
            this.AssignMapping();
            var timer = new Stopwatch();
            timer.Start();
            viewModel.Variables.AddRange(this.SelectedThings);
            viewModel.UpdatePropertiesBasedOnMappingConfiguration();
            timer.Stop();
            this.statusBar.Append($"Mapping configuration loaded in {timer.ElapsedMilliseconds} ms");
            this.navigationService.ShowDialog<MappingConfigurationDialog, IMappingConfigurationDialogViewModel>(viewModel);
        }

        /// <summary>
        /// Assings a mapping configuration if any to each of the selected variables
        /// </summary>
        private void AssignMapping()
        {
            foreach (var variable in this.SelectedThings)
            {
                variable.MappingConfigurations.AddRange(
                    this.dstController.ExternalIdentifierMap.Correspondence.Where(
                        x => x.ExternalId == variable.ElementName || 
                             x.ExternalId == variable.ParameterName));
            }
        }
        
        /// <summary>
        /// Updates this view model properties
        /// </summary>
        public void UpdateProperties()
        {
            if (this.dstController.IsSessionOpen)
            {
                this.IsBusy = true;

                this.Variables.AddRange(this.dstController.Variables.Select(r => new VariableRowViewModel(r)));

                this.AddSubscriptions();
            }
            else
            {
                this.Variables.Clear();
                this.dstController.ClearSubscriptions();
            }

            this.IsBusy = false;
        }

        /// <summary>
        /// Adds all the subscription
        /// </summary>
        private void AddSubscriptions()
        {
            foreach (var variable in this.Variables)
            {
                this.dstController.AddSubscription(variable.Reference);
            }
        }

        /// <summary>
        /// Populate the context menu for this browser
        /// </summary>
        public void PopulateContextMenu()
        {
            this.ContextMenu.Clear();

            if (this.SelectedThing == null)
            {
                return;
            }

            this.ContextMenu.Add(new ContextMenuItemViewModel("Map selection", "", this.MapCommand,
                MenuItemKind.Export, ClassKind.NotThing));
        }
    }
}
