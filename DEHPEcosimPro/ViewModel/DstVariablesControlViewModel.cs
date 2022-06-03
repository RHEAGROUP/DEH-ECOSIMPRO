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
    using System.Threading.Tasks;
    using System.Windows.Input;

    using Autofac;

    using CDP4Common.CommonData;

    using CDP4Dal;

    using DEHPCommon;
    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Events;
    using DEHPEcosimPro.ViewModel.Dialogs.Interfaces;
    using DEHPEcosimPro.ViewModel.Interfaces;
    using DEHPEcosimPro.ViewModel.Rows;
    using DEHPEcosimPro.Views.Dialogs;

    using NLog;

    using Opc.Ua;

    using ReactiveUI;

    /// <summary>
    /// The <see cref="DstVariablesControlViewModel"/> is the view model for displaying OPC references
    /// </summary>
    public class DstVariablesControlViewModel : ReactiveObject, IDstVariablesControlViewModel, IHaveContextMenuViewModel
    {
        /// <summary>
        /// The <see cref="NLog"/> logger
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
        protected readonly IDstController DstController;

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
        /// Gets the collection of <see cref="VariableRowViewModel"/>
        /// </summary>
        public ReactiveList<VariableRowViewModel> Variables { get; }

        /// <summary>
        /// Gets or sets the selected row that represents a <see cref="ReferenceDescription"/>
        /// </summary>
        public ReactiveList<VariableRowViewModel> SelectedThings { get; set; } = new ReactiveList<VariableRowViewModel>();

        /// <summary>
        /// Gets the Context Menu for this browser
        /// </summary>
        public ReactiveList<ContextMenuItemViewModel> ContextMenu { get; } = new ReactiveList<ContextMenuItemViewModel>();

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
            this.DstController = dstController;
            this.navigationService = navigationService;
            this.hubController = hubController;
            this.statusBar = statusBar;
            
            this.WhenAnyValue(vm => vm.SelectedThing, vm => vm.SelectedThings.CountChanged)
                .Subscribe(_ =>
                {
                    this.PopulateContextMenu();
                });

            this.SelectedThings.CountChanged.Subscribe(_ => this.UpdateNetChangePreviewBasedOnSelection());
            
            this.Variables = this.DstController.VariableRowViewModels;

            this.InitializeCommands();
        }

        /// <summary>
        /// Sends an update event to the Hub net change preview based on the current <see cref="SelectedThings"/>
        /// </summary>
        private void UpdateNetChangePreviewBasedOnSelection()
        {
            CDPMessageBus.Current.SendMessage(new UpdateHubPreviewBasedOnSelectionEvent(this.SelectedThings, null, false));
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
                vm => vm.DstController.MappingDirection,
                (selected, selection, iteration, mappingDirection) =>
                    iteration.Value != null && (selected.Value != null || this.SelectedThings.Any()) && mappingDirection.Value is MappingDirection.FromDstToHub).ObserveOn(RxApp.MainThreadScheduler);

            this.MapCommand = ReactiveCommand.Create(canMap);
            this.MapCommand.Subscribe(_ => this.MapCommandExecute());
            this.MapCommand.ThrownExceptions.Subscribe(e => Logger.Error(e));
        }
        
        /// <summary>
        /// Executes the <see cref="MapCommand"/>
        /// </summary>
        private void MapCommandExecute()
        {
            try
            {
                Logger.Debug("Map command execute starting");
                var viewModel = AppContainer.Container.Resolve<IDstMappingConfigurationDialogViewModel>();
                viewModel.Initialize();
                var timer = new Stopwatch();
                timer.Start();

                Logger.Debug("Start assigning SelectedThings to the dialog Variables");

                viewModel.Variables.AddRange(this.SelectedThings.Select(x =>
                {
                    x.SetChartValues();
                    return x;
                }));

                Logger.Debug("End assigning SelectedThings to the dialog Variables");

                timer.Stop();
                this.statusBar.Append($"Mapping configuration loaded in {timer.ElapsedMilliseconds} ms");

                Logger.Debug("Calling NavigationService to open the DstMappingConfigurationDialog");
                this.navigationService.ShowDialog<DstMappingConfigurationDialog, IDstMappingConfigurationDialogViewModel>(viewModel);
                viewModel.ClearSubscriptions();
                Logger.Debug("DstMappingConfigurationDialog closed");
                this.SelectedThings.Clear();
                this.statusBar.Append($"Mapping in progress");
                Logger.Debug("Map command execute end");
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Console.WriteLine(e);
                throw;
            }
        }
        
        /// <summary>
        /// Populate the context menu for this browser
        /// </summary>
        public virtual void PopulateContextMenu()
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
