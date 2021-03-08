// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindowViewModelTestFixture.cs" company="RHEA System S.A.">
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
    using Autofac;

    using DEHPCommon;
    using DEHPCommon.Enumerators;
    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.NetChangePreview.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Services.OpcConnector;
    using DEHPEcosimPro.Services.OpcConnector.Interfaces;
    using DEHPEcosimPro.ViewModel;
    using DEHPEcosimPro.ViewModel.Interfaces;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class MainWindowViewModelTestFixture
    {
        private Mock<IStatusBarControlViewModel> statusBarViewModel;
        private Mock<IHubDataSourceViewModel> hubDataSourceViewModel;
        private Mock<IDstDataSourceViewModel> dstDataSourceViewModel;
        private Mock<IHubNetChangePreviewViewModel> hubNetChangePreviewViewModel;
        private Mock<IDstController> dstController;
        private MainWindowViewModel viewModel;
        private Mock<ITransferControlViewModel> transferControlViewModel;
        private Mock<IDstNetChangePreviewViewModel> dstNetChangePreviewViewModel;
        private Mock<IMappingViewModel> mappingViewModel;

        [SetUp]
        public void Setup()
        {
            this.statusBarViewModel = new Mock<IStatusBarControlViewModel>();
            this.hubDataSourceViewModel = new Mock<IHubDataSourceViewModel>();
            this.dstDataSourceViewModel = new Mock<IDstDataSourceViewModel>();
            this.hubNetChangePreviewViewModel = new Mock<IHubNetChangePreviewViewModel>();
            this.dstNetChangePreviewViewModel = new Mock<IDstNetChangePreviewViewModel>();
            this.transferControlViewModel = new Mock<ITransferControlViewModel>();
            this.dstController = new Mock<IDstController>();
            this.mappingViewModel = new Mock<IMappingViewModel>();

            this.viewModel = new MainWindowViewModel(this.hubDataSourceViewModel.Object, this.dstDataSourceViewModel.Object,
                this.statusBarViewModel.Object, this.hubNetChangePreviewViewModel.Object, this.dstNetChangePreviewViewModel.Object,
                this.dstController.Object, this.transferControlViewModel.Object, this.mappingViewModel.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsNotNull(this.viewModel.HubDataSourceViewModel);
            Assert.IsNotNull(this.viewModel.DstSourceViewModel);
            Assert.IsNotNull(this.viewModel.StatusBarControlViewModel);
            Assert.IsNotNull(this.viewModel.HubNetChangePreviewViewModel);
            Assert.IsNull(this.viewModel.SwitchPanelBehavior);
            Assert.IsNotNull(this.viewModel.ChangeMappingDirection);
            Assert.IsNotNull(this.viewModel.TransferControlViewModel);
        }

        [Test]
        public void VerifyChangeMappingDirectionCommand()
        {
            Assert.IsTrue(this.viewModel.ChangeMappingDirection.CanExecute(null));
            this.viewModel.ChangeMappingDirection.Execute(null);
            Assert.AreEqual(MappingDirection.FromDstToHub, this.dstController.Object.MappingDirection);
            var mock = new Mock<ISwitchLayoutPanelOrderBehavior>();
            mock.Setup(x => x.Switch());
            mock.Setup(x => x.MappingDirection).Returns(MappingDirection.FromHubToDst);
            this.viewModel.SwitchPanelBehavior = mock.Object;

            this.viewModel.ChangeMappingDirection.Execute(null);
            Assert.AreEqual(MappingDirection.FromDstToHub, this.dstController.Object.MappingDirection);
        }
    }
}
