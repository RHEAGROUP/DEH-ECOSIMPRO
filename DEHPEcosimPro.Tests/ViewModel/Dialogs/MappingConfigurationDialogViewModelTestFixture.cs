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
    using System.Threading.Tasks;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel.Dialogs;
    using DEHPEcosimPro.ViewModel.Rows;

    using Moq;

    using NUnit.Framework;

    using Opc.Ua;

    [TestFixture]
    public class MappingConfigurationDialogViewModelTestFixture
    {
        private DstMappingConfigurationDialogViewModel viewModel;
        private Mock<IDstController> dstController;
        private Mock<IHubController> hubController;
        private List<VariableRowViewModel> variableRowViewModels;
        private Iteration iteration;
        private DomainOfExpertise domain;
        private Mock<ICloseWindowBehavior> closeBehavior;
        private Mock<IStatusBarControlViewModel> statusBar;
        private SampledFunctionParameterType parameterType;
        private ModelReferenceDataLibrary modelReferenceDataLibrary;

        [SetUp]
        public void Setup()
        {
            this.domain = new DomainOfExpertise();

            this.modelReferenceDataLibrary = new ModelReferenceDataLibrary();

            this.iteration = new Iteration()
            {
                Element = { new ElementDefinition() { Owner = this.domain } },
                Option = { new Option() },
                Container = new EngineeringModel()
                {
                    EngineeringModelSetup = new EngineeringModelSetup()
                    {
                        RequiredRdl = { modelReferenceDataLibrary },
                        Container = new SiteReferenceDataLibrary()
                        {
                            Container = new SiteDirectory()
                        }
                    }
                }
            };
            
            this.hubController = new Mock<IHubController>();
            this.hubController.Setup(x => x.OpenIteration).Returns(this.iteration);
            this.hubController.Setup(x => x.CurrentDomainOfExpertise).Returns(this.domain);
            this.hubController.Setup(x => x.GetSiteDirectory()).Returns(new SiteDirectory());
            
            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.Map(It.IsAny<List<VariableRowViewModel>>()));

            this.variableRowViewModels = new List<VariableRowViewModel> 
            { 
                new VariableRowViewModel(
                    (new ReferenceDescription()
                    {
                        NodeId = new ExpandedNodeId(Guid.NewGuid()),
                        DisplayName = new LocalizedText("", "el.DummyVariable0")
                    }, new DataValue() { Value = .2 })),

                new VariableRowViewModel(
                    (new ReferenceDescription()
                    {
                        NodeId = new ExpandedNodeId(Guid.NewGuid()),
                        DisplayName = new LocalizedText("", "res0.DummyVariable1")
                    }, new DataValue())),

                new VariableRowViewModel(
                    (new ReferenceDescription()
                    {
                        NodeId = new ExpandedNodeId(Guid.NewGuid()),
                        DisplayName = new LocalizedText("", "trans0.Gain.DummyVariable2")
                    }, new DataValue()))
            };

            this.parameterType = new SampledFunctionParameterType()
            {
                Name = "TextXQuantity",
                IndependentParameterType =
                {
                    new IndependentParameterTypeAssignment()
                    {
                        ParameterType = new TextParameterType()
                        {
                            Name = "IndependentText"
                        }
                    }
                },

                DependentParameterType =
                {
                    new DependentParameterTypeAssignment()
                    {
                        ParameterType = new SimpleQuantityKind()
                        {
                            Name = "DependentQuantityKing"
                        }
                    }
                }
            };

            var invalidParameterType = new SampledFunctionParameterType()
            {
                Name = "TextXQuantity",
                IndependentParameterType =
                {
                    new IndependentParameterTypeAssignment()
                    {
                        ParameterType = new CompoundParameterType()
                        {
                            Name = "IndependentText"
                        }
                    }
                },

                DependentParameterType =
                {
                    new DependentParameterTypeAssignment()
                    {
                        ParameterType = new SimpleQuantityKind()
                        {
                            Name = "DependentQuantityKing"
                        }
                    }
                }
            };
            this.modelReferenceDataLibrary.ParameterType.Add(this.parameterType);
            this.modelReferenceDataLibrary.ParameterType.Add(invalidParameterType);
            this.statusBar = new Mock<IStatusBarControlViewModel>();

            this.viewModel = new DstMappingConfigurationDialogViewModel(
            this.hubController.Object, this.dstController.Object, this.statusBar.Object);
            this.viewModel.Initialize();

            this.viewModel.Variables.AddRange(this.variableRowViewModels);

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
            Assert.AreEqual(1, this.viewModel.AvailableParameterTypes.Count);
            Assert.IsNotEmpty(this.viewModel.AvailableElementDefinitions);
            Assert.IsEmpty(this.viewModel.AvailableElementUsages);
            Assert.IsEmpty(this.viewModel.AvailableParameters);
            Assert.IsNotEmpty(this.viewModel.AvailableOptions);
            Assert.IsNotEmpty(this.viewModel.Variables);
            Assert.IsNotEmpty(this.viewModel.TimeSteps);
            Assert.IsNotNull(this.viewModel.ContinueCommand);
        }

        [Test]
        public void VerifyContinueCommand()
        {
            this.viewModel.InitializesCommandsAndObservableSubscriptions();
            Assert.IsFalse(this.viewModel.ContinueCommand.CanExecute(null));
            this.viewModel.SelectedThing = this.variableRowViewModels.First();
            this.viewModel.Variables.First().SelectedValues.AddRange(this.variableRowViewModels.First().Values);
            this.viewModel.Variables.First().SelectedParameterType = this.parameterType;
            Assert.IsTrue(this.viewModel.ContinueCommand.CanExecute(null));

            this.viewModel.CloseWindowBehavior = this.closeBehavior.Object;
            this.viewModel.ContinueCommand.Execute(null);
            this.dstController.Setup(x => x.Map(It.IsAny<List<VariableRowViewModel>>())).Throws<InvalidOperationException>();
            this.viewModel.ContinueCommand.Execute(null);

            this.closeBehavior.Verify(x => x.Close(), Times.Once);
            this.dstController.Verify(x => x.Map(It.IsAny<List<VariableRowViewModel>>()), Times.Exactly(2));
        }

        [Test]
        public void VerifyUpdatePropertiesBasedOnMappingConfiguration()
        {
            var elementDefinition = new ElementDefinition(Guid.NewGuid(), null, null);
            this.hubController.Setup(x => x.GetThingById(It.IsAny<Guid>(), It.IsAny<Iteration>(), out elementDefinition)).Returns(true);

            var parameter = new Parameter(Guid.NewGuid(), null, null);
            this.hubController.Setup(x => x.GetThingById(It.IsAny<Guid>(), It.IsAny<Iteration>(), out parameter)).Returns(true);

            var option = new Option(Guid.NewGuid(), null, null);
            this.hubController.Setup(x => x.GetThingById(It.IsAny<Guid>(), It.IsAny<Iteration>(), out option)).Returns(true);

            var state = new ActualFiniteState(Guid.NewGuid(), null, null);
            this.hubController.Setup(x => x.GetThingById(It.IsAny<Guid>(), It.IsAny<Iteration>(), out state)).Returns(true);

            var elementUsage = new ElementUsage(Guid.NewGuid(), null, null);
            this.hubController.Setup(x => x.GetThingById(It.IsAny<Guid>(), It.IsAny<Iteration>(), out elementUsage)).Returns(true);

            var correspondences = new List<IdCorrespondence>
            {
                new IdCorrespondence() { ExternalId = "trans0", InternalThing = elementDefinition.Iid},
                new IdCorrespondence() { ExternalId = "res0", InternalThing = parameter.Iid },
                new IdCorrespondence() { ExternalId = "trans0", InternalThing = option.Iid },
                new IdCorrespondence() { ExternalId = "trans0", InternalThing = state.Iid },
                new IdCorrespondence() { ExternalId = "res0", InternalThing = elementUsage.Iid },
            };

            foreach (var variable in this.viewModel.Variables)
            {
                variable.MappingConfigurations.AddRange(
                    correspondences.Where(
                        x => x.ExternalId == variable.ElementName ||
                             x.ExternalId == variable.ParameterName));
            }

            Assert.DoesNotThrow(() => this.viewModel.UpdatePropertiesBasedOnMappingConfiguration());
            this.hubController.Verify(x => x.GetThingById(It.IsAny<Guid>(), It.IsAny<Iteration>(), out It.Ref<Thing>.IsAny), Times.Exactly(5));
        }

        [Test]
        public void VerifyApplyTimeStep()
        {
            Assert.IsNotNull(this.viewModel.ApplyTimeStepOnSelectionCommand);
            Assert.IsTrue(this.viewModel.ApplyTimeStepOnSelectionCommand.CanExecute(null));
            Assert.DoesNotThrow(() => this.viewModel.ApplyTimeStepOnSelectionCommand.Execute(null));
            this.viewModel.SelectedThing = this.viewModel.Variables.First();
            Assert.IsEmpty(this.viewModel.SelectedThing.SelectedValues);
            Assert.DoesNotThrow(() => this.viewModel.ApplyTimeStepOnSelectionCommand.Execute(null));
            Assert.IsNotEmpty(this.viewModel.SelectedThing.SelectedValues);
        }

        [Test]
        public void VerifyUpdateSelectedParameterType()
        {
            this.viewModel.SelectedThing = null;
            Assert.DoesNotThrow(() => this.viewModel.UpdateSelectedParameterType());
            this.viewModel.SelectedThing = this.viewModel.Variables.First();
            var randomParameterType = new BooleanParameterType();
            this.viewModel.SelectedThing.SelectedParameterType = randomParameterType;

            this.viewModel.SelectedThing.SelectedParameter = new Parameter()
            {
                ParameterType = randomParameterType
            };

            Assert.DoesNotThrow(() => this.viewModel.UpdateSelectedParameterType());
            Assert.IsNull(this.viewModel.SelectedThing.SelectedParameterType);

            this.viewModel.SelectedThing.SelectedParameter = new Parameter()
            {
                ParameterType = this.parameterType
            };

            Assert.DoesNotThrow(() => this.viewModel.UpdateSelectedParameterType());
            Assert.AreSame(this.viewModel.SelectedThing.SelectedParameter.ParameterType, this.viewModel.SelectedThing.SelectedParameterType);
        }

        [Test]
        public void VerifyUpdateSelectedParameter()
        {
            this.viewModel.SelectedThing = null;
            Assert.DoesNotThrow(() => this.viewModel.UpdateSelectedParameter());

            this.viewModel.SelectedThing = this.viewModel.Variables.First();
            Assert.DoesNotThrow(() => this.viewModel.UpdateSelectedParameter());

            this.viewModel.SelectedThing.SelectedParameter = new Parameter()
            {
                ParameterType = new BooleanParameterType()
            };

            this.viewModel.SelectedThing.SelectedParameterType = this.parameterType;
            Assert.DoesNotThrow(() => this.viewModel.UpdateSelectedParameter());
            Assert.IsNotNull(this.viewModel.SelectedThing.SelectedParameter);

            this.viewModel.AvailableParameters.Add(new Parameter()
            {
                ParameterType = this.parameterType
            });

            Assert.DoesNotThrow(() => this.viewModel.UpdateSelectedParameter());
            Assert.AreSame(this.viewModel.SelectedThing.SelectedParameterType, this.viewModel.SelectedThing.SelectedParameter.ParameterType);
        }
    }
}
