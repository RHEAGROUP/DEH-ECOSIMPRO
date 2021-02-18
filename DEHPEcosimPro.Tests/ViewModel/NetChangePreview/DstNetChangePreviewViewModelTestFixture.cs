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

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.DstController;
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

        [SetUp]
        public void Setup()
        {
            this.dstController = new Mock<IDstController>();
            this.navigation = new Mock<INavigationService>();
            this.hubController = new Mock<IHubController>();
            this.statusBar = new Mock<IStatusBarControlViewModel>();

            this.viewModel = new DstNetChangePreviewViewModel(this.dstController.Object, 
                this.navigation.Object, this.hubController.Object, this.statusBar.Object);

            this.viewModel.Variables.AddRange(new[]
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

            this.dstController.Setup(x => x.HubMapResult).Returns(
                new ReactiveList<MappedElementDefinitionRowViewModel>()
                {
                    new MappedElementDefinitionRowViewModel()
                    {
                        SelectedParameter = new Parameter(),
                        SelectedValue = new ValueSetValueRowViewModel(new ParameterValueSet(), "42", new RatioScale()),
                        SelectedVariable = this.viewModel.Variables.First()
                    },
                    new MappedElementDefinitionRowViewModel()
                    {
                        SelectedParameter = new Parameter(),
                        SelectedValue = new ValueSetValueRowViewModel(new ParameterValueSet(), "42", new RatioScale()),
                        SelectedVariable = this.viewModel.Variables.Last()
                    }
                });
        }

        [Test]
        public void VerifyComputeValues()
        {
            Assert.DoesNotThrow(() => this.viewModel.UpdateTree(true));
            Assert.DoesNotThrow(() => this.viewModel.UpdateTree(false));
        }
    }
}
