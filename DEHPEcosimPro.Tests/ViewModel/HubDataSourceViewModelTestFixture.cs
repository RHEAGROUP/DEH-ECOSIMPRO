// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubDataSourceViewModelTestFixture.cs" company="RHEA System S.A.">
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
    using System;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Threading;

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.Services.ObjectBrowserTreeSelectorService;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.PublicationBrowser;
    using DEHPCommon.UserInterfaces.Views;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel;
    using DEHPEcosimPro.ViewModel.Interfaces;

    using DevExpress.XtraPrinting.Native;

    using Moq;

    using NUnit.Framework;

    using ReactiveUI;

    [TestFixture, Apartment(ApartmentState.STA)]
    public class HubDataSourceViewModelTestFixture
    {
        private Mock<IHubController> hubController;
        private Mock<INavigationService> navigationService;
        private Mock<IObjectBrowserViewModel> objectBrowser;
        private Mock<IPublicationBrowserViewModel> publicationBrowser;
        private Mock<IObjectBrowserTreeSelectorService> treeSelectorService;
        private IHubDataSourceViewModel viewModel;
        private Mock<IHubBrowserHeaderViewModel> hubBrowserHeader;
        private Mock<IDstController> dstController;

        [SetUp]
        public void Setup()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;
            this.navigationService = new Mock<INavigationService>();
            this.navigationService.Setup(x => x.ShowDialog<Login>());

            this.hubController = new Mock<IHubController>();
            this.hubController.Setup(x => x.IsSessionOpen).Returns(false);
            this.hubController.Setup(x => x.Close());

            this.objectBrowser = new Mock<IObjectBrowserViewModel>();
            this.objectBrowser.Setup(x => x.CanMap).Returns(new Mock<IObservable<bool>>().Object);
            this.objectBrowser.Setup(x => x.MapCommand).Returns(ReactiveCommand.Create());
            this.objectBrowser.Setup(x => x.Things).Returns(new ReactiveList<BrowserViewModelBase>());

            this.publicationBrowser = new Mock<IPublicationBrowserViewModel>();

            this.treeSelectorService = new Mock<IObjectBrowserTreeSelectorService>();

            this.hubBrowserHeader = new Mock<IHubBrowserHeaderViewModel>();
            this.dstController = new Mock<IDstController>();

            this.viewModel = new HubDataSourceViewModel(this.navigationService.Object, this.hubController.Object, this.objectBrowser.Object, this.publicationBrowser.Object, 
                this.treeSelectorService.Object, this.hubBrowserHeader.Object, this.dstController.Object);
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
