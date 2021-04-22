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
    using System.Collections.Generic;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Threading;

    using Autofac;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using CDP4Dal;
    using CDP4Dal.Permission;

    using DEHPCommon;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.Services.ObjectBrowserTreeSelectorService;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.PublicationBrowser;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;
    using DEHPCommon.UserInterfaces.Views;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel;
    using DEHPEcosimPro.ViewModel.Dialogs;
    using DEHPEcosimPro.ViewModel.Dialogs.Interfaces;
    using DEHPEcosimPro.ViewModel.Interfaces;
    using DEHPEcosimPro.ViewModel.Rows;
    using DEHPEcosimPro.Views.Dialogs;

    using DevExpress.Xpf.Core;
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
        private HubDataSourceViewModel viewModel;
        private Mock<IHubBrowserHeaderViewModel> hubBrowserHeader;
        private Mock<IDstController> dstController;
        private Iteration iteration;
        private Person person;
        private Participant participant;
        private DomainOfExpertise domain;
        private Mock<ISession> session;
        private Mock<IHubSessionControlViewModel> sessionControl;

        [SetUp]
        public void Setup()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;
            this.navigationService = new Mock<INavigationService>();
            this.navigationService.Setup(x => x.ShowDialog<Login>());

            this.hubController = new Mock<IHubController>();
            this.hubController.Setup(x => x.IsSessionOpen).Returns(false);
            this.hubController.Setup(x => x.Close());

            this.session = new Mock<ISession>();

            var permissionService = new Mock<IPermissionService>();
            permissionService.Setup(x => x.Session).Returns(this.session.Object);
            permissionService.Setup(x => x.CanRead(It.IsAny<Thing>())).Returns(true);
            permissionService.Setup(x => x.CanWrite(It.IsAny<Thing>())).Returns(true);
            this.session.Setup(x => x.PermissionService).Returns(permissionService.Object);
            
            this.domain = new DomainOfExpertise(Guid.NewGuid(), null, null)
            {
                Name = "t",
                ShortName = "e"
            };

            this.person = new Person(Guid.NewGuid(), null, null) { GivenName = "test", DefaultDomain = this.domain};
            
            this.session.Setup(x => x.ActivePerson).Returns(this.person);

            this.participant = new Participant(Guid.NewGuid(), null, null)
            {
                Person = this.person
            };

            var engineeringModelSetup = new EngineeringModelSetup(Guid.NewGuid(), null, null)
            {
                Participant = { this.participant },
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

            this.session.Setup(x => x.OpenIterations).Returns(
                new Dictionary<Iteration, Tuple<DomainOfExpertise, Participant>>()
                {
                    {
                        this.iteration,
                        new Tuple<DomainOfExpertise, Participant>(this.domain, this.participant)
                    }
                });

            this.hubController.Setup(x => x.Session).Returns(this.session.Object);

            this.objectBrowser = new Mock<IObjectBrowserViewModel>();
            this.objectBrowser.Setup(x => x.CanMap).Returns(new Mock<IObservable<bool>>().Object);
            this.objectBrowser.Setup(x => x.MapCommand).Returns(ReactiveCommand.Create());
            this.objectBrowser.Setup(x => x.Things).Returns(new ReactiveList<BrowserViewModelBase>());
            this.objectBrowser.Setup(x => x.SelectedThings).Returns(new ReactiveList<object>());
            
            this.publicationBrowser = new Mock<IPublicationBrowserViewModel>();

            this.treeSelectorService = new Mock<IObjectBrowserTreeSelectorService>();

            this.hubBrowserHeader = new Mock<IHubBrowserHeaderViewModel>();
            this.dstController = new Mock<IDstController>();
            this.sessionControl = new Mock<IHubSessionControlViewModel>();

            this.viewModel = new HubDataSourceViewModel(this.navigationService.Object, this.hubController.Object, this.objectBrowser.Object, this.publicationBrowser.Object, 
                this.treeSelectorService.Object, this.hubBrowserHeader.Object, this.dstController.Object, this.sessionControl.Object);
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
            this.dstController.Setup(x => x.DstMapResult)
                .Returns(new ReactiveList<ElementBase>());

            this.dstController.Setup(x => x.HubMapResult)
                .Returns(new ReactiveList<MappedElementDefinitionRowViewModel>());

            Assert.IsTrue(this.viewModel.ConnectCommand.CanExecute(null));
            this.hubController.Setup(x => x.IsSessionOpen).Returns(true);
            this.viewModel.ConnectCommand.Execute(null); //
            Assert.AreEqual("Connect", this.viewModel.ConnectButtonText);
            this.hubController.Setup(x => x.IsSessionOpen).Returns(false);
            this.viewModel.ConnectCommand.Execute(null);
            this.hubController.Setup(x => x.IsSessionOpen).Returns(true);
            
            this.dstController.Setup(x => x.DstMapResult).Returns(new ReactiveList<ElementBase>()
            {
                new ElementDefinition()
            });

            this.navigationService.Setup(x => x.ShowDxDialog<DXDialogWindow>()).Returns(true);
            this.viewModel.ConnectCommand.Execute(null);
            this.navigationService.Setup(x => x.ShowDxDialog<DXDialogWindow>()).Returns(false);
            this.viewModel.ConnectCommand.Execute(null);
            this.hubController.Verify(x => x.Close(), Times.Exactly(3));
            this.navigationService.Verify(x => x.ShowDialog<Login>(), Times.Once);
        }

        [Test]
        public void VerifyMapCommand()
        {
            var dialog = new Mock<IHubMappingConfigurationDialogViewModel>();

            dialog.Setup(x => x.Elements)
                .Returns(new ReactiveList<ElementDefinitionRowViewModel>());

            var container = new ContainerBuilder();
            container.RegisterInstance(dialog.Object).As<IHubMappingConfigurationDialogViewModel>();

            AppContainer.Container = container.Build();

            this.objectBrowser.Setup(x => x.SelectedThings)
                .Returns(new ReactiveList<object>());
            
            var elementDefinition = new ElementDefinition(Guid.NewGuid(), null, new Uri("t://s.t"))
            {
                Parameter =
                {
                    new Parameter(Guid.NewGuid(), null, new Uri("t://s.t"))
                    {
                        ParameterType = new DateTimeParameterType(Guid.NewGuid(), null, new Uri("t://s.t")),
                        ValueSet = { new ParameterValueSet(Guid.NewGuid(), null, new Uri("t://s.t")) }
                    }
                }
            };
            
            this.iteration.Element.Add(elementDefinition);

            var elementRow = new ElementDefinitionRowViewModel(elementDefinition, 
                new DomainOfExpertise(), this.session.Object, null);
            
            this.viewModel.ObjectBrowser.SelectedThings.Add(
                elementRow);

            Assert.IsTrue(this.viewModel.ObjectBrowser.MapCommand.CanExecute(null));
            Assert.DoesNotThrow(() => this.viewModel.ObjectBrowser.MapCommand.Execute(null));
            Assert.DoesNotThrow(() => this.viewModel.MapCommandExecute());
            
            this.navigationService.Verify(x => 
                x.ShowDialog<HubMappingConfigurationDialog, IHubMappingConfigurationDialogViewModel>(It.IsAny<IHubMappingConfigurationDialogViewModel>()),
                Times.Exactly(2));
        }
    }
}
