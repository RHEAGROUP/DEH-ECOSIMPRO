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

    using CDP4Dal;

    using DEHPCommon.Enumerators;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Events;
    using DEHPEcosimPro.ViewModel;

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

        [SetUp]
        public void Setup()
        {
            this.dstController = new Mock<IDstController>();
            this.statusBarViewModel = new Mock<IStatusBarControlViewModel>();

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

            this.dstController.Setup(x => x.ServerAddress).Returns("dummyAddress");
            this.dstController.Setup(x => x.RefreshInterval).Returns(500);
            this.dstController.Setup(x => x.GetServerStartTime()).Returns(new DateTime(2021, 1, 1));
            this.dstController.Setup(x => x.GetCurrentServerTime()).Returns(new DateTime(2021, 1, 3));

            this.viewModel = new DstBrowserHeaderViewModel(this.dstController.Object, this.statusBarViewModel.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsEmpty(this.viewModel.ServerAddress);
            Assert.Zero(this.viewModel.SamplingInterval);
            Assert.Zero(this.viewModel.VariablesCount);
            Assert.IsNull(this.viewModel.ServerStartTime);
            Assert.IsNull(this.viewModel.CurrentServerTime);
        }

        [Test]
        public void VerifyUpdateProperties()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;

            this.dstController.Setup(x => x.IsSessionOpen).Returns(true);
            this.viewModel.UpdateProperties();

            Assert.AreEqual("dummyAddress", this.viewModel.ServerAddress);
            Assert.AreEqual(500, this.viewModel.SamplingInterval);
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

            CDPMessageBus.Current.SendMessage(new OpcVariableChangedEvent { Id = currentServerTimeNodeId.Identifier, Value = DateTime.Today });

            Assert.AreEqual(DateTime.Today, this.viewModel.CurrentServerTime);
        }

        [Test]
        public void VerifyCallRunMethodCommand()
        {
            Assert.IsFalse(this.viewModel.CallRunMethodCommand.CanExecute(null));

            this.dstController.Setup(c => c.IsSessionOpen).Returns(true);
            this.viewModel = new DstBrowserHeaderViewModel(this.dstController.Object, this.statusBarViewModel.Object);

            Assert.IsTrue(this.viewModel.CallRunMethodCommand.CanExecute(null));

            this.viewModel.CallRunMethodCommand.Execute(null);
            this.dstController.Verify(x => x.CallServerMethod("method_run"), Times.Once);

            this.statusBarViewModel.Verify(x => x.Append(It.Is<string>(s => s.Contains("No method was found")), StatusBarMessageSeverity.Error), Times.Once);

            this.dstController.Setup(x => x.CallServerMethod("method_run")).Returns(new List<object>());
            this.viewModel.CallRunMethodCommand.Execute(null);

            this.statusBarViewModel.Verify(x => x.Append(It.Is<string>(s => s.Contains("executed")), StatusBarMessageSeverity.Info), Times.Once);

            this.dstController.Setup(x => x.CallServerMethod("method_run")).Throws(new Exception("dummy-exception"));
            this.viewModel.CallRunMethodCommand.Execute(null);

            this.statusBarViewModel.Verify(x => x.Append(It.Is<string>(s => s.Contains("dummy-exception")), StatusBarMessageSeverity.Error), Times.Once);
        }

        [Test]
        public void VerifyCallResetMethodCommand()
        {
            Assert.IsFalse(this.viewModel.CallResetMethodCommand.CanExecute(null));

            this.dstController.Setup(c => c.IsSessionOpen).Returns(true);
            this.viewModel = new DstBrowserHeaderViewModel(this.dstController.Object, this.statusBarViewModel.Object);

            Assert.IsTrue(this.viewModel.CallResetMethodCommand.CanExecute(null));

            this.viewModel.CallResetMethodCommand.Execute(null);
            this.dstController.Verify(x => x.CallServerMethod("method_reset"), Times.Once);

            this.statusBarViewModel.Verify(x => x.Append(It.Is<string>(s => s.Contains("No method was found")), StatusBarMessageSeverity.Error), Times.Once);

            this.dstController.Setup(x => x.CallServerMethod("method_reset")).Returns(new List<object>());
            this.viewModel.CallResetMethodCommand.Execute(null);

            this.statusBarViewModel.Verify(x => x.Append(It.Is<string>(s => s.Contains("executed")), StatusBarMessageSeverity.Info), Times.Once);

            this.dstController.Setup(x => x.CallServerMethod("method_reset")).Throws(new Exception("dummy-exception"));
            this.viewModel.CallResetMethodCommand.Execute(null);

            this.statusBarViewModel.Verify(x => x.Append(It.Is<string>(s => s.Contains("dummy-exception")), StatusBarMessageSeverity.Error), Times.Once);
        }
    }
}
