﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EcosimProTransferControlViewModel.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.ViewModel
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using CDP4Common.EngineeringModelData;

    using CDP4Dal;

    using DEHPCommon.Enumerators;
    using DEHPCommon.Events;
    using DEHPCommon.Services.ExchangeHistory;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Events;

    using NLog;

    using ReactiveUI;

    /// <summary>
    /// <inheritdoc cref="TransferControlViewModel"/>
    /// </summary>
    public class EcosimProTransferControlViewModel : TransferControlViewModel
    {
        /// <summary>
        /// The <see cref="NLog.Logger"/>
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel"/>
        /// </summary>
        private readonly IStatusBarControlViewModel statusBar;

        /// <summary>
        /// The <see cref="IExchangeHistoryService"/>
        /// </summary>
        private readonly IExchangeHistoryService exchangeHistoryService;

        /// <summary>
        /// Backing field for <see cref="AreThereAnyTransferInProgress"/>
        /// </summary>
        private bool areThereAnyTransferInProgress;

        /// <summary>
        /// Gets or sets a value indicating whether the TransferCommand" is executing
        /// </summary>
        public bool AreThereAnyTransferInProgress
        {
            get => this.areThereAnyTransferInProgress;
            set => this.RaiseAndSetIfChanged(ref this.areThereAnyTransferInProgress, value);
        }

        /// <summary>
        /// Backing field for <see cref="CanTransfer"/>
        /// </summary>
        private bool canTransfer;

        /// <summary>
        /// Gets or sets a value indicating whether there is any awaiting transfer
        /// </summary>
        public bool CanTransfer
        {
            get => this.canTransfer;
            set => this.RaiseAndSetIfChanged(ref this.canTransfer, value);
        }

        /// <summary>
        /// Initializes a new <see cref="EcosimProTransferControlViewModel"/>
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel"/></param>
        /// <param name="exchangeHistoryService">The <see cref="IExchangeHistoryService"/></param>
        public EcosimProTransferControlViewModel(IDstController dstController, IStatusBarControlViewModel statusBar, IExchangeHistoryService exchangeHistoryService)
        {
            this.dstController = dstController;
            this.statusBar = statusBar;
            this.exchangeHistoryService = exchangeHistoryService;
            
            this.dstController.SelectedDstMapResultToTransfer.CountChanged.Subscribe(_ => this.UpdateNumberOfThingsToTransfer());
            this.dstController.SelectedHubMapResultToTransfer.CountChanged.Subscribe(_ => this.UpdateNumberOfThingsToTransfer());

            this.WhenAnyValue(x => x.dstController.MappingDirection)
                .Subscribe(x => this.UpdateNumberOfThingsToTransfer());
            
            this.TransferCommand = ReactiveCommand.CreateAsyncTask(
                this.WhenAnyValue(x => x.CanTransfer),
                async _ => await this.TransferCommandExecute(),
                RxApp.MainThreadScheduler);

            this.TransferCommand.ThrownExceptions
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(exception =>
                {
                    this.statusBar.Append($"{exception.Message}", StatusBarMessageSeverity.Error);
                    this.logger.Error(exception);
                });
            
            var canCancel = this.WhenAnyValue(x => x.AreThereAnyTransferInProgress);

            this.CancelCommand = ReactiveCommand.CreateAsyncTask(canCancel,
                async _ => await this.CancelTransfer(),
                RxApp.MainThreadScheduler);
        }
        
        /// <summary>
        /// Updates the <see cref="TransferControlViewModel.NumberOfThing"/>
        /// </summary>
        public void UpdateNumberOfThingsToTransfer()
        {
            this.NumberOfThing = this.dstController.MappingDirection switch
            {
                MappingDirection.FromHubToDst => this.dstController.SelectedHubMapResultToTransfer.Count,
                MappingDirection.FromDstToHub => this.dstController.SelectedDstMapResultToTransfer.Count,
                _ => throw new ArgumentException($"Use of unsupported value of {nameof(this.dstController.MappingDirection)}")
            };

            this.UpdateCanTransfer(this.NumberOfThing > 0);
        }
        
        /// <summary>
        /// Updates the <see cref="CanTransfer"/>
        /// </summary>
        private void UpdateCanTransfer(bool value)
        {
            this.CanTransfer = value;
        }

        /// <summary>
        /// Cancels the transfer in progress
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        private async Task CancelTransfer()
        {
            this.dstController.DstMapResult.Clear();
            this.dstController.HubMapResult.Clear();
            this.dstController.ParameterVariable.Clear();
            this.exchangeHistoryService.ClearPending();
            await Task.Delay(1);
            CDPMessageBus.Current.SendMessage(new UpdateDstVariableTreeEvent(true));
            CDPMessageBus.Current.SendMessage(new UpdateObjectBrowserTreeEvent(true));
            this.AreThereAnyTransferInProgress = false;
            this.IsIndeterminate = false;
        }

        /// <summary>
        /// Executes the transfer command
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        private async Task TransferCommandExecute()
        {
            var timer = new Stopwatch();
            timer.Start();
            this.AreThereAnyTransferInProgress = true;
            this.IsIndeterminate = true;
            this.statusBar.Append($"Transfers in progress");

            if (this.dstController.MappingDirection is MappingDirection.FromDstToHub)
            {
                await this.dstController.TransferMappedThingsToHub();
            }
            else
            {
                await this.dstController.TransferMappedThingsToDst();
            }

            await this.exchangeHistoryService.Write();
            timer.Stop();
            this.statusBar.Append($"Transfers completed in {timer.ElapsedMilliseconds} ms");
            this.IsIndeterminate = false;
            this.AreThereAnyTransferInProgress = false;
        }
    }
}
