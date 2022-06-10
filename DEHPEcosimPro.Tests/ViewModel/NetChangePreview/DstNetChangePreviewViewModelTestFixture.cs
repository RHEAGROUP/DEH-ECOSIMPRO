// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstNetChangePreviewViewModelTestFixture.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2021 RHEA System S.A.
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

namespace DEHPEcosimPro.Tests.ViewModel.NetChangePreview
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using CDP4Dal;
    using CDP4Dal.Permission;

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Events;
    using DEHPEcosimPro.ViewModel.NetChangePreview;
    using DEHPEcosimPro.ViewModel.Rows;

    using Moq;

    using NUnit.Framework;

    using Opc.Ua;

    using ReactiveUI;

    [TestFixture]
    public class DstNetChangePreviewViewModelTestFixture
    {
        private DstNetChangePreviewViewModel viewModel;
        private Mock<IDstController> dstController;
        private Mock<INavigationService> navigation;
        private Mock<IHubController> hubController;
        private Mock<IStatusBarControlViewModel> statusBar;
        private Parameter parameter0;
        private Parameter parameter1;
        private Mock<ISession> session;
        private ReactiveList<MappedElementDefinitionRowViewModel> hubMapResult;
        private ReactiveList<MappedElementDefinitionRowViewModel> selectedHubMapResultToTransfer;

        [SetUp]
        public void Setup()
        {
            this.dstController = new Mock<IDstController>();
            this.navigation = new Mock<INavigationService>();
            this.hubController = new Mock<IHubController>();
            this.statusBar = new Mock<IStatusBarControlViewModel>();
            this.selectedHubMapResultToTransfer = new ReactiveList<MappedElementDefinitionRowViewModel>();

            this.dstController.Setup(x => x.VariableRowViewModels).Returns(new ReactiveList<VariableRowViewModel>()
            {
                new VariableRowViewModel((
                    new ReferenceDescription()
                    {
                        NodeId = new ExpandedNodeId(Guid.NewGuid()),
                        DisplayName = new LocalizedText(string.Empty, "Mos.a")
                    },
                    new DataValue() {Value = 5, ServerTimestamp = DateTime.MinValue})),
                new VariableRowViewModel((
                    new ReferenceDescription()
                    {
                        NodeId = new ExpandedNodeId(Guid.NewGuid()),
                        DisplayName = new LocalizedText(string.Empty, "Mos.a")
                    },
                    new DataValue() {Value = 5, ServerTimestamp = DateTime.MinValue}))
            });

            this.dstController.Setup(x => x.SelectedHubMapResultToTransfer).Returns(this.selectedHubMapResultToTransfer);

            this.parameter0 = new Parameter() { ParameterType = new BooleanParameterType()};
            this.parameter1 = new Parameter() { ParameterType = new TextParameterType()};
            this.hubMapResult = new ReactiveList<MappedElementDefinitionRowViewModel>();
            this.dstController.Setup(x => x.HubMapResult).Returns(this.hubMapResult);

            this.session = new Mock<ISession>();
            this.session.Setup(x => x.PermissionService).Returns(new Mock<IPermissionService>().Object);

            this.viewModel = new DstNetChangePreviewViewModel(this.dstController.Object,
                this.navigation.Object, this.hubController.Object, this.statusBar.Object);

            this.hubMapResult.AddRange(new List<MappedElementDefinitionRowViewModel>()
            {
                new MappedElementDefinitionRowViewModel()
                {
                    SelectedParameter = this.parameter0,
                    SelectedValue = new ValueSetValueRowViewModel(new ParameterValueSet(), "42", new RatioScale()),
                    SelectedVariable = this.viewModel.Variables.First()
                },
                new MappedElementDefinitionRowViewModel()
                {
                    SelectedParameter = this.parameter1,
                    SelectedValue = new ValueSetValueRowViewModel(new ParameterValueSet(), "42", new RatioScale()),
                    SelectedVariable = this.viewModel.Variables.Last()
                }
            });
        }

        [Test]
        public void VerifyComputeValuesAndCommands()
        {
            Assert.DoesNotThrow(() => this.viewModel.UpdateTree(true));
            Assert.DoesNotThrow(() => this.viewModel.UpdateTree(false));
            Assert.DoesNotThrow(() => this.viewModel.SelectAllCommand.Execute(null));
            Assert.DoesNotThrow(() => this.viewModel.DeselectAllCommand.Execute(null));
            this.hubMapResult.Clear();
        }

        [Test]
        public void VerifyUpdateTreeBasedOnSelection()
        {
            this.viewModel.ComputeValues();
            
            Assert.DoesNotThrow(() => CDPMessageBus.Current.SendMessage(new UpdateDstPreviewBasedOnSelectionEvent(new List<ElementDefinitionRowViewModel>(), null, false )));
            Assert.DoesNotThrow(() => CDPMessageBus.Current.SendMessage(new UpdateDstPreviewBasedOnSelectionEvent(new List<ElementDefinitionRowViewModel>(), null, true )));
            
            Assert.DoesNotThrow(() => CDPMessageBus.Current.SendMessage(new UpdateDstPreviewBasedOnSelectionEvent(new List<ElementDefinitionRowViewModel>()
            {
                new ElementDefinitionRowViewModel(
                    new ElementDefinition() { Parameter = {this.parameter0} }, new DomainOfExpertise(), this.session.Object, null )
            }, null, true )));
        }
    }
}
