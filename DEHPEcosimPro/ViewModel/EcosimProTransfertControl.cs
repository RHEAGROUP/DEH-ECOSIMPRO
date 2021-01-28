// --------------------------------------------------------------------------------------------------------------------
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
    using System.Reactive;
    using System.Threading.Tasks;

    using DEHPCommon.UserInterfaces.ViewModels;

    using DEHPEcosimPro.DstController;

    using ReactiveUI;

    /// <summary>
    /// <inheritdoc cref="TransferControlViewModel"/>
    /// </summary>
    public class EcosimProTransferControlViewModel : TransferControlViewModel
    {
        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// Backing field for <see cref="AreThereAnyTransferInProgress"/>
        /// </summary>
        private bool areThereAnyTransferInProgress;

        /// <summary>
        /// Gets or sets a value indicating whether the TransfertCommand" is executing
        /// </summary>
        public bool AreThereAnyTransferInProgress
        {
            get => this.areThereAnyTransferInProgress;
            set => this.RaiseAndSetIfChanged(ref this.areThereAnyTransferInProgress, value);
        }

        /// <summary>
        /// Initializes a new <see cref="EcosimProTransferControlViewModel"/>
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        public EcosimProTransferControlViewModel(IDstController dstController)
        {
            this.dstController = dstController;

            var canTransfert = this.WhenAnyValue(x => x.dstController.HasSomeMappedThingsReadyToTransfert);
            this.TransferCommand = ReactiveCommand.CreateAsyncTask(canTransfert, async _ => await this.TransfertCommandExecute());
            var canCancel = this.WhenAnyValue(x => x.AreThereAnyTransferInProgress);
            this.CancelCommand = ReactiveCommand.CreateAsyncTask(canCancel, async _ => await this.CancelTransfer());
        }

        /// <summary>
        /// Cancels the transfer in progress
        /// </summary>
        /// <returns>A <see cref="Task"/><returns>
        private Task CancelTransfer()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Executes the transfert command
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        private async Task TransfertCommandExecute()
        {
            await this.dstController.Transfer();
        }
    }
}
