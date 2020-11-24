﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstDataSourceViewModelTestFixture.cs" company="RHEA System S.A.">
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
    using DEHPCommon.Services.NavigationService;

    using DEHPEcosimPro.DstAdapter;
    using DEHPEcosimPro.ViewModel;
    using DEHPEcosimPro.ViewModel.Interfaces;
    using DEHPEcosimPro.Views.Dialogs;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class DstDataSourceViewModelTestFixture
    {
        private DstDataSourceViewModel viewModel;
        private Mock<INavigationService> navigationService;
        private Mock<IDstBrowserHeaderViewModel> browserHeader;
        private Mock<IDstAdapter> dstAdapter;

        [SetUp]
        public void Setup()
        {
            this.dstAdapter = new Mock<IDstAdapter>();
            this.dstAdapter.Setup(x => x.IsSessionOpen).Returns(true);
            this.dstAdapter.Setup(x => x.CloseSession());

            this.navigationService = new Mock<INavigationService>();
            this.navigationService.Setup(x => x.ShowDialog<DstLogin>());

            this.browserHeader = new Mock<IDstBrowserHeaderViewModel>();

            this.viewModel = new DstDataSourceViewModel(this.navigationService.Object, this.dstAdapter.Object, this.browserHeader.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsNotNull(this.viewModel.DstBrowserHeader);
        }

        [Test]
        public void VerifyConnectCommand()
        {
            Assert.IsTrue(this.viewModel.ConnectCommand.CanExecute(null));
            Assert.AreEqual("Connect", this.viewModel.ConnectButtonText);
            Assert.DoesNotThrow(() => this.viewModel.ConnectCommand.Execute(null));
            this.dstAdapter.Verify(x => x.CloseSession(), Times.Once);
            Assert.AreEqual("Disconnect", this.viewModel.ConnectButtonText);
            this.dstAdapter.Setup(x => x.IsSessionOpen).Returns(false);
            Assert.DoesNotThrow(() => this.viewModel.ConnectCommand.Execute(null));
            this.navigationService.Verify(x => x.ShowDialog<DstLogin>(), Times.Once);
        }
    }
}
