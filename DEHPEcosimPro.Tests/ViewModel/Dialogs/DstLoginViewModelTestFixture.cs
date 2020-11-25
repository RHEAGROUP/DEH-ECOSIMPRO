// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstLoginViewModelTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Tests.ViewModel.Dialogs
{
    using System.Reactive.Concurrency;
    using System.Threading.Tasks;

    using DEHPCommon.Enumerators;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.DstAdapter;
    using DEHPEcosimPro.ViewModel.Dialogs;

    using Moq;

    using NUnit.Framework;

    using Opc.Ua;

    using ReactiveUI;

    [TestFixture]
    public class DstLoginViewModelTestFixture
    {
        private Mock<IDstController> dstAdapter;
        private Mock<IStatusBarControlViewModel> statusBar;
        private DstLoginViewModel viewModel;

        [SetUp]
        public void Setup()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;
            this.dstAdapter = new Mock<IDstController>();
            this.dstAdapter.Setup(x => x.IsSessionOpen).Returns(true);
            this.dstAdapter.Setup(x => x.Connect(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IUserIdentity>())).Returns(Task.CompletedTask);

            this.statusBar = new Mock<IStatusBarControlViewModel>();
            this.statusBar.Setup(x => x.Append(It.IsAny<string>(), It.IsAny<StatusBarMessageSeverity>()));

            this.viewModel = new DstLoginViewModel(this.dstAdapter.Object, this.statusBar.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsNull(this.viewModel.CloseWindowBehavior);
            Assert.IsFalse(this.viewModel.LoginSuccessfull);
            Assert.IsNotNull(this.viewModel.LoginCommand);
            Assert.IsNull(this.viewModel.Password);
            Assert.IsNull(this.viewModel.UserName);
            Assert.IsFalse(this.viewModel.RequiresAuthentication);
            Assert.IsNull(this.viewModel.Uri);
        }

        [Test]
        public void VerifyLoginCommand()
        {
            Assert.IsFalse(this.viewModel.LoginCommand.CanExecute(null));
            this.viewModel.Uri = "u://r.l";
            Assert.IsTrue(this.viewModel.LoginCommand.CanExecute(null));
            this.viewModel.RequiresAuthentication = true;
            Assert.IsFalse(this.viewModel.LoginCommand.CanExecute(null));
            this.viewModel.UserName = "name";
            this.viewModel.Password = "pass";
            Assert.IsTrue(this.viewModel.LoginCommand.CanExecute(null));
            this.viewModel.Uri = string.Empty;
            Assert.IsFalse(this.viewModel.LoginCommand.CanExecute(null));
            this.viewModel.Uri = "u://r.l";
            Assert.DoesNotThrowAsync(async () => await this.viewModel.LoginCommand.ExecuteAsyncTask(null));
            
            this.dstAdapter.Verify(
                x => x.Connect(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IUserIdentity>()), Times.Once);
            
            this.statusBar.Verify(x => x.Append(It.IsAny<string>(), StatusBarMessageSeverity.Info), Times.Exactly(2));

            this.dstAdapter.Setup(x => x.Connect(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IUserIdentity>()))
                .Returns(Task.FromException(new TaskCanceledException()));

            Assert.DoesNotThrowAsync(async () => await this.viewModel.LoginCommand.ExecuteAsyncTask(null));

            this.statusBar.Verify(x => x.Append(It.IsAny<string>(), StatusBarMessageSeverity.Info), Times.Exactly(3));
            this.statusBar.Verify(x => x.Append(It.IsAny<string>(), StatusBarMessageSeverity.Error), Times.Once);
        }
    }
}
