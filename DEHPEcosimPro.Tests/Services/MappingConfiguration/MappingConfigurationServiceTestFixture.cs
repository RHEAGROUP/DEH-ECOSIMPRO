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

    using CDP4Common.EngineeringModelData;
    using CDP4Common.Types;

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
        private Mock<IHubController> hubservice;
        private List<ExternalIdentifier> externalIdentifiers;
        private ExternalIdentifierMap externalIdentifierMap;

        [SetUp]
        public void Setup()
        {
            this.statusBar = new Mock<IStatusBarControlViewModel>();
            this.hubservice = new Mock<IHubController>();
            this.service = new MappingConfigurationService(this.statusBar.Object, this.hubservice.Object);

            this.externalIdentifiers = new List<ExternalIdentifier>()
            {
                new ExternalIdentifier()
                {
                    MappingDirection = MappingDirection.FromDstToHub,
                    Identifier = "var0",
                    ValueIndex = 2
                },
                new ExternalIdentifier()
                {
                    MappingDirection = MappingDirection.FromHubToDst,
                    Identifier = "var1",
                    ValueIndex = 0,
                    ParameterSwitchKind = ParameterSwitchKind.COMPUTED
                }
            };

            this.externalIdentifierMap = new ExternalIdentifierMap(Guid.NewGuid(), null, null)
            {
                Correspondence =
                {
                    new IdCorrespondence()
                    {
                        InternalThing = Guid.NewGuid(), ExternalId = JsonConvert.SerializeObject(this.externalIdentifiers[0])
                    },
                    new IdCorrespondence()
                    {
                        InternalThing = Guid.NewGuid(), ExternalId = JsonConvert.SerializeObject(this.externalIdentifiers[1])
                    },
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
                    Manual = new ValueArray<string>(new List<string>(){"1","42","3"}),
                    Computed = new ValueArray<string>(new List<string>(){"1","42","3"}),
                    Reference = new ValueArray<string>(new List<string>(){"1","42","3"})
                },"41", null)
            });

            Assert.AreEqual(2, this.service.ExternalIdentifierMap.Correspondence.Count);

            this.service.AddToExternalIdentifierMap(internalId, "node23", MappingDirection.FromDstToHub); 
            Assert.AreEqual(3, this.service.ExternalIdentifierMap.Correspondence.Count);

            this.service.AddToExternalIdentifierMap(internalId, 2d, "node56", MappingDirection.FromDstToHub);
            Assert.AreEqual(4, this.service.ExternalIdentifierMap.Correspondence.Count);
            
            this.service.AddToExternalIdentifierMap(new Dictionary<ParameterOrOverrideBase, VariableRowViewModel>()
            {
                {new Parameter(), new VariableRowViewModel((new ReferenceDescription()
                {
                    NodeId = new ExpandedNodeId("noe85"), DisplayName = "node85"
                }, new DataValue("53")))},
                {new Parameter(), new VariableRowViewModel((new ReferenceDescription()
                {
                    NodeId = new ExpandedNodeId("node66"), DisplayName = "node66"
                }, new DataValue("86")))}
            });
        }
    }
}
