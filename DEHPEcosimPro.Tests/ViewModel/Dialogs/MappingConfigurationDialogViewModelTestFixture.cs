// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappingConfigurationDialogViewModelTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Tests.ViewModel.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.Behaviors;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel.Dialogs;
    using DEHPEcosimPro.ViewModel.Rows;

    using Moq;

    using NUnit.Framework;

    using Opc.Ua;

    [TestFixture]
    public class MappingConfigurationDialogViewModelTestFixture
    {
        private MappingConfigurationDialogViewModel viewModel;
        private Mock<IDstController> dstController;
        private Mock<IHubController> hubController;
        private VariableRowViewModel variableRowViewModel;
        private Iteration iteration;
        private DomainOfExpertise domain;
        private Mock<ICloseWindowBehavior> closeBehavior;

        [SetUp]
        public void Setup()
        {
            this.domain = new DomainOfExpertise();

            this.iteration = new Iteration()
            {
                Option = { new Option() { Name = "TestOption" }},
                Element = { new ElementDefinition() { Owner = this.domain }}
            };

            this.hubController = new Mock<IHubController>();
            this.hubController.Setup(x => x.OpenIteration).Returns(this.iteration);
            this.hubController.Setup(x => x.CurrentDomainOfExpertise).Returns(this.domain);
            this.hubController.Setup(x => x.GetSiteDirectory()).Returns(new SiteDirectory());

            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.Map(It.IsAny<List<VariableRowViewModel>>())).Returns(true);

            this.variableRowViewModel = new VariableRowViewModel((
                new ReferenceDescription()
                {
                    NodeId = new ExpandedNodeId(Guid.NewGuid(), 4), 
                    NodeClass = NodeClass.Variable, 
                    DisplayName = new LocalizedText(null, "Name")
                },
                new DataValue() {Value = .2}));

            this.viewModel = new MappingConfigurationDialogViewModel(this.hubController.Object, this.dstController.Object)
            {
                Variables = 
                {
                    this.variableRowViewModel
                }
            };

            this.closeBehavior = new Mock<ICloseWindowBehavior>();
            this.closeBehavior.Setup(x => x.Close());
        }

        [Test]
        public void VerifyProperty()
        {
            Assert.IsNull(this.viewModel.CloseWindowBehavior);
            Assert.IsNull(this.viewModel.SelectedThing);
            Assert.IsFalse(this.viewModel.IsBusy);
            Assert.IsEmpty(this.viewModel.AvailableActualFiniteStates);
            Assert.IsEmpty(this.viewModel.AvailableParameterTypes);
            Assert.IsNotEmpty(this.viewModel.AvailableElementDefinitions);
            Assert.IsEmpty(this.viewModel.AvailableElementUsages);
            Assert.IsEmpty(this.viewModel.AvailableParameters);
            Assert.IsNotEmpty(this.viewModel.AvailableOptions);
            Assert.IsNotEmpty(this.viewModel.Variables);
            Assert.IsNotNull(this.viewModel.ContinueCommand);
        }

        [Test]
        public void VerifyContinueCommand()
        {
            Assert.IsFalse(this.viewModel.ContinueCommand.CanExecute(null));
            this.viewModel.SelectedThing = this.variableRowViewModel;
            this.viewModel.Variables.First().SelectedValues.AddRange(this.viewModel.Variables.First().Values);
            Assert.IsTrue(this.viewModel.ContinueCommand.CanExecute(null));

            this.viewModel.CloseWindowBehavior = this.closeBehavior.Object;
            this.viewModel.ContinueCommand.Execute(null);
            this.dstController.Setup(x => x.Map(It.IsAny<List<VariableRowViewModel>>())).Returns(false);
            this.viewModel.ContinueCommand.Execute(null);
            this.dstController.Setup(x => x.Map(It.IsAny<List<VariableRowViewModel>>())).Throws<InvalidOperationException>();
            this.viewModel.ContinueCommand.Execute(null);

            this.closeBehavior.Verify(x => x.Close(), Times.Once);
            this.dstController.Verify(x => x.Map(It.IsAny<List<VariableRowViewModel>>()), Times.Exactly(3));
        }
    }
}
