// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstBrowserHeaderViewModel.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2020 RHEA System S.A.
// 
//    Author: Sam Gerené, Alex Vorobiev, Alexander van Delft, Nathanael Smiechowski, Ahmed Abulwafa Ahmed
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

    using CDP4Dal;

    using DEHPCommon.Enumerators;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Events;
    using DEHPEcosimPro.ViewModel.Interfaces;
    using DEHPEcosimPro.Views;
    using Opc.Ua;
    using ReactiveUI;

    /// <summary>
    /// The view model for <see cref="DstBrowserHeader"/>
    /// </summary>
    public class DstBrowserHeaderViewModel : ReactiveObject, IDstBrowserHeaderViewModel
    {
        /// <summary>
        /// The <see cref="NodeId"/> of the ServerStatus.CurrentTime node in an OPC session
        /// </summary>
        private readonly NodeId currentServerTimeNodeId = new NodeId(Variables.Server_ServerStatus_CurrentTime);

        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel"/> instance
        /// </summary>
        private readonly IStatusBarControlViewModel statusBarControlViewModel;

        /// <summary>
        /// Backing field for <see cref="ServerAddress"/>
        /// </summary>
        private string serverAddress;

        /// <summary>
        /// Backing field for <see cref="SamplingInterval"/>
        /// </summary>
        private int samplingInterval;

        /// <summary>
        /// Backing field for <see cref="VariablesCount"/>
        /// </summary>
        private int variablesCount;

        /// <summary>
        /// Backing field for <see cref="ServerStartTime"/> 
        /// </summary>
        private DateTime? serverStartTime;

        /// <summary>
        /// Backing field for <see cref="CurrentServerTime"/> 
        /// </summary>
        private DateTime? currentServerTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="DstBrowserHeaderViewModel"/>
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="statusBarControlViewModel">The <see cref="IStatusBarControlViewModel"/></param>
        public DstBrowserHeaderViewModel(IDstController dstController, IStatusBarControlViewModel statusBarControlViewModel)
        {
            this.dstController = dstController;
            this.statusBarControlViewModel = statusBarControlViewModel;

            this.WhenAnyValue(x => x.dstController.IsSessionOpen).Subscribe(_ => this.UpdateProperties());

            CDPMessageBus.Current.Listen<OpcVariableChangedEvent>().Where(x => x.Id == this.currentServerTimeNodeId.Identifier)
                .Subscribe(e => this.CurrentServerTime = (DateTime)e.Value);

            var canCallServerMethods = this.WhenAnyValue(vm => vm.dstController.IsSessionOpen);

            this.CallRunMethodCommand = ReactiveCommand.Create(canCallServerMethods);
            this.CallRunMethodCommand.Subscribe(_ => this.CallServerMethod("method_run"));

            this.CallResetMethodCommand = ReactiveCommand.Create(canCallServerMethods);
            this.CallResetMethodCommand.Subscribe(_ => this.CallServerMethod("method_reset"));
        }

        /// <summary>
        /// Gets or sets the URI of the connected data source
        /// </summary>
        public string ServerAddress
        {
            get => this.serverAddress;
            set => this.RaiseAndSetIfChanged(ref this.serverAddress, value);
        }

        /// <summary>
        /// Gets or sets the time, in milliseconds, between which data is recorded
        /// </summary>
        public int SamplingInterval
        {
            get => this.samplingInterval;
            set => this.RaiseAndSetIfChanged(ref this.samplingInterval, value);
        }

        /// <summary>
        /// Gets or sets the total number of variables in the open session
        /// </summary>
        public int VariablesCount
        {
            get => this.variablesCount;
            set => this.RaiseAndSetIfChanged(ref this.variablesCount, value);
        }

        /// <summary>
        /// Gets or sets the date and time, in UTC, from which the server has been up and running
        /// </summary>
        public DateTime? ServerStartTime
        {
            get => this.serverStartTime;
            set => this.RaiseAndSetIfChanged(ref this.serverStartTime, value);
        }

        /// <summary>
        /// Gets or sets the current date/time, in UTC, of the server
        /// </summary>
        public DateTime? CurrentServerTime
        {
            get => this.currentServerTime;
            set => this.RaiseAndSetIfChanged(ref this.currentServerTime, value);
        }

        /// <summary>
        /// <see cref="ReactiveCommand{T}"/> for calling the 'Run' server method
        /// </summary>
        public ReactiveCommand<object> CallRunMethodCommand { get; set; }

        /// <summary>
        /// <see cref="ReactiveCommand{T}"/> for calling the 'Reset' server method
        /// </summary>
        public ReactiveCommand<object> CallResetMethodCommand { get; set; }

        /// <summary>
        /// Updates the view model's properties
        /// </summary>
        public void UpdateProperties()
        {
            if (this.dstController.IsSessionOpen)
            {
                this.ServerAddress = this.dstController.ServerAddress;
                this.SamplingInterval = this.dstController.RefreshInterval;
                this.VariablesCount = this.dstController.Variables.Count;
                this.ServerStartTime = this.dstController.GetServerStartTime();
                this.CurrentServerTime = this.dstController.GetCurrentServerTime();

                this.dstController.AddSubscription(this.currentServerTimeNodeId);
            }
            else
            {
                this.ServerAddress = string.Empty;
                this.SamplingInterval = 0;
                this.VariablesCount = 0;
                this.ServerStartTime = null;
                this.CurrentServerTime = null;

                this.dstController.ClearSubscriptions();
            }
        }

        /// <summary>
        /// Calls a server method and reports its execution state to the represented status bar
        /// </summary>
        /// <param name="methodBrowseName">The BrowseName of the server method</param>
        private void CallServerMethod(string methodBrowseName)
        {
            if (string.IsNullOrEmpty(methodBrowseName))
            {
                return;
            }

            try
            {
                var callMethodResult = this.dstController.CallServerMethod(methodBrowseName);

                if (callMethodResult != null)
                {
                    this.statusBarControlViewModel.Append($"Method executed successfully. {string.Join(", ", callMethodResult)}");
                }
                else
                {
                    this.statusBarControlViewModel.Append($"No method was found with the BrowseName '{methodBrowseName}'", StatusBarMessageSeverity.Error);
                }
            }
            catch (Exception exception)
            {
                this.statusBarControlViewModel.Append($"Executing method {methodBrowseName} failed: {exception.Message}", StatusBarMessageSeverity.Error);
            }
        }
    }
}
