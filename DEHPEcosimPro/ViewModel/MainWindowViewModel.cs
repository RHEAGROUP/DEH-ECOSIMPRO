// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindowViewModel.cs" company="RHEA System S.A.">
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
    using System.Windows.Input;

    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.Views.ExchangeHistory;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel.Interfaces;

    using ReactiveUI;

    /// <summary>
    /// <see cref="MainWindowViewModel"/> is the view model for <see cref="Views.MainWindow"/>
    /// </summary>
    public class MainWindowViewModel : ReactiveObject, IMainWindowViewModel
    {
        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="INavigationService"/>
        /// </summary>
        private readonly INavigationService navigationService;

        public IDifferenceViewModel DifferenceViewModel { get; private set; }

        /// <summary>
        /// Backing field for <see cref="MappingDirection"/>
        /// </summary>
        private int mappingDirection;

        /// <summary>
        /// Gets or sets the <see cref="ISwitchLayoutPanelOrderBehavior"/>
        /// </summary>
        public ISwitchLayoutPanelOrderBehavior SwitchPanelBehavior { get; set; }

        /// <summary>
        /// Gets the <see cref="ITransferControlViewModel"/>
        /// </summary>
        public ITransferControlViewModel TransferControlViewModel { get; }

        /// <summary>
        /// Gets the <see cref="IMappingViewModel"/>
        /// </summary>
        public IMappingViewModel MappingViewModel { get; }

        /// <summary>
        /// Gets the view model that represents the net change preview panel
        /// </summary>
        public IHubNetChangePreviewViewModel HubNetChangePreviewViewModel { get; }

        /// <summary>
        /// Gets the view model that represents the net change preview panel
        /// </summary>
        public IDstNetChangePreviewViewModel DstNetChangePreviewViewModel { get; }

        /// <summary>
        /// Gets the view model that represents the 10-25 data source
        /// </summary>
        public IHubDataSourceViewModel HubDataSourceViewModel { get; }

        /// <summary>
        /// Gets the view model that represents the EcosimPro data source
        /// </summary>
        public IDstDataSourceViewModel DstSourceViewModel { get; }

        /// <summary>
        /// Gets the view model that represents the status bar
        /// </summary>
        public IStatusBarControlViewModel StatusBarControlViewModel { get; }
        
        /// <summary>
        /// Gets or sets the <see cref="ICommand"/> that will change the mapping direction
        /// </summary>
        public ReactiveCommand<object> ChangeMappingDirection { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="ICommand"/> that will open the ExchangeHistory window
        /// </summary>
        public ReactiveCommand<object> OpenExchangeHistory { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="MappingDirection"/> for proper binding
        /// </summary>
        public int MappingDirection
        {
            get => this.mappingDirection;
            set => this.RaiseAndSetIfChanged(ref this.mappingDirection, value);
        }

        /// <summary>
        /// Initializes a new <see cref="MainWindowViewModel"/>
        /// </summary>
        /// <param name="hubDataSourceViewModelViewModel">A <see cref="IHubDataSourceViewModel"/></param>
        /// <param name="dstSourceViewModelViewModel">A <see cref="IHubDataSourceViewModel"/></param>
        /// <param name="statusBarControlViewModel">The <see cref="IStatusBarControlViewModel"/></param>
        /// <param name="hubNetChangePreviewViewModel">The <see cref="IHubNetChangePreviewViewModel"/></param>
        /// <param name="dstNetChangePreviewViewModel">The <see cref="IDstNetChangePreviewViewModel"/></param>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="transferControlViewModel">The <see cref="ITransferControlViewModel"/></param>
        /// <param name="mappingViewModel">The <see cref="IMappingViewModel"/></param>
        /// <param name="navigationService">The <see cref="INavigationService"/></param>
        /// <param name="differenceViewModel">The <see cref="IDifferenceViewModel"/></param>
        public MainWindowViewModel(IHubDataSourceViewModel hubDataSourceViewModelViewModel, IDstDataSourceViewModel dstSourceViewModelViewModel, 
            IStatusBarControlViewModel statusBarControlViewModel, IHubNetChangePreviewViewModel hubNetChangePreviewViewModel, 
            IDstNetChangePreviewViewModel dstNetChangePreviewViewModel, IDstController dstController, 
            ITransferControlViewModel transferControlViewModel, IMappingViewModel mappingViewModel,
            INavigationService navigationService, IDifferenceViewModel differenceViewModel)
        {
            this.dstController = dstController;
            this.navigationService = navigationService;
            this.DifferenceViewModel = differenceViewModel;
            this.TransferControlViewModel = transferControlViewModel;
            this.MappingViewModel = mappingViewModel;
            this.HubNetChangePreviewViewModel = hubNetChangePreviewViewModel;
            this.DstNetChangePreviewViewModel = dstNetChangePreviewViewModel;
            this.HubDataSourceViewModel = hubDataSourceViewModelViewModel;
            this.DstSourceViewModel = dstSourceViewModelViewModel;
            this.StatusBarControlViewModel = statusBarControlViewModel; 
            
            this.InitializeCommands();
        }

        /// <summary>
        /// Initializes this view model <see cref="ICommand"/>
        /// </summary>
        private void InitializeCommands()
        {
            this.ChangeMappingDirection = ReactiveCommand.Create();
            this.ChangeMappingDirection.Subscribe(_ => this.ChangeMappingDirectionExecute());

            this.OpenExchangeHistory = ReactiveCommand.Create();
            this.OpenExchangeHistory.Subscribe(_ => this.navigationService.ShowDialog<ExchangeHistory>());
        }

        /// <summary>
        /// Executes the <see cref="ChangeMappingDirection"/>
        /// </summary>
        private void ChangeMappingDirectionExecute()
        {
            this.SwitchPanelBehavior?.Switch();
            
            this.dstController.MappingDirection = this.SwitchPanelBehavior?.MappingDirection 
                                                  ?? DEHPCommon.Enumerators.MappingDirection.FromDstToHub;

            this.MappingDirection = (int)this.dstController.MappingDirection;
        }
    }
}
