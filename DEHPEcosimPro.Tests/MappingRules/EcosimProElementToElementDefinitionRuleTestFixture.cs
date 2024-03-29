﻿// --------------------------------------------------------------------------------------------------------------------
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
    using DEHPEcosimPro.Services.MappingConfiguration;
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
        private ActualFiniteStateList actualFiniteStates;
        private RatioScale scale;
        private SimpleQuantityKind quantityKindParameterType;
        private Mock<IMappingConfigurationService> mappingConfigurationService;

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
            this.mappingConfigurationService = new Mock<IMappingConfigurationService>();

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(this.hubController.Object).As<IHubController>();
            containerBuilder.RegisterInstance(this.dstController.Object).As<IDstController>();
            containerBuilder.RegisterInstance(this.mappingConfigurationService.Object).As<IMappingConfigurationService>();
            AppContainer.Container = containerBuilder.Build();

            this.actualFiniteStates = new ActualFiniteStateList()
            {
                ActualState =
                {
                    new ActualFiniteState(),
                    new ActualFiniteState()
                }
            };

            this.rule = new EcosimProElementToElementDefinitionRule();
            
            this.SetParameterTypes();

            this.variables = new List<VariableRowViewModel>()
            {
                new VariableRowViewModel((
                    new ReferenceDescription() 
                    { 
                        NodeId = new ExpandedNodeId(Guid.NewGuid()), 
                        DisplayName = new LocalizedText(string.Empty, "Mos.a")
                    },
                    new DataValue() {Value = 5, ServerTimestamp = DateTime.MinValue}))
                {
                    SelectedParameterType = this.scalarParameterType
                }
            };
        }

        [Test]
        public void VerifyMapToNewElementDefinition()
        {
            this.iteration.Element.Add(new ElementDefinition(){ Name = "Cap" });
            var timeTaggedValueRowViewModel = new TimeTaggedValueRowViewModel(.2, .2);

            this.variables.Add(new VariableRowViewModel((
                new ReferenceDescription() { NodeId = new ExpandedNodeId(Guid.NewGuid()), DisplayName = new LocalizedText(string.Empty, "Cap.a") },
                new DataValue() { Value = 5, ServerTimestamp = DateTime.MinValue }))
            {
                Values = { timeTaggedValueRowViewModel },
                SelectedValues = { timeTaggedValueRowViewModel },
                SelectedParameterType = this.scalarParameterType
            });

            this.variables.Add(new VariableRowViewModel((
                new ReferenceDescription() { NodeId = new ExpandedNodeId(Guid.NewGuid()), DisplayName = new LocalizedText(string.Empty, "Cap.b") },
                new DataValue() { Value = 5, ServerTimestamp = DateTime.MinValue }))
            {
                Values = { timeTaggedValueRowViewModel },
                SelectedValues = { timeTaggedValueRowViewModel },
                SelectedParameterType = this.dateTimeParameterType
            });

            this.variables.Add(new VariableRowViewModel((
                new ReferenceDescription() { NodeId = new ExpandedNodeId(Guid.NewGuid()), DisplayName = new LocalizedText(string.Empty, "Cap.c") },
                new DataValue() { Value = 5, ServerTimestamp = DateTime.MinValue }))
            {
                Values = { new TimeTaggedValueRowViewModel(6,0) },
                SelectedValues = { new TimeTaggedValueRowViewModel(42, 1) },
                SelectedParameterType = this.quantityKindParameterType,
                SelectedScale = this.scale
            });

            this.variables.FirstOrDefault()?.SelectedValues.Add(new TimeTaggedValueRowViewModel(42, .3));
            var elements = this.rule.Transform(this.variables).elementBases.OfType<ElementDefinition>().ToList();
            Assert.AreEqual(2, elements.Count);
            Assert.AreEqual(3, elements.Last().Parameter.Count);

            var firstParameter = elements.Last().Parameter.First();
            Assert.AreEqual("TextXQuantity", firstParameter.ParameterType.Name);
            var firstParameterValueSet = firstParameter.ValueSet.Last();
            Assert.AreEqual("0.2", firstParameterValueSet.Computed[0]);
            Assert.AreEqual("0.2", firstParameterValueSet.Computed[1]);

            var lastParameter = elements.Last().Parameter.Last();
            Assert.AreEqual("SimpleQuantityKind", lastParameter.ParameterType.Name);
            Assert.AreEqual(this.scale, lastParameter.Scale);
            var parameterValueSet = lastParameter.ValueSet.Last();
            Assert.AreEqual("42", parameterValueSet.Computed[0]);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = parameterValueSet.Computed[1]);
        }
        
        [Test]
        public void VerifyMapToElementUsageParameter()
        {
            var timeTaggedValueRowViewModel = new TimeTaggedValueRowViewModel(.2, .01);
            
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
                new ReferenceDescription() 
                { 
                    NodeId = new ExpandedNodeId(Guid.NewGuid()),
                    DisplayName = new LocalizedText(string.Empty, "Cap.a")
                },
                new DataValue() { Value = 5, ServerTimestamp = DateTime.MinValue }))
            {
                Values = { timeTaggedValueRowViewModel },
                SelectedValues = { timeTaggedValueRowViewModel },
                SelectedOption = new Option(),
                SelectedActualFiniteState = this.actualFiniteStates.ActualState.First(),
                SelectedElementDefinition = elementDefinition,
                SelectedElementUsages = { elementUsage },
                SelectedParameter = parameter,
                SelectedParameterType = parameter.ParameterType
            });

            var elements = this.rule.Transform(this.variables).elementBases.OfType<ElementDefinition>();
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

            this.scale = new RatioScale() { NumberSet = NumberSetKind.REAL_NUMBER_SET };

            this.quantityKindParameterType = new SimpleQuantityKind()
            {
                DefaultScale = this.scale, PossibleScale = { this.scale }, Name = "SimpleQuantityKind"
            };
        }

        [Test]
        public void VerifyUpdateValueSet()
        {
            var parameter0 = new Parameter()
            {
                ParameterType = this.dateTimeParameterType,

                ValueSet =
                {
                    new ParameterValueSet()
                    {
                        Computed = new ValueArray<string>(),
                        Formula = new ValueArray<string>(new[] { "-", "-" }),
                        Manual = new ValueArray<string>(new[] { "-", "-" }),
                        Reference = new ValueArray<string>(new[] { "-", "-" }),
                        Published = new ValueArray<string>(new[] { "-", "-" })
                    }
                }
            };

            var parameter1 = new Parameter()
            {
                ParameterType = this.scalarParameterType,
                ValueSet =
                {
                    new ParameterValueSet()
                    {
                        Computed = new ValueArray<string>(),
                        Formula = new ValueArray<string>(new[] { "-", "-" }),
                        Manual = new ValueArray<string>(new[] { "-", "-" }),
                        Reference = new ValueArray<string>(new[] { "-", "-" }),
                        Published = new ValueArray<string>(new[] { "-", "-" })
                    }
                }
            };

            var parameter2 = new Parameter()
            {
                ParameterType = GenerateTimeQuantityParamerType(),
                ValueSet =
                {
                    new ParameterValueSet()
                    {
                        Computed = new ValueArray<string>(),
                        Formula = new ValueArray<string>(new[] { "-", "-" }),
                        Manual = new ValueArray<string>(new[] { "-", "-" }),
                        Reference = new ValueArray<string>(new[] { "-", "-" }),
                        Published = new ValueArray<string>(new[] { "-", "-" })
                    }
                }
            };

            var parameter3 = new Parameter()
            {
                ParameterType = new TextParameterType(),
                ValueSet =
                {
                    new ParameterValueSet()
                    {
                        Computed = new ValueArray<string>(),
                        Formula = new ValueArray<string>(new[] { "-", "-" }),
                        Manual = new ValueArray<string>(new[] { "-", "-" }),
                        Reference = new ValueArray<string>(new[] { "-", "-" }),
                        Published = new ValueArray<string>(new[] { "-", "-" })
                    }
                }
            };

            _ = new ElementDefinition()
            {
                Parameter = { parameter0, parameter1, parameter2, parameter3 },
                Name = "nonameElement"
            };

            var variableRowViewModel = this.variables.First();
            variableRowViewModel.SelectedValues.AddRange(variableRowViewModel.Values);
            this.rule.Transform(new List<VariableRowViewModel>());

            Assert.DoesNotThrow(() => this.rule.UpdateValueSet(variableRowViewModel, parameter0));
            Assert.DoesNotThrow(() => this.rule.UpdateValueSet(variableRowViewModel, parameter1));
            Assert.DoesNotThrow(() => this.rule.UpdateValueSet(variableRowViewModel, parameter2));
            Assert.DoesNotThrow(() => this.rule.UpdateValueSet(variableRowViewModel, parameter3));
            Assert.Throws<NullReferenceException>(() => this.rule.UpdateValueSet(null, null));
        }

        private static SampledFunctionParameterType GenerateTimeQuantityParamerType()
        {
            return new SampledFunctionParameterType(Guid.NewGuid(), null, null)
            {
                Name = "TextXQuantity",
                IndependentParameterType =
                    {
                        new IndependentParameterTypeAssignment(Guid.NewGuid(), null, null)
                        {
                            ParameterType = new SimpleQuantityKind(Guid.NewGuid(), null, null)
                            {
                                Name = "Time", PossibleScale =
                                {
                                    new RatioScale() { Name = "millisecond" },
                                    new RatioScale() { Name = "second" },
                                    new RatioScale() { Name = "minute" },
                                    new RatioScale() { Name = "hour" },
                                    new RatioScale() { Name = "Day" }
                                }
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
        }
    }
}
