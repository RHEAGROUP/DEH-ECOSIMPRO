// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappingConfigurationService.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Tests.Services.MappingConfiguration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.Types;

    using CDP4Dal.Operations;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.Services.MappingConfiguration;
    using DEHPEcosimPro.ViewModel.Rows;

    using Moq;

    using Newtonsoft.Json;

    using NUnit.Framework;

    using Opc.Ua;

    [TestFixture]
    public class MappingConfigurationServiceTestFixture
    {
        private MappingConfigurationService service;
        private Mock<IStatusBarControlViewModel> statusBar;
        private Mock<IHubController> hubController;
        private List<ExternalIdentifier> externalIdentifiers;
        private ExternalIdentifierMap externalIdentifierMap;
        private List<VariableRowViewModel> variables;
        private ElementDefinition element;
        private Parameter parameter;

        [SetUp]
        public void Setup()
        {
            this.statusBar = new Mock<IStatusBarControlViewModel>();
            this.hubController = new Mock<IHubController>();
            this.service = new MappingConfigurationService(this.statusBar.Object, this.hubController.Object);

            this.variables = new List<VariableRowViewModel>()
            {
                new VariableRowViewModel((
                    new ReferenceDescription()
                    {
                        DisplayName = new LocalizedText(string.Empty, "Mos.a"),
                        NodeId = new NodeId("Mos.a")
                    },
                    new DataValue() { Value = 5, ServerTimestamp = DateTime.MinValue })),
                new VariableRowViewModel((
                    new ReferenceDescription()
                    {
                        DisplayName = new LocalizedText(string.Empty, "res0"),
                        NodeId = new NodeId("res0")
                    },
                    new DataValue() { Value = 5, ServerTimestamp = DateTime.MinValue }))
                {
                    Values =
                    {
                        new TimeTaggedValueRowViewModel(8, 0),
                        new TimeTaggedValueRowViewModel(16, 1),
                        new TimeTaggedValueRowViewModel(32, 2)
                    }
                }
            };

            this.externalIdentifiers = new List<ExternalIdentifier>()
            {
                new ExternalIdentifier()
                {
                    MappingDirection = MappingDirection.FromDstToHub,
                    Identifier = "res0",
                    ValueIndex = 2
                },
                new ExternalIdentifier()
                {
                    MappingDirection = MappingDirection.FromDstToHub,
                    Identifier = "res0",
                    ValueIndex = 0
                },
                new ExternalIdentifier()
                {
                    MappingDirection = MappingDirection.FromHubToDst,
                    Identifier = "Mos.a",
                    ValueIndex = 0,
                    ParameterSwitchKind = ParameterSwitchKind.COMPUTED
                }
            };

            this.parameter = new Parameter(Guid.NewGuid(), null, null);

            this.element = new ElementDefinition(Guid.NewGuid(), null, null)
            {
                Parameter = {this.parameter}
            };
            
            this.externalIdentifierMap = new ExternalIdentifierMap(Guid.NewGuid(), null, null)
            {
                Correspondence = 
                {
                    new IdCorrespondence() { InternalThing = this.element.Iid, ExternalId = JsonConvert.SerializeObject(this.externalIdentifiers[0]) },
                    new IdCorrespondence() { InternalThing = this.parameter.Iid, ExternalId = JsonConvert.SerializeObject(this.externalIdentifiers[1]) },
                    new IdCorrespondence() { InternalThing = Guid.NewGuid(), ExternalId = JsonConvert.SerializeObject(this.externalIdentifiers[2]) },
                }
            };
        }

        [Test]
        public void VerifyProperies()
        {
            Assert.IsNull(this.service.ExternalIdentifierMap);
        }

        [Test]
        public void VerifyLoadValues()
        {
            Assert.True(true);
        }
        
        [Test]
        public void VerifyCreateExternalIdentifierMap()
        {
            var newExternalIdentifierMap = this.service.CreateExternalIdentifierMap("Name");
            this.service.ExternalIdentifierMap = newExternalIdentifierMap;
            Assert.AreEqual("Name", this.service.ExternalIdentifierMap.Name);
            Assert.AreEqual("Name", this.service.ExternalIdentifierMap.ExternalModelName);
        }

        [Test]
        public void VerifyAddToExternalIdentifierMap()
        {
            this.service.ExternalIdentifierMap = this.service.CreateExternalIdentifierMap("test");

            var internalId = Guid.NewGuid();
            this.service.AddToExternalIdentifierMap(internalId, this.externalIdentifiers[0]);
            Assert.IsNotEmpty(this.service.ExternalIdentifierMap.Correspondence);
            Assert.AreEqual(1, this.service.ExternalIdentifierMap.Correspondence.Count);

            this.service.AddToExternalIdentifierMap(new MappedElementDefinitionRowViewModel() 
            {
                SelectedVariable = new VariableRowViewModel((new ReferenceDescription()
                {
                    NodeId = new ExpandedNodeId("4"), DisplayName = "cata"
                }, new DataValue("1"))),
                SelectedValue = new ValueSetValueRowViewModel(
                new ParameterValueSet()
                {
                    Manual = new ValueArray<string>(new List<string>(){"16","43","33"}),
                    Computed = new ValueArray<string>(new List<string>(){"81","48","32"}),
                    Reference = new ValueArray<string>(new List<string>(){"19","42","31"})
                },"42", null)
            });

            this.service.AddToExternalIdentifierMap(new MappedElementDefinitionRowViewModel()
            {
                SelectedVariable = new VariableRowViewModel((new ReferenceDescription()
                {
                    NodeId = new ExpandedNodeId("4"),
                    DisplayName = "cata"
                }, new DataValue("1"))),
                SelectedValue = new ValueSetValueRowViewModel(
                    new ParameterValueSet()
                    {
                        Manual = new ValueArray<string>(new List<string>() { "871", "428", "37" }),
                        Computed = new ValueArray<string>(new List<string>() { "91", "642", "893" }),
                        Reference = new ValueArray<string>(new List<string>() { "551", "442", "38" })
                    }, "428", null)
            });

            this.service.AddToExternalIdentifierMap(new MappedElementDefinitionRowViewModel()
            {
                SelectedVariable = new VariableRowViewModel((new ReferenceDescription()
                {
                    NodeId = new ExpandedNodeId("4"),
                    DisplayName = "cata"
                }, new DataValue("1"))),
                SelectedValue = new ValueSetValueRowViewModel(
                    new ParameterValueSet()
                    {
                        Manual = new ValueArray<string>(new List<string>() { "871", "428", "37" }),
                        Computed = new ValueArray<string>(new List<string>() { "91", "642", "893" }),
                        Reference = new ValueArray<string>(new List<string>() { "551", "442", "38" })
                    }, "37", null)
            });

            Assert.AreEqual(4, this.service.ExternalIdentifierMap.Correspondence.Count);

            this.service.AddToExternalIdentifierMap(internalId, "node23", MappingDirection.FromDstToHub); 
            Assert.AreEqual(5, this.service.ExternalIdentifierMap.Correspondence.Count);

            this.service.AddToExternalIdentifierMap(internalId, 2d, "node56", MappingDirection.FromDstToHub);
            Assert.AreEqual(6, this.service.ExternalIdentifierMap.Correspondence.Count);
            
            this.service.AddToExternalIdentifierMap(new Dictionary<ParameterOrOverrideBase, VariableRowViewModel>()
            {
                {
                    new Parameter(), new VariableRowViewModel((new ReferenceDescription()
                    {
                        NodeId = new ExpandedNodeId("noe85"), DisplayName = "node85"
                    }, new DataValue("53")))},
                {
                    new Parameter(), new VariableRowViewModel((new ReferenceDescription()
                    {
                        NodeId = new ExpandedNodeId("node66"), DisplayName = "node66"
                    }, new DataValue("86")))
                }
            });
        }

        [Test]
        public void VerifyRefresh()
        {
            this.service.ExternalIdentifierMap = this.externalIdentifierMap;
            Assert.AreSame(this.externalIdentifierMap, this.service.ExternalIdentifierMap);
            var map = new ExternalIdentifierMap();
            this.hubController.Setup(x => x.GetThingById(It.IsAny<Guid>(), It.IsAny<Iteration>(), out map));
            Assert.DoesNotThrow(() => this.service.RefreshExternalIdentifierMap());
            Assert.IsNotNull(this.service.ExternalIdentifierMap);
            Assert.AreSame(map, this.service.ExternalIdentifierMap.Original);
            Assert.AreNotSame(this.externalIdentifierMap, this.service.ExternalIdentifierMap);
        }

        [Test]
        public void VerifyLoadMappingFromHubToDst()
        {
            Assert.IsNull(this.service.LoadMappingFromHubToDst(this.variables));
            this.service.ExternalIdentifierMap = this.externalIdentifierMap;
            this.externalIdentifierMap.Iid = Guid.Empty;
            Assert.IsNull(this.service.LoadMappingFromHubToDst(this.variables));
            this.externalIdentifierMap.Iid = Guid.NewGuid();
            var correspondences = this.externalIdentifierMap.Correspondence.ToArray();

            this.externalIdentifierMap.Correspondence.Clear();
            Assert.IsNull(this.service.LoadMappingFromHubToDst(this.variables));
            this.externalIdentifierMap.Correspondence.AddRange(correspondences);

            var mappedRows = new List<MappedElementDefinitionRowViewModel>();
            Assert.DoesNotThrow(() => mappedRows = this.service.LoadMappingFromHubToDst(this.variables));
            ParameterValueSetBase valueSet = null;
            this.hubController.Setup(x => x.GetThingById(It.IsAny<Guid>(), It.IsAny<Iteration>(), out valueSet));
            Assert.DoesNotThrow(() => mappedRows = this.service.LoadMappingFromHubToDst(this.variables));

           valueSet = (ParameterValueSetBase)new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "42" })
            };

           this.hubController.Setup(x => x.GetThingById(It.IsAny<Guid>(), It.IsAny<Iteration>(), out valueSet)).Returns(true);
           Assert.DoesNotThrow(() => mappedRows = this.service.LoadMappingFromHubToDst(this.variables));

           this.hubController.Verify(x => x.GetThingById(It.IsAny<Guid>(), It.IsAny<Iteration>(), out valueSet), Times.Exactly(3)); 
           Assert.AreEqual(1, mappedRows.Count);
           Assert.AreEqual("42", mappedRows.First().SelectedValue.Value);
        }
        
        [Test]
        public void VerifyLoadMappingFromDstToHub()
        {
            this.service.ExternalIdentifierMap = this.externalIdentifierMap;
            Assert.DoesNotThrow(() => this.service.LoadMappingFromDstToHub(this.variables));
            
            var thing = default(Thing);
            Assert.DoesNotThrow(() => this.service.LoadMappingFromDstToHub(this.variables));

            var parameterAsThing = (Thing) this.parameter;
            var elementAsThing = (Thing)this.element;
            this.hubController.Setup(x => x.GetThingById(this.parameter.Iid, It.IsAny<Iteration>(), out parameterAsThing)).Returns(true);
            this.hubController.Setup(x => x.GetThingById(this.element.Iid, It.IsAny<Iteration>(), out elementAsThing)).Returns(true);

            var mappedVariables = new List<VariableRowViewModel>();
            Assert.DoesNotThrow(() => mappedVariables = this.service.LoadMappingFromDstToHub(this.variables));

            Assert.IsNotNull(mappedVariables);
            Assert.AreEqual(1, mappedVariables.Count);

            this.hubController.Verify(x => x.GetThingById(It.IsAny<Guid>(), It.IsAny<Iteration>(), out thing), Times.Exactly(6));
        }

        [Test]
        public void VerifyPersist()
        {
            this.service.ExternalIdentifierMap = this.externalIdentifierMap;
            var transactionMock = new Mock<IThingTransaction>();
            var iteration = new Iteration();
            Assert.DoesNotThrow(() => this.service.PersistExternalIdentifierMap(transactionMock.Object, iteration));

            this.service.ExternalIdentifierMap = new ExternalIdentifierMap()
            {
                Correspondence = { new IdCorrespondence(Guid.NewGuid(), null, null) }
            };

            Assert.DoesNotThrow(() => this.service.PersistExternalIdentifierMap(transactionMock.Object, iteration));

            Assert.AreEqual(1, iteration.ExternalIdentifierMap.Count);
            transactionMock.Verify(x => x.CreateOrUpdate(It.IsAny<Thing>()), Times.Exactly(3));
            transactionMock.Verify(x => x.Create(It.IsAny<Thing>(), null), Times.Exactly(3));
        }

        [Test]
        public void VerifySelectValues()
        {
            this.service.ExternalIdentifierMap = this.externalIdentifierMap;
            Assert.DoesNotThrow(() => this.service.LoadMappingFromDstToHub(this.variables));
            Assert.DoesNotThrow(() => this.service.SelectValues(this.variables));
            var mappedRow = this.variables.FirstOrDefault(x => x.MappingConfigurations.Any());

            Assert.AreEqual(2,mappedRow?.SelectedValues.Count);
            Assert.AreEqual(this.externalIdentifiers.First().ValueIndex,mappedRow?.SelectedValues.First().TimeStep);
            Assert.AreEqual(this.externalIdentifiers.Last().ValueIndex,mappedRow?.SelectedValues.Last().TimeStep);
        }
    }
}
