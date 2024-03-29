﻿// --------------------------------------------------------------------------------------------------------------------
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

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel;
    using DEHPEcosimPro.ViewModel.Rows;

    using Moq;

    using NUnit.Framework;

    using Opc.Ua;

    using ReactiveUI;

    [TestFixture]
    public class MappingViewModelTestFixture
    {
        private Mock<IDstController> dstController;
        private MappingViewModel viewModel;
        private List<VariableRowViewModel> variableRowViewModels;
        private ElementDefinition element0;
        private Iteration iteration;
        private Parameter parameter0;
        private ElementUsage elementUsage;
        private ReactiveList<ElementBase> dstMapResult;
        private Mock<IHubController> hubController;
        private ReactiveList<MappedElementDefinitionRowViewModel> hubMapResult;
        private Parameter parameter1;

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
                ParameterType = new TextParameterType(){Name = "parameterType0"}, ValueSet = 
                {
                    new ParameterValueSet()
                    {
                        Computed = new ValueArray<string>(new []{"8"}),
                        Manual = new ValueArray<string>(new []{"5"}),
                        Reference = new ValueArray<string>(new []{"3"})
                    }
                }
            };

            this.parameter1 = new Parameter(Guid.NewGuid(), null, null)
            {
                ParameterType = new TextParameterType(){Name = "parameterType1"}, ValueSet = 
                {
                    new ParameterValueSet()
                    {
                        Computed = new ValueArray<string>(new []{"1"}),
                        Manual = new ValueArray<string>(new []{"2"}),
                        Reference = new ValueArray<string>(new []{"3"})
                    }
                }
            };

            this.element0 = new ElementDefinition(Guid.NewGuid(), null, null)
            {
                Name = "element",
                Parameter =
                {
                    this.parameter0, this.parameter1,
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

            this.dstController.Setup(x => x.ParameterVariable).Returns(new Dictionary<ParameterOrOverrideBase, VariableRowViewModel>()
                {
                    { this.parameter0, this.variableRowViewModels.FirstOrDefault()}
                });

            this.viewModel = new MappingViewModel(this.dstController.Object, this.hubController.Object);
        }

        [Test]
        public void VerifyOnAddingMappingToHub()
        {
            Assert.IsEmpty(this.viewModel.MappingRows);
            this.dstMapResult.Add(this.element0);
            Assert.AreEqual(1,this.viewModel.MappingRows.Count);
            
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
                SelectedParameter = this.parameter1,
                IsValid = true,
                SelectedVariable = this.variableRowViewModels.Last()
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
