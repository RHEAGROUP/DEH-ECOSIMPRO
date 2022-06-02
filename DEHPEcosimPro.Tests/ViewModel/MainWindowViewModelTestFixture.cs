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
    using System;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.Views.ExchangeHistory;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Services.MappingConfiguration;
    using DEHPEcosimPro.ViewModel;
    using DEHPEcosimPro.ViewModel.Interfaces;
    using DEHPEcosimPro.Views.Dialogs;

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
        private Mock<INavigationService> navigationService;
        private Mock<IDifferenceViewModel> differenceViewModel;
        private Mock<IHubController> hubController;
        private Mock<IMappingConfigurationService> mappingConfiguration;
        private ExternalIdentifierMap map;
        private Iteration iteration;

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
            this.navigationService = new Mock<INavigationService>();
            this.differenceViewModel = new Mock<IDifferenceViewModel>();
            this.hubController = new Mock<IHubController>();
            this.mappingConfiguration = new Mock<IMappingConfigurationService>();
            this.map = new ExternalIdentifierMap(Guid.NewGuid(), null, null);
            this.mappingConfiguration.Setup(x => x.ExternalIdentifierMap).Returns(this.map);
            this.navigationService.Setup(x => x.ShowDialog<MappingConfigurationServiceDialog>()).Returns(true);

            var person = new Person(Guid.NewGuid(), null, null) { GivenName = "test", DefaultDomain = new DomainOfExpertise() };

            var participant = new Participant(Guid.NewGuid(), null, null)
            {
                Person = person
            };

            var engineeringModelSetup = new EngineeringModelSetup(Guid.NewGuid(), null, null)
            {
                Participant = { participant },
                Name = "est"
            };

            this.iteration = new Iteration(Guid.NewGuid(), null, null)
            {
                IterationSetup = new IterationSetup(Guid.NewGuid(), null, null)
                {
                    IterationNumber = 23,
                    Container = engineeringModelSetup
                },
                Container = new EngineeringModel(Guid.NewGuid(), null, null)
                {
                    EngineeringModelSetup = engineeringModelSetup
                }
            };

            this.iteration.ExternalIdentifierMap.Add(this.map);
            this.hubController.Setup(x => x.OpenIteration).Returns(this.iteration);

            this.viewModel = new MainWindowViewModel(this.hubDataSourceViewModel.Object, this.dstDataSourceViewModel.Object,
                this.statusBarViewModel.Object, this.hubNetChangePreviewViewModel.Object, this.dstNetChangePreviewViewModel.Object,
                this.dstController.Object, this.transferControlViewModel.Object, this.mappingViewModel.Object, this.navigationService.Object,
                this.differenceViewModel.Object,this.hubController.Object, this.mappingConfiguration.Object);
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
            Assert.IsNotNull(this.viewModel.OpenExchangeHistory);
            Assert.IsNotNull(this.viewModel.OpenMappingConfigurationDialog);
            Assert.IsEmpty(this.viewModel.CurrentMappingConfigurationName);
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

        [Test]
        public void VerifyOpenHistoryOfTransfer()
        {
            Assert.IsTrue(this.viewModel.OpenExchangeHistory.CanExecute(null));
            Assert.DoesNotThrow(() => this.viewModel.OpenExchangeHistory.Execute(null));
            this.navigationService.Verify(x => x.ShowDialog<ExchangeHistory>(), Times.Once);
        }

        [Test]
        public void VerifyOpenMappingServiceDialogCommand()
        {
            Assert.IsTrue(this.viewModel.OpenMappingConfigurationDialog.CanExecute(null));
            Assert.DoesNotThrow(() => this.viewModel.OpenMappingConfigurationDialog.Execute(null));
            Assert.IsEmpty(this.viewModel.CurrentMappingConfigurationName);

            this.map.Name = "AName";
            Assert.DoesNotThrow(() => this.viewModel.OpenMappingConfigurationDialog.Execute(null));
            Assert.IsNotEmpty(this.viewModel.CurrentMappingConfigurationName);

            this.navigationService.Verify(x => x.ShowDialog<MappingConfigurationServiceDialog>(), Times.Exactly(2));
            this.dstController.Verify(x => x.ClearMappingCollections(), Times.Exactly(2));
            this.dstController.Verify(x => x.LoadMapping(), Times.Exactly(2));
        }
    }
}
