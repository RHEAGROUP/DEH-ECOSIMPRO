// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataSourceViewModel.cs" company="RHEA System S.A.">
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
    using DEHPCommon.UserInterfaces.Views;

    using DEHPEcosimPro.ViewModel.Interfaces;

    using ReactiveUI;

    /// <summary>
    /// View model that represents a data source panel
    /// </summary>
    public class DataSourceViewModel : ReactiveObject, IDataSourceViewModel
    {
        /// <summary>
        /// The connect text for the connect button
        /// </summary>
        private const string ConnectText = "Connect";

        /// <summary>
        /// The disconnect text for the connect button
        /// </summary>
        private const string DisconnectText = "Disconnect";

        /// <summary>
        /// The <see cref="INavigationService"/>
        /// </summary>
        private readonly INavigationService navigationService;

        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// Backing field for <see cref="ConnectButtonText"/>
        /// </summary>
        private string connectButtonText = ConnectText;

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string ConnectButtonText
        {
            get => this.connectButtonText;
            set => this.RaiseAndSetIfChanged(ref this.connectButtonText, value);
        }
        
        /// <summary>
        /// <see cref="ReactiveCommand{T}"/> for connecting to a data source
        /// </summary>
        public ReactiveCommand<object> ConnectCommand { get; set; }

        /// <summary>
        /// Initializes a new <see cref="DataSourceViewModel"/>
        /// </summary>
        public DataSourceViewModel(INavigationService navigationService, IHubController hubController)
        {
            this.navigationService = navigationService;
            this.hubController = hubController;
            this.InitializeCommands();
        }

        /// <summary>
        /// Initializes the <see cref="ReactiveCommand{T}"/>
        /// </summary>
        private void InitializeCommands()
        {
            this.ConnectCommand = ReactiveCommand.Create();
            this.ConnectCommand.Subscribe(_ => this.ConnectCommandExecute());
        }

        /// <summary>
        /// Executes the <see cref="ConnectCommand"/>
        /// </summary>
        private void ConnectCommandExecute()
        {
            if (this.hubController.IsSessionOpen)
            {
                this.hubController.Close();
            }
            else
            {
                this.navigationService.ShowDialog<Login>();
            }

            this.ConnectButtonText = this.hubController.IsSessionOpen ? DisconnectText : ConnectText;
        }
    }
}
