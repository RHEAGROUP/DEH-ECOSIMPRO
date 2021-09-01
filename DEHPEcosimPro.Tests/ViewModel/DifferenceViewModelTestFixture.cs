// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DifferenceViewModelTestFixture.cs" company="RHEA System S.A.">
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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;
    using CDP4Dal;
    using CDP4Dal.Permission;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Events;
    using DEHPEcosimPro.ViewModel;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class DifferenceViewModelTestFixture
    {
        #region Properties
        private DifferenceViewModel viewModel;
        private Mock<IHubController> hubController;
        private Mock<IDstController> dstController;

        #region 1

        //private DifferenceViewModel viewModel10;
        //private Mock<IHubController> hubController10;
        //private Mock<IDstController> dstController10;
        private Assembler assembler10;
        private readonly Uri uri10 = new Uri("http://test.com");
        private ElementDefinition elementDefinition10;

        private Parameter parameter101;
        private Parameter parameter102;
        
        private QuantityKind qqParamType10;
        private DomainOfExpertise activeDomain10;

        #endregion

        #region 2
        private Assembler assembler20;
        private readonly Uri uri20 = new Uri("http://test.com");
        private ElementDefinition elementDefinition20;

        private Parameter parameter201;
        private Parameter parameter202;
        
        private QuantityKind qqParamType20;
        private DomainOfExpertise activeDomain20;
        private ActualFiniteStateList actualStateList20;
        private ActualFiniteState actualState201;
        private ActualFiniteState actualState202;
        private PossibleFiniteState possibleState201;
        private PossibleFiniteState possibleState202;
        private PossibleFiniteStateList possibleStateList20;
        #endregion

        #region 3

        private Assembler assembler30;
        private readonly Uri uri30 = new Uri("http://test.com");
        private ElementDefinition elementDefinition30;

        private Parameter parameter301;
        private Parameter parameter302;

        private Option option301;
        private Option option302;
        private QuantityKind qqParamType30;
        private DomainOfExpertise activeDomain30;
        #endregion

        #region 4

        private Assembler assembler40;
        private readonly Uri uri40 = new Uri("http://test.com");
        private ElementDefinition elementDefinition40;

        private Parameter parameter401;
        private Parameter parameter402;

        private Option option401;
        private Option option402;
        private QuantityKind qqParamType40;
        private DomainOfExpertise activeDomain40;
        private ActualFiniteStateList actualStateList40;
        private ActualFiniteState actualState401;
        private ActualFiniteState actualState402;
        private PossibleFiniteState possibleState401;
        private PossibleFiniteState possibleState402;
        private PossibleFiniteStateList possibleStateList40;
        #endregion

        #region 5

        private Assembler assembler50;
        private readonly Uri uri50 = new Uri("http://test.com");
        private ElementDefinition elementDefinition50;

        private Parameter parameter501;
        private Parameter parameter502;
        private Parameter parameter503;
        private Parameter parameter504;
        
        private QuantityKind qqParamType50;
        private DomainOfExpertise activeDomain50;
        #endregion
       
        #endregion

        [SetUp]
        public void SetUp()
        {
            this.hubController = new Mock<IHubController>();
            this.dstController = new Mock<IDstController>();
            this.viewModel = new DifferenceViewModel(this.hubController.Object, this.dstController.Object);

        }

        [TearDown]
        public void TearDown()
        {
            this.hubController.Reset();
            this.dstController.Reset();
        }


        [Test]
        public void VerifyProperties()
        {
            Assert.IsEmpty(this.viewModel.Parameters);
        }

        [Test]
        public void VerifyDifferenceViewModelForNoOptionAndNoStates()
        {
            this.SetThingsForVerifyDifferenceViewModelForNoOptionAndNoStates();

            this.hubController.Setup(x => x.GetThingById(this.parameter102.Iid, It.IsAny<Iteration>(), out this.parameter101)).Returns(true);
            
            CDPMessageBus.Current.SendMessage(new DifferenceEvent<ParameterOrOverrideBase>(true, this.parameter102));

            this.hubController.Verify(x => x.GetThingById(this.parameter102.Iid, It.IsAny<Iteration>(), out this.parameter101), Times.Once);
            
            var listOfParameters = this.viewModel.Parameters;

            Assert.IsNotNull(listOfParameters);
            Assert.AreEqual(1, listOfParameters.Count);
            Assert.AreEqual("-8", listOfParameters.FirstOrDefault()?.Difference);
        }

        private void SetThingsForVerifyDifferenceViewModelForNoOptionAndNoStates()
        {
            #region Both

            //this.hubController10 = new Mock<IHubController>();
            //this.dstController10 = new Mock<IDstController>();
            //this.viewModel10 = new DifferenceViewModel(this.hubController10.Object, this.dstController10.Object);

            this.assembler10 = new Assembler(this.uri10);

            this.activeDomain10 = new DomainOfExpertise(Guid.NewGuid(), this.assembler10.Cache, this.uri10) { Name = "active", ShortName = "active" };
            this.elementDefinition10 = new ElementDefinition(Guid.NewGuid(), this.assembler10.Cache, this.uri10)
            {
                Owner = this.activeDomain10,
                ShortName = "Element10"
            };

            this.qqParamType10 = new SimpleQuantityKind(Guid.NewGuid(), this.assembler10.Cache, this.uri10)
            {
                Name = "PTName",
                ShortName = "PTShortName"
            };
            #endregion
            
            #region Parameter101

            var valueset101 = new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "20" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED
            };

            this.parameter101 = new Parameter(Guid.NewGuid(), this.assembler10.Cache, this.uri10)
            {
                ParameterType = this.qqParamType10,
                Owner = this.activeDomain10,
                ValueSet =
                {
                    valueset101
                }
            };
            this.elementDefinition10.Parameter.Add(this.parameter101);

            #endregion

            #region Parameter102

            var valueset102 = new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "12" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED
            };
            this.parameter102 = new Parameter(this.parameter101.Iid, this.assembler10.Cache, this.uri10)
            {
                ParameterType = this.qqParamType10,
                Owner = this.activeDomain10,
                ValueSet =
                {
                    valueset102
                }
            };

            this.elementDefinition10.Parameter.Add(this.parameter102);

            #endregion

        }
        
        [Test]
        public void VerifyDifferenceViewModelForNoOptionAndTwoState()
        {
            this.SetThingsForVerifyDifferenceViewModelForNoOptionAndTwoState();
            
            this.hubController.Setup(x => x.GetThingById(this.parameter202.Iid, It.IsAny<Iteration>(), out this.parameter201)).Returns(true);

            CDPMessageBus.Current.SendMessage(new DifferenceEvent<ParameterOrOverrideBase>(true, this.parameter202));

            var listOfParameters = this.viewModel.Parameters;

            Assert.IsNotNull(listOfParameters);
            Assert.AreEqual(2, listOfParameters.Count);
            Assert.AreEqual("-9", listOfParameters.FirstOrDefault()?.Difference);
            Assert.AreEqual("-9", listOfParameters.LastOrDefault()?.Difference);
        }

        private void SetThingsForVerifyDifferenceViewModelForNoOptionAndTwoState()
        {
            #region Both
            
            this.assembler20 = new Assembler(this.uri20);

            this.activeDomain20 = new DomainOfExpertise(Guid.NewGuid(), this.assembler20.Cache, this.uri20) { Name = "active", ShortName = "active" };
            this.elementDefinition20 = new ElementDefinition(Guid.NewGuid(), this.assembler20.Cache, this.uri20)
            {
                Owner = this.activeDomain20,
                ShortName = "Element"
            };

            this.possibleStateList20 = new PossibleFiniteStateList(Guid.NewGuid(), this.assembler20.Cache, this.uri20) {Name = "possibleStateList", ShortName = "possibleStateList" };

            this.possibleState201 = new PossibleFiniteState(Guid.NewGuid(), this.assembler20.Cache, this.uri20) { ShortName = "possibleState1", Name = "possibleState1" };
            this.possibleState202 = new PossibleFiniteState(Guid.NewGuid(), this.assembler20.Cache, this.uri20) { ShortName = "possibleState2", Name = "possibleState2" };

            this.possibleStateList20.PossibleState.Add(this.possibleState201);
            this.possibleStateList20.PossibleState.Add(this.possibleState202);

            this.actualStateList20 = new ActualFiniteStateList(Guid.NewGuid(), this.assembler20.Cache, this.uri20);

            this.actualStateList20.PossibleFiniteStateList.Add(this.possibleStateList20);

            this.actualState201 = new ActualFiniteState(Guid.NewGuid(), this.assembler20.Cache, this.uri20)
            {
                PossibleState = new List<PossibleFiniteState> { this.possibleState201 },
                Kind = ActualFiniteStateKind.MANDATORY
            };

            this.actualState202 = new ActualFiniteState(Guid.NewGuid(), this.assembler20.Cache, this.uri20)
            {
                PossibleState = new List<PossibleFiniteState> { this.possibleState202 },
                Kind = ActualFiniteStateKind.MANDATORY
            };

            this.actualStateList20.ActualState.Add(this.actualState201);

            this.actualStateList20.ActualState.Add(this.actualState202);

            this.qqParamType20 = new SimpleQuantityKind(Guid.NewGuid(), this.assembler20.Cache, this.uri20)
            {
                Name = "PTName",
                ShortName = "PTShortName"
            };

            #endregion

            #region Parameter201
            
            var valueset = new ParameterValueSet()
                {
                    Computed = new ValueArray<string>(new[] { "21" }),
                    ValueSwitch = ParameterSwitchKind.COMPUTED,
                    ActualState = this.actualState201
            };

            this.parameter201 = new Parameter(Guid.NewGuid(), this.assembler20.Cache, this.uri20)
            {
                ParameterType = this.qqParamType20,
                Owner = this.activeDomain20,
                StateDependence = this.actualStateList20,
                ValueSet =
                {
                    valueset
                }
            };
            this.parameter201.ValueSet.Add(new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "22" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED,
                ActualState = this.actualState202
            });
            
            this.elementDefinition20.Parameter.Add(this.parameter201);

            #endregion

            #region Parameter202

            var valueset2 = new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "12" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED,
                ActualState = this.actualState201
            };
            this.parameter202 = new Parameter(this.parameter201.Iid, this.assembler20.Cache, this.uri20)
            {
                ParameterType = this.qqParamType20,
                Owner = this.activeDomain20,
                StateDependence = this.actualStateList20,
                ValueSet =
                {
                    valueset2
                }
            };

            this.parameter202.ValueSet.Add(new ParameterValueSet()
                {
                    Computed = new ValueArray<string>(new[] { "13" }),
                    ValueSwitch = ParameterSwitchKind.COMPUTED,
                    ActualState = this.actualState202
                });


            this.elementDefinition20.Parameter.Add(this.parameter202);

            #endregion

        }
        
        [Test]
        public void VerifyDifferenceViewModelForTwoOptionAndNoState()
        {
            this.SetThingsForVerifyDifferenceViewModelForTwoOptionAndNoState();
            
            this.hubController.Setup(x => x.GetThingById(this.parameter302.Iid, It.IsAny<Iteration>(), out this.parameter301)).Returns(true);

            CDPMessageBus.Current.SendMessage(new DifferenceEvent<ParameterOrOverrideBase>(true, this.parameter302));

            var listOfParameters = this.viewModel.Parameters;

            Assert.IsNotNull(listOfParameters);
            Assert.AreEqual(2, listOfParameters.Count);
            Assert.AreEqual("-9", listOfParameters.FirstOrDefault()?.Difference);
            Assert.AreEqual("-9", listOfParameters.LastOrDefault()?.Difference);
        }

        private void SetThingsForVerifyDifferenceViewModelForTwoOptionAndNoState()
        {
            #region Both

            this.assembler30 = new Assembler(this.uri30);

            this.activeDomain30 = new DomainOfExpertise(Guid.NewGuid(), this.assembler30.Cache, this.uri30) { Name = "active", ShortName = "active" };
            this.elementDefinition30 = new ElementDefinition(Guid.NewGuid(), this.assembler30.Cache, this.uri30)
            {
                Owner = this.activeDomain30,
                ShortName = "Element"
            };


            this.option301 = new Option(Guid.NewGuid(), this.assembler30.Cache, this.uri30) { ShortName = "option1" };
            this.option302 = new Option(Guid.NewGuid(), this.assembler30.Cache, this.uri30) { ShortName = "option2" };

            this.qqParamType30 = new SimpleQuantityKind(Guid.NewGuid(), this.assembler30.Cache, this.uri30)
            {
                Name = "PTName",
                ShortName = "PTShortName"
            };

            #endregion

            #region Parameter301

            var valueset = new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "21" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED,
                ActualOption = this.option301
            };

            this.parameter301 = new Parameter(Guid.NewGuid(), this.assembler30.Cache, this.uri30)
            {
                ParameterType = this.qqParamType30,
                IsOptionDependent = true,
                Owner = this.activeDomain30,
                ValueSet =
                {
                    valueset
                }
            };
            this.parameter301.ValueSet.Add(new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "22" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED,
                ActualOption = this.option302
            });

            this.elementDefinition30.Parameter.Add(this.parameter301);

            #endregion

            #region Parameter302

            var valueset2 = new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "12" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED,
                ActualOption = this.option301
            };
            this.parameter302 = new Parameter(this.parameter301.Iid, this.assembler30.Cache, this.uri30)
            {
                ParameterType = this.qqParamType30,
                Owner = this.activeDomain30,
                IsOptionDependent = true,
                ValueSet =
                {
                    valueset2
                }
            };

            this.parameter302.ValueSet.Add(new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "13" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED,
                ActualOption = this.option302
            });


            this.elementDefinition30.Parameter.Add(this.parameter302);

            #endregion

        }
        
        [Test]
        public void VerifyDifferenceViewModelForTwoOptionAndTwoState()
        {
            this.SetThingsForVerifyDifferenceViewModelForTwoOptionAndTwoState();
            
            this.hubController.Setup(x => x.GetThingById(this.parameter402.Iid, It.IsAny<Iteration>(), out this.parameter401)).Returns(true);

            CDPMessageBus.Current.SendMessage(new DifferenceEvent<ParameterOrOverrideBase>(true, this.parameter402));

            var listOfParameters = this.viewModel.Parameters;

            Assert.IsNotNull(listOfParameters);
            Assert.AreEqual(4, listOfParameters.Count);
            Assert.AreEqual("-9", listOfParameters[0]?.Difference);
            Assert.AreEqual("-9", listOfParameters[1]?.Difference);
            Assert.AreEqual("-9", listOfParameters[2]?.Difference);
            Assert.AreEqual("-9", listOfParameters[3]?.Difference);
        }

        private void SetThingsForVerifyDifferenceViewModelForTwoOptionAndTwoState()
        {
            #region Both

            this.assembler40 = new Assembler(this.uri40);

            this.activeDomain40 = new DomainOfExpertise(Guid.NewGuid(), this.assembler40.Cache, this.uri40) { Name = "active", ShortName = "active" };
            this.elementDefinition40 = new ElementDefinition(Guid.NewGuid(), this.assembler40.Cache, this.uri40)
            {
                Owner = this.activeDomain40,
                ShortName = "Element"
            };

            this.possibleStateList40 = new PossibleFiniteStateList(Guid.NewGuid(), this.assembler40.Cache, this.uri40) { Name = "possibleStateList", ShortName = "possibleStateList" };

            this.possibleState401 = new PossibleFiniteState(Guid.NewGuid(), this.assembler40.Cache, this.uri40) { ShortName = "possibleState1", Name = "possibleState1" };
            this.possibleState402 = new PossibleFiniteState(Guid.NewGuid(), this.assembler40.Cache, this.uri40) { ShortName = "possibleState2", Name = "possibleState2" };

            this.possibleStateList40.PossibleState.Add(this.possibleState401);
            this.possibleStateList40.PossibleState.Add(this.possibleState402);

            this.actualStateList40 = new ActualFiniteStateList(Guid.NewGuid(), this.assembler40.Cache, this.uri40);

            this.actualStateList40.PossibleFiniteStateList.Add(this.possibleStateList40);

            this.actualState401 = new ActualFiniteState(Guid.NewGuid(), this.assembler40.Cache, this.uri40)
            {
                PossibleState = new List<PossibleFiniteState> { this.possibleState401 },
                Kind = ActualFiniteStateKind.MANDATORY
            };

            this.actualState402 = new ActualFiniteState(Guid.NewGuid(), this.assembler40.Cache, this.uri40)
            {
                PossibleState = new List<PossibleFiniteState> { this.possibleState402 },
                Kind = ActualFiniteStateKind.MANDATORY
            };

            this.actualStateList40.ActualState.Add(this.actualState401);

            this.actualStateList40.ActualState.Add(this.actualState402);

            this.qqParamType40 = new SimpleQuantityKind(Guid.NewGuid(), this.assembler40.Cache, this.uri40)
            {
                Name = "PTName",
                ShortName = "PTShortName"
            };

            this.option401 = new Option(Guid.NewGuid(), this.assembler40.Cache, this.uri40) { ShortName = "option1" };
            this.option402 = new Option(Guid.NewGuid(), this.assembler40.Cache, this.uri40) { ShortName = "option2" };

            #endregion

            #region Parameter401

            var valueset = new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "21" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED,
                ActualState = this.actualState401,
                ActualOption = this.option401
            };

            this.parameter401 = new Parameter(Guid.NewGuid(), this.assembler40.Cache, this.uri40)
            {
                ParameterType = this.qqParamType40,
                Owner = this.activeDomain40,
                StateDependence = this.actualStateList40,
                IsOptionDependent = true,
                ValueSet =
                {
                    valueset
                }
            };
            this.parameter401.ValueSet.Add(new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "22" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED,
                ActualState = this.actualState402,
                ActualOption = this.option401
            });
            this.parameter401.ValueSet.Add(new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "23" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED,
                ActualState = this.actualState401,
                ActualOption = this.option402
            });
            this.parameter401.ValueSet.Add(new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "24" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED,
                ActualState = this.actualState402,
                ActualOption = this.option402
            });

            this.elementDefinition40.Parameter.Add(this.parameter401);

            #endregion

            #region Parameter402

            var valueset2 = new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "12" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED,
                ActualState = this.actualState401,
                ActualOption = this.option401
            };
            this.parameter402 = new Parameter(this.parameter401.Iid, this.assembler40.Cache, this.uri40)
            {
                ParameterType = this.qqParamType40,
                Owner = this.activeDomain40,
                StateDependence = this.actualStateList40,
                IsOptionDependent = true,
                ValueSet =
                {
                    valueset2
                }
            };

            this.parameter402.ValueSet.Add(new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "13" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED,
                ActualState = this.actualState402,
                ActualOption = this.option401
            });
            this.parameter402.ValueSet.Add(new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "14" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED,
                ActualState = this.actualState401,
                ActualOption = this.option402
            });
            this.parameter402.ValueSet.Add(new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "15" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED,
                ActualState = this.actualState402,
                ActualOption = this.option402
            });


            this.elementDefinition40.Parameter.Add(this.parameter402);

            #endregion

        }

        [Test]
        public void VerifyAListOfDifferenceViewModelForTwoOptionAndTwoState()
        {
            this.SetAListOfThingsForVerifyDifferenceViewModelForNoOptionAndNoState();
            
            this.hubController.Setup(x => x.GetThingById(this.parameter501.Iid, It.IsAny<Iteration>(), out this.parameter502)).Returns(true);
            this.hubController.Setup(x => x.GetThingById(this.parameter503.Iid, It.IsAny<Iteration>(), out this.parameter504)).Returns(true);
            
            var elementDefinition = new ElementDefinition(Guid.NewGuid(), this.assembler50.Cache, this.uri50)
            {
                Owner = this.activeDomain50,
                ShortName = "ElementMultiple"
            }; 
            elementDefinition.Parameter.Add(this.parameter501);
            elementDefinition.Parameter.Add(this.parameter503);
            CDPMessageBus.Current.SendMessage(new DifferenceEvent<ElementDefinition>(true, elementDefinition));
            
            var listOfParameters = this.viewModel.Parameters;

            Assert.IsNotNull(listOfParameters);
            Assert.AreEqual(2, listOfParameters.Count);
            Assert.AreEqual("+8", listOfParameters.FirstOrDefault()?.Difference);
            Assert.AreEqual("+12", listOfParameters.LastOrDefault()?.Difference);
        }

        private void SetAListOfThingsForVerifyDifferenceViewModelForNoOptionAndNoState()
        {
            #region Both

            this.assembler50 = new Assembler(this.uri50);

            this.activeDomain50 = new DomainOfExpertise(Guid.NewGuid(), this.assembler50.Cache, this.uri50) { Name = "active", ShortName = "active" };
            this.elementDefinition50 = new ElementDefinition(Guid.NewGuid(), this.assembler50.Cache, this.uri50)
            {
                Owner = this.activeDomain50,
                ShortName = "Element"
            };

            this.qqParamType50 = new SimpleQuantityKind(Guid.NewGuid(), this.assembler50.Cache, this.uri50)
            {
                Name = "PTName",
                ShortName = "PTShortName"
            };

            #endregion
            
            #region Parameter501

            var valueset50 = new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "20" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED
            };

            this.parameter501 = new Parameter(Guid.NewGuid(), this.assembler50.Cache, this.uri50)
            {
                ParameterType = this.qqParamType50,
                Owner = this.activeDomain50,
                ValueSet =
                {
                    valueset50
                }
            };
            this.elementDefinition50.Parameter.Add(this.parameter501);

            #endregion

            #region Parameter502

            var valueset502 = new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "12" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED
            };
            this.parameter502 = new Parameter(this.parameter501.Iid, this.assembler50.Cache, this.uri50)
            {
                ParameterType = this.qqParamType50,
                Owner = this.activeDomain50,
                ValueSet =
                {
                    valueset502
                }
            };

            this.elementDefinition50.Parameter.Add(this.parameter502);

            #endregion

            #region Parameter503

            var valueset503 = new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "25" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED
            };

            this.parameter503 = new Parameter(Guid.NewGuid(), this.assembler50.Cache, this.uri50)
            {
                ParameterType = this.qqParamType50,
                Owner = this.activeDomain50,
                ValueSet =
                {
                    valueset503
                }
            };
            this.elementDefinition50.Parameter.Add(this.parameter503);

            #endregion

            #region Parameter504

            var valueset504 = new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "13" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED
            };
            this.parameter504 = new Parameter(this.parameter503.Iid, this.assembler50.Cache, this.uri50)
            {
                ParameterType = this.qqParamType50,
                Owner = this.activeDomain50,
                ValueSet =
                {
                    valueset504
                }
            };

            this.elementDefinition50.Parameter.Add(this.parameter504);

            #endregion

        }
    }
}
