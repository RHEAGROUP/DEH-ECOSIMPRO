// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EcosimProTransferControlViewModelTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Tests.ViewModel
{
    using System;
    using System.Threading.Tasks;

    using CDP4Dal;

    using DEHPCommon.Events;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class EcosimProTransferControlViewModelTestFixture
    {
        private EcosimProTransferControlViewModel viewModel;

        private Mock<IDstController> dstController;

        [SetUp]
        public void Setup()
        {
            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.Transfer()).Returns(Task.CompletedTask);
            this.viewModel = new EcosimProTransferControlViewModel(this.dstController.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsFalse(this.viewModel.AreThereAnyTransferInProgress);
            Assert.IsFalse(this.viewModel.IsIndeterminate);
            Assert.Zero(this.viewModel.Progress);
            Assert.IsNotNull(this.viewModel.TransferCommand);
            Assert.IsNotNull(this.viewModel.CancelCommand);
        }

        [Test]
        public void VerifyTransferCommand()
        {
            Assert.IsFalse(this.viewModel.TransferCommand.CanExecute(null));
            CDPMessageBus.Current.SendMessage(new UpdateObjectBrowserTreeEvent(true));
            Assert.IsFalse(this.viewModel.TransferCommand.CanExecute(null));
            CDPMessageBus.Current.SendMessage(new UpdateObjectBrowserTreeEvent());
            Assert.IsTrue(this.viewModel.TransferCommand.CanExecute(null));
            
            Assert.DoesNotThrowAsync(() => this.viewModel.TransferCommand.ExecuteAsyncTask(null));
            this.dstController.Verify(x => x.Transfer(), Times.Once);
        }

        [Test]
        public void VerifyCancelCommand()
        {
            Assert.IsFalse(this.viewModel.CancelCommand.CanExecute(null));
            this.viewModel.AreThereAnyTransferInProgress = true;
            Assert.IsTrue(this.viewModel.CancelCommand.CanExecute(null));

            Assert.ThrowsAsync<NotImplementedException>(() => this.viewModel.CancelCommand.ExecuteAsyncTask(null));
        }
    }
}
