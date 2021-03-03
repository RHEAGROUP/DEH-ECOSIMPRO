// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstVariablesControlViewModelTestFixture.cs" company="RHEA System S.A.">
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
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Threading;

    using Autofac;

    using CDP4Common.EngineeringModelData;

    using CDP4Dal;

    using DEHPCommon;
    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel;
    using DEHPEcosimPro.ViewModel.Dialogs;
    using DEHPEcosimPro.ViewModel.Dialogs.Interfaces;
    using DEHPEcosimPro.ViewModel.Rows;
    using DEHPEcosimPro.Views.Dialogs;

    using Moq;

    using NUnit.Framework;

    using Opc.Ua;

    using ReactiveUI;

    [TestFixture, Apartment(ApartmentState.STA)]
    public class DstVariablesControlViewModelTestFixture
    {
        private DstVariablesControlViewModel viewModel;
        private Mock<IDstController> dstController;
        private Mock<INavigationService> navigationService;
        private Mock<IHubController> hubController;
        private Mock<IStatusBarControlViewModel> statusBar;
        private Mock<IDstMappingConfigurationDialogViewModel> mappingConfigurationDialog;

        [SetUp]
        public void Setup()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;

            this.statusBar = new Mock<IStatusBarControlViewModel>();

            this.dstController = new Mock<IDstController>();

            this.dstController.Setup(x => x.AddSubscription(It.IsAny<ReferenceDescription>()));
            this.dstController.Setup(x => x.IsSessionOpen).Returns(true);
            this.dstController.Setup(x => x.ParameterNodeIds).Returns(new Dictionary<ParameterOrOverrideBase, object>());

            this.dstController.Setup(x => x.Variables).Returns(
                new List<(ReferenceDescription Reference, DataValue Value)>()
                {
                    (new ReferenceDescription()
                    {
                        NodeId = new ExpandedNodeId(Guid.NewGuid()), DisplayName = new LocalizedText("", "el.DummyVariable0"),
                    }, new DataValue()),
                    (new ReferenceDescription()
                    {
                        NodeId = new ExpandedNodeId(Guid.NewGuid()), DisplayName = new LocalizedText("", "res0.DummyVariable1")
                    }, new DataValue()),
                    (new ReferenceDescription()
                    {
                        NodeId = new ExpandedNodeId(Guid.NewGuid()), DisplayName = new LocalizedText("", "trans0.Gain.DummyVariable2")
                    }, new DataValue()),
                });

            this.dstController.Setup(x => x.ExternalIdentifierMap).Returns(
                new ExternalIdentifierMap()
                {
                    Correspondence =
                    {
                        new IdCorrespondence() { ExternalId = "trans0"},
                        new IdCorrespondence() { ExternalId = "Gain.DummyVariable2"},
                        new IdCorrespondence() { ExternalId = "res0"},
                    }
                });

            this.dstController.Setup(x => x.MappingDirection).Returns(MappingDirection.FromDstToHub);

            this.mappingConfigurationDialog = new Mock<IDstMappingConfigurationDialogViewModel>();
            this.mappingConfigurationDialog.Setup(x => x.Variables).Returns(new ReactiveList<VariableRowViewModel>());
            this.mappingConfigurationDialog.Setup(x => x.UpdatePropertiesBasedOnMappingConfiguration());

            this.navigationService = new Mock<INavigationService>();
            this.navigationService.Setup(x => x.ShowDialog<DstMappingConfigurationDialog, IDstMappingConfigurationDialogViewModel>(It.IsAny<DstMappingConfigurationDialogViewModel>()));
            this.hubController = new Mock<IHubController>();
            this.hubController.Setup(x => x.OpenIteration).Returns(new Iteration());

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(this.mappingConfigurationDialog.Object).As<IDstMappingConfigurationDialogViewModel>();
            AppContainer.Container = containerBuilder.Build();

            this.viewModel = new DstVariablesControlViewModel(this.dstController.Object, this.navigationService.Object, 
                this.hubController.Object, this.statusBar.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsNotEmpty(this.viewModel.Variables);
            Assert.IsFalse(this.viewModel.IsBusy);
            Assert.IsNotNull(this.viewModel.MapCommand);
        }

        [Test]
        public void VerifyUpdateProperties()
        {
            Assert.AreEqual(3, this.viewModel.Variables.Count);

            this.dstController.Verify(x => x.AddSubscription(It.IsAny<ReferenceDescription>()), Times.Exactly(3));
            
            this.dstController.Setup(x => x.IsSessionOpen).Returns(false);
            this.viewModel.UpdateProperties();

            this.dstController.Verify(x => x.IsSessionOpen, Times.Exactly(4));
            this.dstController.Verify(x => x.ClearSubscriptions(), Times.Exactly(1));
        }

        [Test]
        public void VerifyMapCommandExecute()
        {
            CDPMessageBus.Current.ClearSubscriptions();
            Assert.IsFalse(this.viewModel.MapCommand.CanExecute(null));
            this.viewModel.SelectedThings.Clear();
            this.viewModel.SelectedThing = this.viewModel.Variables.First();
            this.viewModel.InitializeCommands();
            Assert.IsTrue(this.viewModel.MapCommand.CanExecute(null));
            this.viewModel.SelectedThings.AddRange(this.viewModel.Variables);
            this.viewModel.SelectedThing = null;
            this.viewModel.InitializeCommands();
            Assert.IsTrue(this.viewModel.MapCommand.CanExecute(null));

            Assert.DoesNotThrow(() => this.viewModel.MapCommand.Execute(null));

            Assert.IsTrue(this.viewModel.Variables.All(x => x.ChartValues.Count == 1));
            this.mappingConfigurationDialog.Verify(x => x.Variables, Times.Once);
            this.navigationService.Verify(x => x.ShowDialog<DstMappingConfigurationDialog, IDstMappingConfigurationDialogViewModel>(It.IsAny<IDstMappingConfigurationDialogViewModel>()), Times.Once());
        }
    }
}
