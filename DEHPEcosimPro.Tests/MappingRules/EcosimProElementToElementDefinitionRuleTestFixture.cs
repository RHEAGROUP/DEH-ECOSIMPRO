// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EcosimProElementToElementDefinitionRuleTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Tests.MappingRules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Autofac;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using CDP4Dal;

    using DEHPCommon;
    using DEHPCommon.HubController.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.MappingRules;
    using DEHPEcosimPro.ViewModel.Rows;

    using Moq;

    using NUnit.Framework;

    using Opc.Ua;

    [TestFixture]
    public class EcosimProElementToElementDefinitionRuleTestFixture
    {
        private EcosimProElementToElementDefinitionRule rule;

        private List<VariableRowViewModel> variables;
        private Mock<IHubController> hubController;
        private Uri uri;
        private Assembler assembler;
        private DomainOfExpertise domain;
        private Mock<ISession> session;
        private Iteration iteration;
        private Mock<IDstController> dstController;
        private SampledFunctionParameterType scalarParameterType;
        private SampledFunctionParameterType dateTimeParameterType;

        [SetUp]
        public void Setup()
        {
            this.uri = new Uri("https://test.test");
            this.assembler = new Assembler(this.uri);
            this.domain = new DomainOfExpertise(Guid.NewGuid(), this.assembler.Cache, this.uri);
            this.session = new Mock<ISession>();
            this.session.Setup(x => x.Assembler).Returns(this.assembler);
            this.session.Setup(x => x.DataSourceUri).Returns(this.uri.AbsoluteUri);

            this.iteration =
                new Iteration(Guid.NewGuid(), this.assembler.Cache, this.uri)
                {
                    Container = new EngineeringModel(Guid.NewGuid(), this.assembler.Cache, this.uri)
                    {
                        EngineeringModelSetup = new EngineeringModelSetup(Guid.NewGuid(), this.assembler.Cache, this.uri)
                        {
                            RequiredRdl = { new ModelReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, this.uri) },
                            Container = new SiteReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, this.uri)
                            {
                                Container = new SiteDirectory(Guid.NewGuid(), this.assembler.Cache, this.uri)
                            }
                        }
                    }
                };

            this.hubController = new Mock<IHubController>();
            this.hubController.Setup(x => x.CurrentDomainOfExpertise).Returns(this.domain);
            this.hubController.Setup(x => x.Session).Returns(this.session.Object);
            this.hubController.Setup(x => x.OpenIteration).Returns(this.iteration);
            this.hubController.Setup(x => x.GetSiteDirectory()).Returns(new SiteDirectory());

            this.dstController = new Mock<IDstController>();
            this.dstController.Setup(x => x.IdCorrespondences).Returns(new List<IdCorrespondence>());

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(this.hubController.Object).As<IHubController>();
            containerBuilder.RegisterInstance(this.dstController.Object).As<IDstController>();
            AppContainer.Container = containerBuilder.Build();

            this.rule = new EcosimProElementToElementDefinitionRule();
            
            this.SetParameterTypes();

            this.variables = new List<VariableRowViewModel>()
            {
                new VariableRowViewModel((
                    new ReferenceDescription() {DisplayName = new LocalizedText(string.Empty, "Mos.a")},
                    new DataValue() {Value = 5, ServerTimestamp = DateTime.MinValue}))
                {
                    SelectedParameterType = this.scalarParameterType
                }
            };
        }

        [Test]
        public void VerifyMapToNewElementDefinition()
        {
            var timeTaggedValueRowViewModel = new TimeTaggedValueRowViewModel(.2, DateTime.MinValue);

            this.variables.Add(new VariableRowViewModel((
                new ReferenceDescription() { DisplayName = new LocalizedText(string.Empty, "Cap.a") },
                new DataValue() { Value = 5, ServerTimestamp = DateTime.MinValue }))
            {
                Values = { timeTaggedValueRowViewModel },
                SelectedValues = { timeTaggedValueRowViewModel },
                SelectedParameterType = this.scalarParameterType
            });

            this.variables.Add(new VariableRowViewModel((
                new ReferenceDescription() { DisplayName = new LocalizedText(string.Empty, "Cap.b") },
                new DataValue() { Value = 5, ServerTimestamp = DateTime.MinValue }))
            {
                Values = { timeTaggedValueRowViewModel },
                SelectedValues = { timeTaggedValueRowViewModel },
                SelectedParameterType = this.dateTimeParameterType
            });

            this.variables.FirstOrDefault()?.SelectedValues.Add(new TimeTaggedValueRowViewModel(42, DateTime.Now, DateTime.Now));

            var elements = this.rule.Transform(this.variables).ToList();
            Assert.AreEqual(3, elements.Count);
            var parameter = elements.Last().Parameter.First();
            Assert.AreEqual("TextXQuantity", parameter.ParameterType.Name);
            var parameterValueSet = parameter.ValueSet.Last();
            Assert.AreEqual("0", parameterValueSet.Computed[0]);
            Assert.AreEqual("0.2", parameterValueSet.Computed[1]);
        }
        
        [Test]
        public void VerifyMapToElementUsageParameter()
        {
            var timeTaggedValueRowViewModel = new TimeTaggedValueRowViewModel(.2, DateTime.MinValue);
            
            var parameter = new Parameter()
            {
                ParameterType = new SampledFunctionParameterType(Guid.NewGuid(), this.assembler.Cache, this.uri)
                {
                    Name = "a",
                    IndependentParameterType =
                    {
                        new IndependentParameterTypeAssignment(Guid.NewGuid(), this.assembler.Cache, this.uri)
                        {
                            ParameterType = new DateTimeParameterType(Guid.NewGuid(), this.assembler.Cache,  this.uri)
                            {
                                Name = "Timestamp"
                            }
                        }
                    },
                    DependentParameterType =
                    {
                        new DependentParameterTypeAssignment(Guid.NewGuid(), this.assembler.Cache,  this.uri)
                        {
                            ParameterType = new SimpleQuantityKind(Guid.NewGuid(), this.assembler.Cache,  this.uri)
                            {
                                Name = "Value"
                            }
                        }
                    }
                }
            };

            var elementDefinition = new ElementDefinition()
            {
                Parameter = { parameter }, Name = "nonameElement"
            };

            var elementUsage = new ElementUsage()
            {
                Name = "a",
                ParameterOverride =
                {
                    new ParameterOverride()
                    {
                        Parameter = parameter, 
                        ValueSet =
                        {
                            new ParameterOverrideValueSet(Guid.NewGuid(), this.assembler.Cache, this.uri)
                            {
                                Computed = new ValueArray<string>(new List<string>(){ "-","-"}),
                                ValueSwitch = ParameterSwitchKind.COMPUTED
                            }
                        }
                    }
                },
                ElementDefinition = elementDefinition
            };

            elementDefinition.ContainedElement.Add(elementUsage);

            this.variables.Clear();
            
            this.variables.Add(new VariableRowViewModel((
                new ReferenceDescription() { DisplayName = new LocalizedText(string.Empty, "Cap.a") },
                new DataValue() { Value = 5, ServerTimestamp = DateTime.MinValue }))
            {
                Values = { timeTaggedValueRowViewModel },
                SelectedValues = { timeTaggedValueRowViewModel },
                SelectedElementDefinition = elementDefinition,
                SelectedElementUsages = { elementUsage },
                SelectedParameterType = parameter.ParameterType
            });

            var elements = this.rule.Transform(this.variables);
            var definition = elements.Last();
            var first = definition.ContainedElement.First();
            var parameterOverride = first.ParameterOverride.Last();
            Assert.AreEqual(1, first.ParameterOverride.Count);
            var set = parameterOverride.ValueSet.First();
            Assert.AreEqual($"-", set.Computed.First());
        }

        private void SetParameterTypes()
        {
            this.scalarParameterType = new SampledFunctionParameterType(Guid.NewGuid(), null, null)
            {
                Name = "TextXQuantity",
                IndependentParameterType =
                {
                    new IndependentParameterTypeAssignment(Guid.NewGuid(),null,null)
                    {
                        ParameterType = new TextParameterType(Guid.NewGuid(),null,null)
                        {
                            Name = "IndependentText"
                        }
                    }
                },

                DependentParameterType =
                {
                    new DependentParameterTypeAssignment(Guid.NewGuid(),null,null)
                    {
                        ParameterType = new SimpleQuantityKind(Guid.NewGuid(),null,null)
                        {
                            Name = "DependentQuantityKing"
                        }
                    }
                }
            };

            this.dateTimeParameterType = new SampledFunctionParameterType(Guid.NewGuid(), null, null)
            {
                Name = "DateTimeXText",
                IndependentParameterType =
                {
                    new IndependentParameterTypeAssignment(Guid.NewGuid(),null,null)
                    {
                        ParameterType = new DateTimeParameterType(Guid.NewGuid(),null,null)
                        {
                            Name = "IndependentDateTime"
                        }
                    }
                },

                DependentParameterType =
                {
                    new DependentParameterTypeAssignment(Guid.NewGuid(),null,null)
                    {
                        ParameterType = new TextParameterType(Guid.NewGuid(),null,null)
                        {
                            Name = "DependentText"
                        }
                    }
                }
            };
        }
    }
}
