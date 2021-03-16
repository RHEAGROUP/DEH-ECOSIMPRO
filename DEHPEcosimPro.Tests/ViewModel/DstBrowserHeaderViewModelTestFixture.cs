// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstVariablesControlViewModelTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Tests.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Concurrency;
    using System.Threading.Tasks;
    using System.Windows.Threading;

    using CDP4Dal;

    using DEHPCommon.Enumerators;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Events;
    using DEHPEcosimPro.ViewModel;
    using DEHPEcosimPro.Views.Dialogs;

    using DevExpress.Xpf.Core;

    using Moq;

    using NUnit.Framework;

    using Opc.Ua;
    using Opc.Ua.Client;

    using ReactiveUI;

    [TestFixture]
    public class DstBrowserHeaderViewModelTestFixture
    {
        private DstBrowserHeaderViewModel viewModel;
        private Mock<IDstController> dstController;
        private Mock<IStatusBarControlViewModel> statusBarViewModel;
        private Mock<INavigationService> navigationService;

        [SetUp]
        public void Setup()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;

            this.dstController = new Mock<IDstController>();
            this.statusBarViewModel = new Mock<IStatusBarControlViewModel>(); 

            this.statusBarViewModel.Setup(x =>
                x.Append(It.IsAny<string>(), StatusBarMessageSeverity.Error));

            this.statusBarViewModel.Setup(x =>
                x.Append(It.IsAny<string>(), StatusBarMessageSeverity.Info));

            this.dstController.Setup(x => x.IsSessionOpen).Returns(false);

            this.dstController.Setup(x => x.AddSubscription(It.IsAny<ReferenceDescription>()));

            this.dstController.Setup(x => x.Variables).Returns(
                new List<(ReferenceDescription Reference, DataValue Value)>()
                {
                    (new ReferenceDescription()
                    {
                        NodeId = new ExpandedNodeId(Guid.NewGuid()), DisplayName = new LocalizedText("", "DummyVariable0")
                    }, new DataValue()),
                    (new ReferenceDescription()
                    {
                        NodeId = new ExpandedNodeId(Guid.NewGuid()), DisplayName = new LocalizedText("", "DummyVariable1")
                    }, new DataValue()),
                    (new ReferenceDescription()
                    {
                        NodeId = new ExpandedNodeId(Guid.NewGuid()), DisplayName = new LocalizedText("", "DummyVariable2")
                    }, new DataValue()),
                });

            this.dstController.Setup(x => x.References)
                .Returns(new List<ReferenceDescription>()
                {
                    new ReferenceDescription() {BrowseName = new QualifiedName("TSTOP")},
                    new ReferenceDescription() {BrowseName = new QualifiedName("CINT")}
                });

            this.dstController.Setup(x => x.WriteToDst(It.IsAny<NodeId>(), It.IsAny<double>())).Returns(true);

            this.dstController.Setup(x => x.ServerAddress).Returns("dummyAddress");
            this.dstController.Setup(x => x.RefreshInterval).Returns(500);
            this.dstController.Setup(x => x.GetServerStartTime()).Returns(new DateTime(2021, 1, 1));
            this.dstController.Setup(x => x.GetCurrentServerTime()).Returns(new DateTime(2021, 1, 3));
            this.dstController.Setup(x => x.TimeNodeId).Returns(new NodeId(Guid.NewGuid()));
            this.dstController.Setup(x => x.GetNextExperimentStep());

            this.navigationService = new Mock<INavigationService>();
            this.navigationService.Setup(x => x.ShowDxDialog<DXDialogWindow>()).Returns(true);

            this.viewModel = new DstBrowserHeaderViewModel(this.dstController.Object, this.statusBarViewModel.Object, this.navigationService.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsEmpty(this.viewModel.ServerAddress);
            Assert.AreEqual(0.01, this.viewModel.SelectedStepping);
            Assert.AreEqual(5, this.viewModel.SelectedStopStep);
            Assert.Zero(this.viewModel.VariablesCount);
            Assert.IsNull(this.viewModel.ServerStartTime);
            Assert.IsNull(this.viewModel.CurrentServerTime);
            Assert.IsFalse(this.viewModel.IsExperimentRunning);
            Assert.IsFalse(this.viewModel.AreTimeStepAnStepTimeEditable);
            Assert.IsFalse(this.viewModel.CanRunExperiment);
            Assert.IsNotNull(this.viewModel.CallResetMethodCommand);
            Assert.IsNotNull(this.viewModel.CallRunMethodCommand);
        }

        [Test]
        public void VerifyUpdateProperties()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;

            this.dstController.Setup(x => x.IsSessionOpen).Returns(true);
            this.viewModel.UpdateProperties();

            Assert.AreEqual("dummyAddress", this.viewModel.ServerAddress);
            Assert.AreEqual(3, this.viewModel.VariablesCount);
            Assert.AreEqual(new DateTime(2021, 1, 1), this.viewModel.ServerStartTime);
            Assert.AreEqual(new DateTime(2021, 1, 3), this.viewModel.CurrentServerTime);

            this.dstController.Verify(x => x.AddSubscription(It.IsAny<NodeId>()), Times.Exactly(1));

            this.dstController.Setup(x => x.IsSessionOpen).Returns(false);
            this.viewModel.UpdateProperties();

            this.dstController.Verify(x => x.ClearSubscriptions(), Times.Exactly(2));
        }

        [Test]
        public void VerifyCurrentServerTimeSubscription()
        {
            var currentServerTimeNodeId = new NodeId(Variables.Server_ServerStatus_CurrentTime);

            CDPMessageBus.Current.SendMessage(new OpcVariableChangedEvent() { Id = currentServerTimeNodeId.Identifier, Value = DateTime.Today });

            Assert.AreEqual(DateTime.Today, this.viewModel.CurrentServerTime);
        }


        [Test]
        public void VerifyRunEperimentCanExecute()
        {
            Assert.IsFalse(this.viewModel.CallRunMethodCommand.CanExecute(null));
            this.dstController.Setup(c => c.IsSessionOpen).Returns(true);
            this.viewModel = new DstBrowserHeaderViewModel(this.dstController.Object, this.statusBarViewModel.Object, this.navigationService.Object);
            Assert.IsTrue(this.viewModel.CallRunMethodCommand.CanExecute(null));
            this.viewModel.SelectedStepping = 0;
            Assert.IsFalse(this.viewModel.CallRunMethodCommand.CanExecute(null));
            this.viewModel.SelectedStepping = 0.1;
            this.viewModel.SelectedStopStep = 0;
            Assert.IsFalse(this.viewModel.CallRunMethodCommand.CanExecute(null));
        }

        [Test]
        public async Task VerifyCallRunMethodCommand()
        {
            this.viewModel.SelectedStepping = 0.1;
            this.viewModel.SelectedStopStep = 0;
            
            this.viewModel.CallRunMethodCommand.Execute(null);

            this.viewModel.IsExperimentRunning = true;
            this.viewModel.CallRunMethodCommand.Execute(null);

            this.viewModel.IsExperimentRunning = false;
            this.viewModel.ExperimentTime = 10;
            this.viewModel.SelectedStopStep = 10;

            this.navigationService.Setup(x =>
                x.ShowDxDialog<ExperimentResetAndReRunConfirmDialog>()).Returns(true);

            this.viewModel.CallRunMethodCommand.Execute(null);

            this.viewModel.ExperimentTime = 10;
            this.viewModel.SelectedStopStep = 10;
            
            this.navigationService.Setup(x =>
                x.ShowDxDialog<ExperimentResetAndReRunConfirmDialog>()).Returns(false);
            
            this.viewModel.CallRunMethodCommand.Execute(null);

            this.viewModel.ExperimentTime = 10;
            this.viewModel.SelectedStopStep = 10;
            
            this.navigationService.Setup(x =>
                x.ShowDxDialog<ExperimentResetAndReRunConfirmDialog>()).Returns(default(bool?));
            
            this.viewModel.CallRunMethodCommand.Execute(null);

            this.viewModel.IsExperimentRunning = false;
            this.viewModel.ExperimentTime = 10;
            this.viewModel.SelectedStopStep = 15;
            this.viewModel.SelectedStepping = 1;

            this.viewModel.CallRunMethodCommand.Execute(null);

            await Task.Delay(100);

            this.dstController.Verify(x => x.GetNextExperimentStep(), Times.Exactly(6));
            this.dstController.Setup(x => x.GetNextExperimentStep()).Throws<InvalidOperationException>();
            
            Assert.DoesNotThrow(() => this.viewModel.CallRunMethodCommand.Execute(null));
            
            this.statusBarViewModel.Verify(x => x.Append(It.IsAny<string>(), StatusBarMessageSeverity.Error));
            this.dstController.Verify(x => x.GetNextExperimentStep(), Times.Exactly(6));
            this.dstController.Verify(x => x.WriteToDst(It.IsAny<NodeId>(), It.IsAny<double>()), Times.Exactly(6));
        }

        [Test]
        public void VerifyCallResetMethodCommand()
        {
            Assert.IsFalse(this.viewModel.CallResetMethodCommand.CanExecute(null));

            this.dstController.Setup(c => c.IsSessionOpen).Returns(true);
            this.viewModel = new DstBrowserHeaderViewModel(this.dstController.Object, this.statusBarViewModel.Object, this.navigationService.Object);

            Assert.IsTrue(this.viewModel.CallResetMethodCommand.CanExecute(null));
            this.navigationService.Setup(x => x.ShowDxDialog<DXDialogWindow>()).Returns(true);
            this.dstController.Setup(x => x.CallServerMethod("method_reset")).Returns(default(IList<object>));
            this.viewModel.Reset();

            this.dstController.Setup(x => x.CallServerMethod("method_reset")).Returns(new List<object>());
            this.viewModel.Reset();

            this.navigationService.Setup(x => x.ShowDxDialog<DXDialogWindow>()).Returns(false);
            this.viewModel.Reset();

            this.navigationService.Setup(x => x.ShowDxDialog<DXDialogWindow>()).Returns(new bool?());
            this.viewModel.Reset();

            this.viewModel.Reset(false);

            this.navigationService.Setup(x => x.ShowDxDialog<DXDialogWindow>()).Throws<InvalidOperationException>();
            Assert.DoesNotThrow(() => this.viewModel.Reset());
            
            this.statusBarViewModel.Verify(x => 
                x.Append(It.IsAny<string>(), StatusBarMessageSeverity.Error), Times.Exactly(2));

            this.statusBarViewModel.Verify(x => 
                x.Append(It.IsAny<string>(), StatusBarMessageSeverity.Info), Times.Exactly(2));

            this.dstController.Verify(x => x.ResetVariables(), Times.Exactly(2));
            this.dstController.Verify(x => x.ReTransferMappedThingsToDst(), Times.Exactly(2));
            this.dstController.Verify(x => x.CallServerMethod("method_reset"), Times.Exactly(3));
        }
    }
}
