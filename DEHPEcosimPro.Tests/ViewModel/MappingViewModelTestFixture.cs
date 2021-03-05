// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappingViewModelTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Tests.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.WebSockets;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel;
    using DEHPEcosimPro.ViewModel.Interfaces;
    using DEHPEcosimPro.ViewModel.Rows;

    using Moq;

    using NUnit.Framework;

    using Opc.Ua;

    using ReactiveUI;

    [TestFixture]
    public class MappingViewModelTestFixture
    {
        private Mock<IDstController> dstController;
        private Mock<IDstVariablesControlViewModel> dstVariableViewModel;
        private MappingViewModel viewModel;
        private List<VariableRowViewModel> variableRowViewModels;
        private ElementDefinition element0;
        private Iteration iteration;
        private Parameter parameter0;
        private ElementUsage elementUsage;
        private ReactiveList<ElementBase> dstMapResult;
        private Mock<IHubController> hubController;
        private ReactiveList<MappedElementDefinitionRowViewModel> hubMapResult;

        [SetUp]
        public void Setup()
        {
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

            this.parameter0 = new Parameter(Guid.NewGuid(), null, null)
            {
                ParameterType = new TextParameterType(), ValueSet = 
                {
                    new ParameterValueSet()
                    {
                        Computed = new ValueArray<string>(new []{"-"}),
                        Manual = new ValueArray<string>(new []{"-"}),
                        Reference = new ValueArray<string>(new []{"-"}),
                        Published = new ValueArray<string>(new []{"-"})
                    }
                }
            };

            this.element0 = new ElementDefinition(Guid.NewGuid(), null, null)
            {
                Parameter =
                {
                    this.parameter0,
                    new Parameter(Guid.NewGuid(), null, null),
                }
            };

            this.elementUsage = new ElementUsage(Guid.NewGuid(), null, null)
            {
                ElementDefinition = this.element0,
                ParameterOverride =
                {
                    new ParameterOverride(Guid.NewGuid(), null, null)
                    {
                        Parameter = this.parameter0,
                        ValueSet = 
                        {
                            new ParameterOverrideValueSet()
                            {
                                Computed = new ValueArray<string>(new []{"-"}),
                                Manual = new ValueArray<string>(new []{"-"}),
                                Reference = new ValueArray<string>(new []{"-"}),
                                Published = new ValueArray<string>(new []{"-"})
                            }
                        }
                    }
                } 
            };

            this.element0.ContainedElement.Add(this.elementUsage);

            this.iteration = new Iteration(Guid.NewGuid(), null, null)
            {
                TopElement = this.element0,
            };

            this.iteration.Element.Add(this.element0);

            this.hubController = new Mock<IHubController>();
            this.hubController.Setup(x => x.OpenIteration).Returns(this.iteration);

            this.dstMapResult = new ReactiveList<ElementBase>();
            this.hubMapResult = new ReactiveList<MappedElementDefinitionRowViewModel>();

            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.MappingDirection).Returns(MappingDirection.FromHubToDst);
            this.dstController.Setup(x => x.DstMapResult).Returns(this.dstMapResult);

            this.dstController.Setup(x => x.HubMapResult).Returns(this.hubMapResult);

            this.dstController.Setup(x => x.ParameterNodeIds).Returns(new Dictionary<ParameterOrOverrideBase, object>()
                {
                    { this.parameter0, this.variableRowViewModels.FirstOrDefault()?.Reference.NodeId.Identifier }
                });

            this.dstVariableViewModel = new Mock<IDstVariablesControlViewModel>();

            this.dstVariableViewModel.Setup(x => x.Variables)
                .Returns(new ReactiveList<VariableRowViewModel>(this.variableRowViewModels));

            this.viewModel = new MappingViewModel(this.dstController.Object, this.hubController.Object, this.dstVariableViewModel.Object);
        }

        [Test]
        public void VerifyOnAddingMappingToHub()
        {
            Assert.IsEmpty(this.viewModel.MappingRows);
            this.dstMapResult.Add(this.element0);
            Assert.AreEqual(1,this.viewModel.MappingRows.Count);
            
            var newParameter = new Parameter(Guid.NewGuid(), null, null)
            {
                ParameterType = new TextParameterType(),
                ValueSet =
                {
                    new ParameterValueSet()
                    {
                        Computed = new ValueArray<string>(new []{"-"}),
                        Manual = new ValueArray<string>(new []{"-"}),
                        Reference = new ValueArray<string>(new []{"-"}),
                        Published = new ValueArray<string>(new []{"-"})
                    }
                }
            };

            var newElement = new ElementDefinition(Guid.NewGuid(), null, null)
            {
                Parameter =
                {
                    newParameter,
                    new Parameter(Guid.NewGuid(), null, null),
                }
            };
            
            this.dstController.Setup(x => x.ParameterNodeIds).Returns(new Dictionary<ParameterOrOverrideBase, object>()
            {
                { newParameter, this.variableRowViewModels.FirstOrDefault()?.Reference.NodeId.Identifier }
            });

            this.dstMapResult.Add(newElement);
            Assert.AreEqual(2, this.viewModel.MappingRows.Count);

            var clone = this.element0.Clone(true);
            clone.Iid = Guid.NewGuid();

            clone.Parameter.Add(newParameter);

            this.dstMapResult.Add(clone);
            Assert.AreEqual(3, this.viewModel.MappingRows.Count);

            this.dstController.Setup(x => x.ParameterNodeIds).Returns(new Dictionary<ParameterOrOverrideBase, object>()
            {
                { this.elementUsage.ParameterOverride.First(), this.variableRowViewModels.FirstOrDefault()?.Reference.NodeId.Identifier }
            });

            this.dstMapResult.Add(this.elementUsage);
            Assert.AreEqual(4, this.viewModel.MappingRows.Count);

            this.hubMapResult.Add(new MappedElementDefinitionRowViewModel()
            {
                SelectedValue = new ValueSetValueRowViewModel(new ParameterValueSet(), "8", new RatioScale() ),
                SelectedParameter = this.parameter0,
                IsValid = true,
                SelectedVariable = this.variableRowViewModels.FirstOrDefault()
            });

            Assert.AreEqual(5, this.viewModel.MappingRows.Count);

            this.hubMapResult.Clear();
            Assert.AreEqual(4, this.viewModel.MappingRows.Count);
            this.dstMapResult.Clear();
            Assert.AreEqual(0, this.viewModel.MappingRows.Count);
        }

        [Test]
        public void VerifySwitchDirection()
        {
            this.dstController.Setup(x => x.MappingDirection).Returns(MappingDirection.FromDstToHub);
            this.dstMapResult.Add(this.element0);
            Assert.AreEqual(0, this.viewModel.MappingRows[0].ArrowDirection);
            Assert.AreEqual(0, this.viewModel.MappingRows[0].DstThing.GridColumnIndex);
            Assert.AreEqual(2, this.viewModel.MappingRows[0].HubThing.GridColumnIndex); 
            
            this.hubMapResult.Add(new MappedElementDefinitionRowViewModel()
            {
                SelectedValue = new ValueSetValueRowViewModel(new ParameterValueSet(), "8", new RatioScale()),
                SelectedParameter = this.parameter0,
                IsValid = true,
                SelectedVariable = this.variableRowViewModels.FirstOrDefault()
            });
            
            Assert.AreEqual(180, this.viewModel.MappingRows[1].ArrowDirection);
            Assert.AreEqual(0, this.viewModel.MappingRows[1].DstThing.GridColumnIndex);
            Assert.AreEqual(2, this.viewModel.MappingRows[1].HubThing.GridColumnIndex);

            this.viewModel.UpdateMappingRowsDirection(MappingDirection.FromHubToDst);

            Assert.AreEqual(180, this.viewModel.MappingRows[0].ArrowDirection);
            Assert.AreEqual(2, this.viewModel.MappingRows[0].DstThing.GridColumnIndex);
            Assert.AreEqual(0, this.viewModel.MappingRows[0].HubThing.GridColumnIndex);

            Assert.AreEqual(0, this.viewModel.MappingRows[1].ArrowDirection);
            Assert.AreEqual(2, this.viewModel.MappingRows[1].DstThing.GridColumnIndex);
            Assert.AreEqual(0, this.viewModel.MappingRows[1].HubThing.GridColumnIndex);
        }
    }
}
