// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataSourceViewModelTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Tests.ViewModel
{
    using System.Reactive.Concurrency;
    using System.Threading;

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.Views;

    using DEHPEcosimPro.ViewModel;
    using DEHPEcosimPro.ViewModel.Interfaces;

    using Moq;

    using NUnit.Framework;

    using ReactiveUI;

    [TestFixture, Apartment(ApartmentState.STA)]
    public class DataSourceViewModelTestFixture
    {
        private Mock<IHubController> hubController;
        private Mock<INavigationService> navigationService;
        private IDataSourceViewModel viewModel;

        [SetUp]
        public void Setup()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;
            this.navigationService = new Mock<INavigationService>();
            this.navigationService.Setup(x => x.ShowDialog<Login>());
            this.hubController = new Mock<IHubController>();
            this.hubController.Setup(x => x.IsSessionOpen).Returns(false);
            this.hubController.Setup(x => x.Close());
            this.viewModel = new DataSourceViewModel(this.navigationService.Object, this.hubController.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsNotNull(this.viewModel.ConnectCommand);
            Assert.AreEqual("Connect", this.viewModel.ConnectButtonText);
        }

        [Test]
        public void VerifyConnectCommand()
        {
            Assert.IsTrue(this.viewModel.ConnectCommand.CanExecute(null));
            this.hubController.Setup(x => x.IsSessionOpen).Returns(true);
            this.viewModel.ConnectCommand.Execute(null);
            Assert.AreEqual("Disconnect", this.viewModel.ConnectButtonText);
            this.hubController.Setup(x => x.IsSessionOpen).Returns(false);
            this.viewModel.ConnectCommand.Execute(null);
            Assert.AreEqual("Connect", this.viewModel.ConnectButtonText);

            this.hubController.Verify(x => x.Close(), Times.Once);
            this.navigationService.Verify(x => x.ShowDialog<Login>(), Times.Once);
        }
    }
}
