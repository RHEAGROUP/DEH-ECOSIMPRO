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
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// Backing field for <see cref="ServerAddress"/>
        /// </summary>
        private string serverAddress;

        /// <summary>
        /// The <see cref="NodeId"/> of the ServerStatus.CurrentTime node in an OPC session
        /// </summary>
        private readonly NodeId currentServerTimeNodeId = new NodeId(Variables.Server_ServerStatus_CurrentTime);

        /// <summary>
        /// Gets or sets the URI of the connected data source
        /// </summary>
        public string ServerAddress
        {
            get => this.serverAddress;
            set => this.RaiseAndSetIfChanged(ref this.serverAddress, value);
        }

        /// <summary>
        /// Backing field for <see cref="SamplingInterval"/>
        /// </summary>
        private int samplingInterval;

        /// <summary>
        /// Gets or sets the time, in milliseconds, between which data is recorded
        /// </summary>
        public int SamplingInterval
        {
            get => this.samplingInterval;
            set => this.RaiseAndSetIfChanged(ref this.samplingInterval, value);
        }

        /// <summary>
        /// Backing field for <see cref="VariablesCount"/>
        /// </summary>
        private int variablesCount;

        /// <summary>
        /// Gets or sets the total number of variables in the open session
        /// </summary>
        public int VariablesCount
        {
            get => this.variablesCount;
            set => this.RaiseAndSetIfChanged(ref this.variablesCount, value);
        }

        /// <summary>
        /// Backing field for <see cref="ServerStartTime"/> 
        /// </summary>
        private DateTime? serverStartTime;

        /// <summary>
        /// Gets or sets the date and time, in UTC, from which the server has been up and running
        /// </summary>
        public DateTime? ServerStartTime
        {
            get => this.serverStartTime;
            set => this.RaiseAndSetIfChanged(ref this.serverStartTime, value);
        }

        /// <summary>
        /// Backing field for <see cref="CurrentServerTime"/> 
        /// </summary>
        private DateTime? currentServerTime;

        /// <summary>
        /// Gets or sets the current date/time, in UTC, of the server
        /// </summary>
        public DateTime? CurrentServerTime
        {
            get => this.currentServerTime;
            set => this.RaiseAndSetIfChanged(ref this.currentServerTime, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DstBrowserHeaderViewModel"/>
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        public DstBrowserHeaderViewModel(IDstController dstController)
        {
            this.dstController = dstController;

            this.WhenAnyValue(x => x.dstController.IsSessionOpen).ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(this.UpdateProperties);

            CDPMessageBus.Current.Listen<OpcVariableChangedEvent>().Where(x => x.Id == this.currentServerTimeNodeId.Identifier)
                .Subscribe(e => this.CurrentServerTime = (DateTime)e.Value);
        }

        /// <summary>
        /// Updates the view model's properties
        /// </summary>
        private void UpdateProperties(bool isSessionOpen)
        {
            if (!isSessionOpen)
            {
                this.ServerAddress = string.Empty;
                this.SamplingInterval = 0;
                this.VariablesCount = 0;
                this.ServerStartTime = null;
                this.CurrentServerTime = null;
            }
            else
            {
                this.ServerAddress = this.dstController.ServerAddress;
                this.SamplingInterval = this.dstController.RefreshInterval;
                this.VariablesCount = this.dstController.Variables.Count;
                this.ServerStartTime = this.dstController.GetServerStartTime();
                this.CurrentServerTime = this.dstController.GetCurrentServerTime();

                this.dstController.AddSubscription(this.currentServerTimeNodeId);
            }
        }
    }
}
