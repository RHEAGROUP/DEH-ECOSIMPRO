﻿// --------------------------------------------------------------------------------------------------------------------
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
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using CDP4Common.EngineeringModelData;

    using DEHPCommon.Enumerators;
    using DEHPCommon.Services.ExchangeHistory;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel;
    using DEHPEcosimPro.ViewModel.Rows;

    using Moq;

    using NUnit.Framework;

    using ReactiveUI;

    [TestFixture]
    public class EcosimProTransferControlViewModelTestFixture
    {
        private EcosimProTransferControlViewModel viewModel;

        private Mock<IDstController> dstController;
        private Mock<IStatusBarControlViewModel> statusBar;
        private Mock<IExchangeHistoryService> exchangeHistoryService;

        [SetUp]
        public void Setup()
        {
            this.statusBar = new Mock<IStatusBarControlViewModel>();
            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.TransferMappedThingsToHub()).Returns(Task.CompletedTask);

            this.dstController.Setup(x => x.ParameterVariable).Returns(new Dictionary<ParameterOrOverrideBase, VariableRowViewModel>());
            
            this.dstController.Setup(x => x.DstMapResult)
                .Returns(new ReactiveList<ElementBase>());

            this.dstController.Setup(x => x.HubMapResult)
                .Returns(new ReactiveList<MappedElementDefinitionRowViewModel>());
            
            this.dstController.Setup(x => x.SelectedDstMapResultToTransfer).Returns(new ReactiveList<ParameterOrOverrideBase>());

            this.dstController.Setup(x => x.SelectedHubMapResultToTransfer).Returns(new ReactiveList<MappedElementDefinitionRowViewModel>()
            {
                new MappedElementDefinitionRowViewModel()
            });

            this.exchangeHistoryService = new Mock<IExchangeHistoryService>();

            this.viewModel = new EcosimProTransferControlViewModel(this.dstController.Object, this.statusBar.Object, this.exchangeHistoryService.Object);
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

            this.dstController.Setup(x => x.MappingDirection)
                .Returns(MappingDirection.FromDstToHub);

            this.dstController.Setup(x => x.SelectedDstMapResultToTransfer).Returns(new ReactiveList<ParameterOrOverrideBase>()
            {
                new Parameter()
            });

            this.viewModel.UpdateNumberOfThingsToTransfer();
            Assert.IsTrue(this.viewModel.TransferCommand.CanExecute(null));
            
            Assert.DoesNotThrowAsync(() => this.viewModel.TransferCommand.ExecuteAsyncTask(null));
            
            this.dstController.Setup(x => x.TransferMappedThingsToHub())
                .Throws<InvalidOperationException>();

            Assert.ThrowsAsync<InvalidOperationException>(() => this.viewModel.TransferCommand.ExecuteAsyncTask(null));
            this.dstController.Verify(x => x.TransferMappedThingsToHub(), Times.Exactly(2));
            this.statusBar.Verify(x => x.Append(It.IsAny<string>(), StatusBarMessageSeverity.Error), Times.Once);

            this.exchangeHistoryService.Verify(x => x.Write(), Times.Once);
        }

        [Test]
        public void VerifyCancelCommand()
        {
            this.dstController.Setup(x => x.DstMapResult).Returns(new ReactiveList<ElementBase>()
            {
                new ElementDefinition()
            });

            this.dstController.Setup(x => x.HubMapResult).Returns(new ReactiveList<MappedElementDefinitionRowViewModel>()
            {
                new MappedElementDefinitionRowViewModel()
            });

            Assert.IsFalse(this.viewModel.CancelCommand.CanExecute(null));
            this.viewModel.AreThereAnyTransferInProgress = true;
            Assert.IsTrue(this.viewModel.CancelCommand.CanExecute(null));
            Assert.IsNotEmpty(this.dstController.Object.HubMapResult);
            Assert.IsNotEmpty(this.dstController.Object.DstMapResult);
            Assert.DoesNotThrow(() => this.viewModel.CancelCommand.ExecuteAsyncTask(null));
            Assert.IsEmpty(this.dstController.Object.HubMapResult);
            Assert.IsEmpty(this.dstController.Object.DstMapResult);
            Assert.IsEmpty(this.dstController.Object.ParameterVariable);
        }   
    }
}
