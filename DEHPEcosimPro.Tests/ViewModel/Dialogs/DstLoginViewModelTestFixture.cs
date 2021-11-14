// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstLoginViewModelTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Tests.ViewModel.Dialogs
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Threading.Tasks;

    using CDP4Common.EngineeringModelData;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserPreferenceHandler.UserPreferenceService;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Services.MappingConfiguration;
    using DEHPEcosimPro.Settings;
    using DEHPEcosimPro.ViewModel.Dialogs;

    using Moq;

    using NUnit.Framework;

    using Opc.Ua;

    using ReactiveUI;

    [TestFixture]
    public class DstLoginViewModelTestFixture
    {
        private Mock<IDstController> dstController;
        private Mock<IHubController> hubController;
        private Mock<IStatusBarControlViewModel> statusBar;
        private Mock<IUserPreferenceService<AppSettings>> userPreferenceService;
        private DstLoginViewModel viewModel;
        private Mock<IMappingConfigurationService> mappingConfigurationService;

        [SetUp]
        public void Setup()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;

            this.mappingConfigurationService = new Mock<IMappingConfigurationService>();

            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.IsSessionOpen).Returns(true);
            this.dstController.Setup(x => x.Connect(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IUserIdentity>(), 1000)).Returns(Task.CompletedTask);

            this.hubController = new Mock<IHubController>();

            this.hubController.Setup(x => x.AvailableExternalIdentifierMap(It.IsAny<string>())).Returns(new List<ExternalIdentifierMap>()
            {
                new ExternalIdentifierMap(), new ExternalIdentifierMap(), new ExternalIdentifierMap()
            });

            this.statusBar = new Mock<IStatusBarControlViewModel>();
            this.statusBar.Setup(x => x.Append(It.IsAny<string>(), It.IsAny<StatusBarMessageSeverity>()));

            this.userPreferenceService = new Mock<IUserPreferenceService<AppSettings>>();
            this.userPreferenceService.SetupProperty(s => s.UserPreferenceSettings, new AppSettings { SavedOpcUris = new List<string>() });

            this.viewModel = new DstLoginViewModel(this.dstController.Object, this.statusBar.Object, 
                this.userPreferenceService.Object, this.hubController.Object, this.mappingConfigurationService.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsFalse(this.viewModel.LoginSuccessful);
            Assert.IsNull(this.viewModel.CloseWindowBehavior);
            
            Assert.IsNotNull(this.viewModel.LoginCommand);
            Assert.IsNotNull(this.viewModel.SaveCurrentUriCommand);

            Assert.IsEmpty(this.viewModel.SavedUris);

            Assert.IsNull(this.viewModel.Uri);
            Assert.IsNull(this.viewModel.Password);
            Assert.IsNull(this.viewModel.UserName);
            Assert.IsFalse(this.viewModel.RequiresAuthentication);
            
            Assert.IsFalse(this.viewModel.CreateNewMappingConfigurationChecked);
            Assert.IsNull(this.viewModel.ExternalIdentifierMapNewName);
            Assert.IsNull(this.viewModel.SelectedExternalIdentifierMap);
            Assert.AreEqual(3, this.viewModel.AvailableExternalIdentifierMap.Count);
        }

        [Test]
        public void VerifySpecifyExternalIdentifierMap()
        {
            this.viewModel.ExternalIdentifierMapNewName = "Experiment0";
            Assert.IsTrue(this.viewModel.CreateNewMappingConfigurationChecked);
            this.viewModel.SelectedExternalIdentifierMap = this.viewModel.AvailableExternalIdentifierMap.First();
            Assert.IsFalse(this.viewModel.CreateNewMappingConfigurationChecked);
            this.viewModel.CreateNewMappingConfigurationChecked = true;
            Assert.IsNull(this.viewModel.SelectedExternalIdentifierMap);
        }

        [Test]
        public void VerifyLoginCommand()
        {
            Assert.IsFalse(this.viewModel.LoginCommand.CanExecute(null));
            this.viewModel.Uri = "u://r.l";
            Assert.IsFalse(this.viewModel.LoginCommand.CanExecute(null));
            this.viewModel.SelectedExternalIdentifierMap = this.viewModel.AvailableExternalIdentifierMap.First();
            Assert.IsTrue(this.viewModel.LoginCommand.CanExecute(null));
            this.viewModel.SelectedExternalIdentifierMap = null;
            this.viewModel.ExternalIdentifierMapNewName = "new Name";
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

            this.dstController.Verify(
                x => x.Connect(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IUserIdentity>(), 1000), Times.Once);

            this.statusBar.Verify(x => x.Append(It.IsAny<string>(), StatusBarMessageSeverity.Info), Times.Exactly(3));

            this.dstController.Setup(x => x.Connect(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IUserIdentity>(), 1000))
                .Returns(Task.FromException(new TaskCanceledException()));

            Assert.ThrowsAsync<TaskCanceledException>(async () => await this.viewModel.LoginCommand.ExecuteAsyncTask(null));

            this.statusBar.Verify(x => x.Append(It.IsAny<string>(), StatusBarMessageSeverity.Info), Times.Exactly(4));
            this.statusBar.Verify(x => x.Append(It.IsAny<string>(), StatusBarMessageSeverity.Error), Times.Once);
        }

        [Test]
        public void VerifyThat_SaveCurrentUriCommand_IsWorkingProperly()
        {
            Assert.IsFalse(this.viewModel.SaveCurrentUriCommand.CanExecute(null));

            this.viewModel.Uri = "u://r.l";
            Assert.IsTrue(this.viewModel.SaveCurrentUriCommand.CanExecute(null));
            
            this.viewModel.SaveCurrentUriCommand.Execute(null);

            Assert.AreEqual(1, this.viewModel.SavedUris.Count);
            CollectionAssert.Contains(this.viewModel.SavedUris, "u://r.l");
            Assert.IsFalse(this.viewModel.SaveCurrentUriCommand.CanExecute(null));

            this.viewModel.Uri = "anotherUrl";
            Assert.IsTrue(this.viewModel.SaveCurrentUriCommand.CanExecute(null));

            this.viewModel.Uri = "u://r.l";
            Assert.IsFalse(this.viewModel.SaveCurrentUriCommand.CanExecute(null));
        }
    }
}
