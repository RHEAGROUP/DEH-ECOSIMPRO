// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstDataSourceViewModel.cs" company="RHEA System S.A.">
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

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel.Interfaces;
    using DEHPEcosimPro.Views.Dialogs;

    using ReactiveUI;

    /// <summary>
    /// The <see cref="DstDataSourceViewModel"/> is the view model for the panel that will display controls and data relative to EcosimPro
    /// </summary>
    public sealed class DstDataSourceViewModel : DataSourceViewModel, IDstDataSourceViewModel
    {
        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;
        
        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// Gets the <see cref="IDstBrowserHeaderViewModel"/>
        /// </summary>
        public IDstBrowserHeaderViewModel DstBrowserHeader { get; }

        /// <summary>
        /// Gets the <see cref="IDstVariablesControlViewModel"/>
        /// </summary>
        public IDstVariablesControlViewModel DstVariablesViewModel { get; }

        /// <summary>
        /// Initializes a new <see cref="DstDataSourceViewModel"/>
        /// </summary>
        /// <param name="navigationService">The <see cref="INavigationService"/></param>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="dstBrowserHeader">The <see cref="IHubBrowserHeaderViewModel"/></param>
        /// <param name="dstVariablesViewModel">The <see cref="IDstVariablesControlViewModel"/></param>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        public DstDataSourceViewModel(INavigationService navigationService, IDstController dstController, 
            IDstBrowserHeaderViewModel dstBrowserHeader, IDstVariablesControlViewModel dstVariablesViewModel, IHubController hubController) : base(navigationService)
        {
            this.dstController = dstController;
            this.hubController = hubController;
            this.DstVariablesViewModel = dstVariablesViewModel;
            this.DstBrowserHeader = dstBrowserHeader;
            this.InitializeCommands();
        }

        /// <summary>
        /// Initializes the <see cref="ReactiveCommand{T}"/>
        /// </summary>
        protected override void InitializeCommands()
        {
            var canExecute = this.WhenAny(x => x.hubController.OpenIteration,
                x => x.dstController.IsSessionOpen,
                x => x.hubController.IsSessionOpen, 
                (i,d , s) 
                    => d.Value || (i.Value != null && s.Value));

            this.ConnectCommand = ReactiveCommand.Create(canExecute, RxApp.MainThreadScheduler);
            this.ConnectCommand.Subscribe(_ => this.ConnectCommandExecute());

            this.WhenAnyValue(x => x.dstController.IsSessionOpen)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(this.UpdateConnectButtonText);

            this.WhenAnyValue(x => x.dstController.IsExperimentRunning)
                .Subscribe(x => this.DstVariablesViewModel.IsBusy = x);
        }

        /// <summary>
        /// Executes the <see cref="DataSourceViewModel.ConnectCommand"/>
        /// </summary>
        protected override void ConnectCommandExecute()
        {
            this.DstVariablesViewModel.IsBusy = true;

            if (this.dstController.IsSessionOpen)
            {
                this.dstController.CloseSession();
            }
            else
            {
                this.NavigationService.ShowDialog<DstLogin>();
            }

            this.DstVariablesViewModel.IsBusy = false;
        }
    }
}
