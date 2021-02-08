﻿// --------------------------------------------------------------------------------------------------------------------
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
    using System.Reactive.Linq;
    using System.Windows.Input;

    using DEHPCommon.Enumerators;
    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.NetChangePreview.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel.Interfaces;

    using DevExpress.CodeParser;

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
        /// Backing field for <see cref="MappingDirection"/>
        /// </summary>
        private MappingDirection mappingDirection;

        /// <summary>
        /// Gets or sets the <see cref="ISwitchLayoutPanelOrderBehavior"/>
        /// </summary>
        public ISwitchLayoutPanelOrderBehavior SwitchPanelBehavior { get; set; }

        /// <summary>
        /// Gets the <see cref="ITransferControlViewModel"/>
        /// </summary>
        public ITransferControlViewModel TransferControlViewModel { get; }

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
        /// Gets or sets the <see cref="MappingDirection"/> for proper binding
        /// </summary>
        public MappingDirection MappingDirection
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
        /// <param name="hubNetChangePreviewViewModel">The <see cref="IDstNetChangePreviewViewModel"/></param>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="transferControlViewModel">The <see cref="ITransferControlViewModel"/></param>
        public MainWindowViewModel(IHubDataSourceViewModel hubDataSourceViewModelViewModel, IDstDataSourceViewModel dstSourceViewModelViewModel, 
            IStatusBarControlViewModel statusBarControlViewModel, IHubNetChangePreviewViewModel hubNetChangePreviewViewModel, 
            IDstNetChangePreviewViewModel dstNetChangePreviewViewModel, IDstController dstController, ITransferControlViewModel transferControlViewModel)
        {
            this.dstController = dstController;
            this.TransferControlViewModel = transferControlViewModel;
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
            
            this.ChangeMappingDirection.ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.ChangeMappingDirectionExecute());
        }

        /// <summary>
        /// Executes the <see cref="ChangeMappingDirection"/>
        /// </summary>
        private void ChangeMappingDirectionExecute()
        {
            this.SwitchPanelBehavior?.Switch();
            this.dstController.MappingDirection = this.SwitchPanelBehavior?.MappingDirection ?? MappingDirection.FromDstToHub;
            this.MappingDirection = this.dstController.MappingDirection;
        }
    }
}
