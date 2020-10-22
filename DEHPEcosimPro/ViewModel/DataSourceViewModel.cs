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

    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.Views;

    using DEHPEcosimPro.ViewModel.Interfaces;

    using ReactiveUI;

    /// <summary>
    /// View model that represents a data source panel <see cref="Views.DataSourcePanel"/>
    /// </summary>
    public class DataSourceViewModel : ReactiveObject, IDataSourceViewModel
    {
        /// <summary>
        /// The <see cref="INavigationService"/>
        /// </summary>
        private readonly INavigationService navigationService;

        /// <summary>
        /// Backing field for <see cref="Name"/>
        /// </summary>
        private string name;

        /// <summary>
        /// Gets or sets the name of this represented <see cref="Views.DataSourcePanel"/>
        /// </summary>
        public string Name
        {
            get => this.name;
            set => this.RaiseAndSetIfChanged(ref this.name, value);
        }

        /// <summary>
        /// Backing field for <see cref="IsConnected"/>
        /// </summary>
        private bool isConnected;

        /// <summary>
        /// Gets or sets an assert whether a connection is established
        /// </summary>
        public bool IsConnected
        {
            get => this.isConnected;
            set => this.RaiseAndSetIfChanged(ref this.isConnected, value);
        }
        
        /// <summary>
        /// <see cref="ReactiveCommand{T}"/> for connecting to a data source
        /// </summary>
        public ReactiveCommand<object> ConnectCommand { get; set; }

        /// <summary>
        /// Initializes a new <see cref="DataSourceViewModel"/>
        /// </summary>
        public DataSourceViewModel(INavigationService navigationService)
        {
            this.navigationService = navigationService;
            this.InitializeCommands();
        }

        /// <summary>
        /// Initializes the <see cref="ReactiveCommand{T}"/>
        /// </summary>
        private void InitializeCommands()
        {
            var canConnect = this.WhenAnyValue(x => x.IsConnected).Where(x => x is false);
            this.ConnectCommand = ReactiveCommand.Create();
            this.ConnectCommand.Subscribe(_ => this.ConnectCommandExecute());
        }

        /// <summary>
        /// Executes the <see cref="ConnectCommand"/>
        /// </summary>
        private void ConnectCommandExecute()
        {
            this.navigationService.ShowDialog<Login>();
        }
    }
}
