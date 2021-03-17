// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstControllerTestFixture.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2020 RHEA System S.A.
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

namespace DEHPEcosimPro.Tests.DstController
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using CDP4Common;
    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using CDP4Dal;
    using CDP4Dal.Operations;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.Services.ExchangeHistory;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.Views;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Enumerator;
    using DEHPEcosimPro.Services.OpcConnector.Interfaces;
    using DEHPEcosimPro.ViewModel.Rows;

    using DevExpress.Mvvm.Native;

    using Moq;

    using NUnit.Framework;

    using Opc.Ua;

    using INavigationService = DEHPCommon.Services.NavigationService.INavigationService;
    using Node = DevExpress.XtraCharts.Native.Node;

    [TestFixture, Apartment(ApartmentState.STA)]
    public class DstControllerTestFixture
    {
        private DstController controller;
        private Mock<IOpcClientService> opcClient;
        private Mock<IHubController> hubController;
        private Mock<IOpcSessionHandler> opcSessionHandler;
        private Mock<IMappingEngine> mappingEngine;

        private readonly List<ReferenceDescription> referenceDescriptionCollection = new List<ReferenceDescription>
        {
            new ReferenceDescription { NodeId = ExpandedNodeId.Parse("server_methods"), BrowseName = new QualifiedName("server_methods"), NodeClass = NodeClass.Object},
            new ReferenceDescription { NodeId = ExpandedNodeId.Parse("method_run"), BrowseName = new QualifiedName("method_run"), NodeClass = NodeClass.Method},
            new ReferenceDescription { NodeId = ExpandedNodeId.Parse("method_reset"),BrowseName = new QualifiedName("method_reset"), NodeClass = NodeClass.Method},
            new ReferenceDescription { NodeId = ExpandedNodeId.Parse("method_integ_cint"),BrowseName = new QualifiedName("method_integ_cint"), NodeClass = NodeClass.Method},
        };

        private Mock<IStatusBarControlViewModel> statusBarViewModel;
        private Iteration iteration;
        private Assembler assembler;
        private Mock<INavigationService> navigationService;
        private Mock<IExchangeHistoryService> exchangeHistoryService;

        [SetUp]
        public void Setup()
        {
            this.hubController = new Mock<IHubController>();

            this.hubController.Setup(x => x.CurrentDomainOfExpertise).Returns(new DomainOfExpertise());

            var uri = new Uri("http://t.e");
            this.assembler = new Assembler(uri);

            this.iteration =
                new Iteration(Guid.NewGuid(), this.assembler.Cache, uri)
                {
                    Container = new EngineeringModel(Guid.NewGuid(), this.assembler.Cache, uri)
                    {
                        EngineeringModelSetup = new EngineeringModelSetup(Guid.NewGuid(), this.assembler.Cache, uri)
                        {
                            RequiredRdl = { new ModelReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, uri) },
                            Container = new SiteReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, uri)
                            {
                                Container = new SiteDirectory(Guid.NewGuid(), this.assembler.Cache, uri)
                            }
                        }
                    }
                };

            this.assembler.Cache.TryAdd(new CacheKey(this.iteration.Iid, null),new Lazy<Thing>(() => this.iteration));

            this.hubController.Setup(x => x.OpenIteration).Returns(this.iteration);

            this.hubController.Setup(
                    x => x.CreateOrUpdate(
                        It.IsAny<ExternalIdentifierMap>(), It.IsAny<Action<Iteration, ExternalIdentifierMap>>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            this.hubController.Setup(
                x => x.CreateOrUpdate(
                    It.IsAny<IEnumerable<ElementDefinition>>(), It.IsAny<Action<Iteration, ElementDefinition>>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            this.hubController.Setup(
                x => x.CreateOrUpdate(
                    It.IsAny<IEnumerable<IdCorrespondence>>(), It.IsAny<Action<ExternalIdentifierMap, IdCorrespondence>>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            this.hubController.Setup(
                x => x.Delete(
                    It.IsAny<IEnumerable<IdCorrespondence>>(), It.IsAny<Action<ExternalIdentifierMap, IdCorrespondence>>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            this.hubController.Setup(x => x.Write(It.IsAny<ThingTransaction>())).Returns(Task.CompletedTask);

            this.opcSessionHandler = new Mock<IOpcSessionHandler>();

            this.mappingEngine = new Mock<IMappingEngine>();

            this.navigationService = new Mock<INavigationService>();

            this.opcClient = new Mock<IOpcClientService>();
            this.opcClient.Setup(x => x.Connect(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IUserIdentity>())).Returns(Task.CompletedTask);
            this.opcClient.Setup(x => x.OpcClientStatusCode).Returns(OpcClientStatusCode.Connected);
            this.opcClient.Setup(x => x.CloseSession());
            this.opcClient.Setup(x => x.ReadNode(Variables.Server_ServerStatus_StartTime)).Returns(new DataValue(new DateTime(2021, 1, 1)));
            this.opcClient.Setup(x => x.ReadNode(Variables.Server_ServerStatus_CurrentTime)).Returns(new DataValue(new DateTime(2021, 1, 3)));
            
            this.opcClient.Setup(
                x => x.WriteNode(It.IsAny<NodeId>(), It.IsAny<object>(), It.IsAny<bool>())).Returns(true);

            this.opcClient.Setup(x => x.References).Returns(new ReferenceDescriptionCollection(new List<ReferenceDescription>()
            {
                new ReferenceDescription() { NodeId = new ExpandedNodeId(Guid.NewGuid(), 4), BrowseName = new QualifiedName("dummy"), NodeClass = NodeClass.Variable},
                new ReferenceDescription() { NodeId = new ExpandedNodeId(Guid.NewGuid(), 2), BrowseName = new QualifiedName("TIME"), NodeClass = NodeClass.Method}
            }));

            this.statusBarViewModel = new Mock<IStatusBarControlViewModel>();
            this.statusBarViewModel.Setup(x => x.Append(It.IsAny<string>(), It.IsAny<StatusBarMessageSeverity>()));

            this.exchangeHistoryService = new Mock<IExchangeHistoryService>();
            this.exchangeHistoryService.Setup(x => x.Write()).Returns(Task.CompletedTask);

            this.controller = new DstController(this.opcClient.Object, this.hubController.Object, 
                this.opcSessionHandler.Object, this.mappingEngine.Object, this.statusBarViewModel.Object,
                this.navigationService.Object, this.exchangeHistoryService.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.Null(this.controller.ServerAddress);
            Assert.Zero(this.controller.RefreshInterval);
            Assert.IsTrue(this.controller.IsSessionOpen);
            Assert.IsNotEmpty(this.controller.Variables);
            Assert.IsNotNull(this.controller.References);
            Assert.IsNotEmpty(this.controller.Methods);
            Assert.AreEqual(MappingDirection.FromDstToHub, this.controller.MappingDirection);
            Assert.IsEmpty(this.controller.DstMapResult);
            Assert.IsEmpty(this.controller.HubMapResult);
            Assert.IsNull(this.controller.ExternalIdentifierMap);
            Assert.IsNotEmpty(this.controller.ThisToolName);
        }

        [Test]
        public void VerifyConnect()
        {
            Assert.DoesNotThrowAsync(async () => await this.controller.Connect("endpoint", true, null, 50));
            this.opcClient.Verify(x => x.Connect(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IUserIdentity>()), Times.Once);
            this.opcClient.VerifySet(x => x.RefreshInterval = 50);
        }

        [Test]
        public void VerifyClose()
        {
            this.controller.CloseSession();
            this.opcClient.Verify(x => x.CloseSession(), Times.Once);
        }

        [Test]
        public void VerifyAddSubscription()
        {
            Assert.DoesNotThrow(() =>
            {
                this.controller.AddSubscription(new ReferenceDescription() { NodeId = new ExpandedNodeId(Guid.NewGuid()) });
                this.controller.AddSubscription(new ReferenceDescription() { NodeId = new ExpandedNodeId(Guid.NewGuid()) });
            });

            this.opcClient.Verify(x => x.AddSubscription(It.IsAny<NodeId>()), Times.Exactly(2));

            this.controller.ClearSubscriptions();
            this.opcSessionHandler.Verify(x => x.ClearSubscriptions(), Times.Once);
        }

        [Test]
        public void VerifyMap()
        {
            this.opcClient.Setup(x => x.ReadNode(It.IsAny<NodeId>())).Returns(new DataValue());

            this.mappingEngine.Setup(x => x.Map(It.IsAny<object>()))
                .Returns(
                    (new Dictionary<ParameterOrOverrideBase, VariableRowViewModel>() 
                        { 
                            {
                                new Parameter(), new VariableRowViewModel((
                                    new ReferenceDescription()
                                    {
                                        DisplayName = new LocalizedText(string.Empty, "Mos.a"),
                                        NodeId = new NodeId(Guid.NewGuid())
                                    },
                                    new DataValue() { Value = 5, ServerTimestamp = DateTime.MinValue }))
                            }
                        }, 
                        new List<ElementBase>() { new ElementDefinition() }));
            
            this.controller.ExternalIdentifierMap = new ExternalIdentifierMap()
            {
                Container = this.iteration
            };

            Assert.DoesNotThrow(() => this.controller.Map(new List<VariableRowViewModel>()));

            this.mappingEngine.Setup(x => x.Map(It.IsAny<object>()))
                .Returns(
                    (new Dictionary<ParameterOrOverrideBase, VariableRowViewModel>()
                        {
                            {
                                new Parameter(), new VariableRowViewModel((
                                    new ReferenceDescription()
                                    {
                                        DisplayName = new LocalizedText(string.Empty, "Mos.a"),
                                        NodeId = new NodeId(Guid.NewGuid())
                                    },
                                    new DataValue() { Value = 5, ServerTimestamp = DateTime.MinValue }))
                            }
                        },
                        new List<ElementBase>() {}));


            Assert.DoesNotThrow(() => this.controller.Map(new List<VariableRowViewModel>()));

            this.mappingEngine.Setup(x => x.Map(It.IsAny<object>()))
                .Returns(new byte());
            
            Assert.DoesNotThrow(() => this.controller.Map(new List<VariableRowViewModel>()));

            this.mappingEngine.Setup(x => x.Map(It.IsAny<object>())).Throws<InvalidOperationException>();
            Assert.Throws<InvalidOperationException>(() => this.controller.Map(default(List<VariableRowViewModel>)));

            this.mappingEngine.Setup(x => x.Map(It.IsAny<object>()))
                .Returns((new Dictionary<ParameterOrOverrideBase, VariableRowViewModel>()
                {
                    { 
                        new Parameter(), new VariableRowViewModel((
                            new ReferenceDescription()
                            {
                                DisplayName = new LocalizedText(string.Empty, "Mos.a"),
                                NodeId = new NodeId(Guid.NewGuid())
                            },
                            new DataValue() { Value = 5, ServerTimestamp = DateTime.MinValue }))
                    }
                }, new List<ElementBase>()
                {
                    new ElementDefinition()
                }));

            Assert.DoesNotThrow(() => this.controller.Map(new List<VariableRowViewModel>()));
            
            this.mappingEngine.Verify(x => x.Map(It.IsAny<object>()), Times.Exactly(5));
        }

        [Test]
        public void VerifyMapElementDefinitionToDst()
        {
            this.controller.ExternalIdentifierMap = new ExternalIdentifierMap();

            var mappedElement = new List<MappedElementDefinitionRowViewModel>()
            {
                new MappedElementDefinitionRowViewModel()
                {
                    SelectedParameter = new Parameter(),
                    SelectedValue = new ValueSetValueRowViewModel(new ParameterValueSet(), "42", new RatioScale()),
                    SelectedVariable = new VariableRowViewModel((
                        new ReferenceDescription()
                        {
                            DisplayName = new LocalizedText(string.Empty, "Mos.a"),
                            NodeId = new NodeId(Guid.NewGuid())
                        },
                        new DataValue() { Value = 5, ServerTimestamp = DateTime.MinValue }))
                },
                new MappedElementDefinitionRowViewModel()
                {
                    SelectedParameter = new Parameter(),
                    SelectedValue = new ValueSetValueRowViewModel(new ParameterValueSet(), "42", new RatioScale()),
                    SelectedVariable = new VariableRowViewModel((
                        new ReferenceDescription()
                        {
                            DisplayName = new LocalizedText(string.Empty, "Mos.a"),
                            NodeId = new NodeId(Guid.NewGuid())
                        },
                        new DataValue() { Value = 5, ServerTimestamp = DateTime.MinValue }))
                }
            };

            Assert.DoesNotThrow(() => this.controller.Map(mappedElement));
            Assert.AreEqual(2, this.controller.HubMapResult.Count);
        }

        [Test]
        public void VerifyTransferToDst()
        {
            this.controller.ExternalIdentifierMap = new ExternalIdentifierMap(Guid.NewGuid(), null, null)
            {
                Container = new ModelReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, null)
                {
                    Container = new SiteDirectory(Guid.NewGuid(), this.assembler.Cache, null)
                }
            };

            var parameter0 = new Parameter() {ParameterType = new SimpleQuantityKind() {Name = "test"}};
            var parameter1 = new Parameter() {ParameterType = new SimpleQuantityKind() {Name = "test"}};

            _ = new ElementDefinition() { Parameter = { parameter0, parameter1 } };

            this.controller.HubMapResult.AddRange(
                new List<MappedElementDefinitionRowViewModel>()
                {
                    new MappedElementDefinitionRowViewModel()
                    {
                        SelectedParameter = parameter0,
                        SelectedValue = new ValueSetValueRowViewModel(new ParameterValueSet(), "42", new RatioScale()),
                        SelectedVariable = new VariableRowViewModel((
                            new ReferenceDescription()
                            {
                                DisplayName = new LocalizedText(string.Empty, "Mos.a"),
                                NodeId = new NodeId(Guid.NewGuid())
                            },
                            new DataValue() {Value = 5, ServerTimestamp = DateTime.MinValue}))
                    },
                    new MappedElementDefinitionRowViewModel()
                    {
                        SelectedParameter = parameter1,
                        SelectedValue = new ValueSetValueRowViewModel(new ParameterValueSet(), "42", new RatioScale()),
                        SelectedVariable = new VariableRowViewModel((
                            new ReferenceDescription() 
                            {
                                DisplayName = new LocalizedText(string.Empty, "Mos.a"),
                                NodeId = new NodeId(Guid.NewGuid())
                            },
                            new DataValue() {Value = 5, ServerTimestamp = DateTime.MinValue}))
                    }
                });

            Assert.DoesNotThrow(() => this.controller.TransferMappedThingsToDst());

            this.opcClient.Verify(
                x => x.WriteNode(It.IsAny<NodeId>(), It.IsAny<object>(), true), 
                Times.Exactly(2));

            this.exchangeHistoryService.Verify(x => x.Append(It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public void VerifyTransferToHub()
        {
            var map = new ExternalIdentifierMap();

            this.hubController.Setup(x => x.GetThingById(It.IsAny<Guid>(), It.IsAny<Iteration>(), out map));

            this.navigationService.Setup(
                x => x.ShowDxDialog<CreateLogEntryDialog, CreateLogEntryDialogViewModel>(
                It.IsAny<CreateLogEntryDialogViewModel>())).Returns(true);

            this.controller.ExternalIdentifierMap = new ExternalIdentifierMap(Guid.NewGuid(), null, null)
            {
                Container = new ModelReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, null)
                { 
                    Container = new SiteDirectory(Guid.NewGuid(), this.assembler.Cache, null)
                }
            };
            
            Assert.DoesNotThrowAsync(async () => await this.controller.TransferMappedThingsToHub());

            var parameter = new Parameter()
            {
                ParameterType = new SimpleQuantityKind(),
                ValueSet =
                {
                    new ParameterValueSet()
                    {
                        Computed = new ValueArray<string>(new [] {"654321"}),
                        ValueSwitch = ParameterSwitchKind.COMPUTED
                    }
                }
            };

            var elementDefinition = new ElementDefinition()
            {
                Parameter = 
                { 
                    parameter
                }
            };

            this.controller.DstMapResult.Add(elementDefinition);

            var parameterOverride = new ParameterOverride(Guid.NewGuid(), null, null)
            {
                Parameter = parameter,
                ValueSet =
                {
                    new ParameterOverrideValueSet()
                    {
                        Computed = new ValueArray<string>(new [] {"654321"}),
                        ValueSwitch = ParameterSwitchKind.COMPUTED
                    }
                }
            };

            this.controller.DstMapResult.Add(new ElementUsage()
            {
                ElementDefinition = elementDefinition,
                ParameterOverride = 
                {
                    parameterOverride
                }
            });

            this.hubController.Setup(x => 
                x.GetThingById(It.IsAny<Guid>(), It.IsAny<Iteration>(), out parameter));

            this.hubController.Setup(x => 
                x.GetThingById(parameterOverride.Iid, It.IsAny<Iteration>(), out parameterOverride));

            Assert.DoesNotThrowAsync(async() => await this.controller.TransferMappedThingsToHub());

            this.navigationService.Setup(
                x => x.ShowDxDialog<CreateLogEntryDialog, CreateLogEntryDialogViewModel>(
                    It.IsAny<CreateLogEntryDialogViewModel>())).Returns(false);
            
            Assert.DoesNotThrowAsync(async() => await this.controller.TransferMappedThingsToHub());
            
            this.navigationService.Setup(
                x => x.ShowDxDialog<CreateLogEntryDialog, CreateLogEntryDialogViewModel>(
                    It.IsAny<CreateLogEntryDialogViewModel>())).Returns(default(bool?));
            
            Assert.DoesNotThrowAsync(async() => await this.controller.TransferMappedThingsToHub());
            
            this.navigationService.Setup(
                x => x.ShowDxDialog<CreateLogEntryDialog, CreateLogEntryDialogViewModel>(
                    It.IsAny<CreateLogEntryDialogViewModel>())).Returns(true);
            
            Assert.DoesNotThrowAsync(async() => await this.controller.TransferMappedThingsToHub());
            
            this.controller.DstMapResult.Clear();

            Assert.DoesNotThrowAsync(async() => await this.controller.TransferMappedThingsToHub());
            
            this.navigationService.Verify(
                x => 
                    x.ShowDxDialog<CreateLogEntryDialog, CreateLogEntryDialogViewModel>(
                        It.IsAny<CreateLogEntryDialogViewModel>())
                , Times.Exactly(1));
            
            this.hubController.Verify(
                x => x.Write(It.IsAny<ThingTransaction>()), Times.Exactly(2));

            this.hubController.Verify(
                x => x.Refresh(), Times.Exactly(1));
            
            this.exchangeHistoryService.Verify(x => 
                x.Append(It.IsAny<Thing>(), It.IsAny<ChangeKind>()), Times.Exactly(3));

            this.exchangeHistoryService.Verify(x => 
                x.Append(It.IsAny<ParameterValueSetBase>(), It.IsAny<IValueSet>()), Times.Exactly(2));
        }

        [Test]
        public void VerifyCallServerMethod()
        {
            this.opcClient.Setup(x => x.References).Returns(new ReferenceDescriptionCollection(this.referenceDescriptionCollection));

            foreach (var referenceDescription in this.referenceDescriptionCollection.Where(r => r.NodeClass == NodeClass.Method))
            {
                this.controller.Methods.Add(referenceDescription);
            }

            Assert.IsNull(this.controller.CallServerMethod("unknown_method"));

            Assert.DoesNotThrow(() => this.controller.CallServerMethod("method_run"));
            this.opcClient.Verify(x => x.CallMethod(new NodeId("server_methods"), new NodeId("method_run"), string.Empty), Times.Once);

            Assert.DoesNotThrow(() => this.controller.CallServerMethod("method_reset"));
            this.opcClient.Verify(x => x.CallMethod(new NodeId("server_methods"), new NodeId("method_reset"), string.Empty), Times.Once);
        }

        [Test]
        public void VerifyGetServerStartTime()
        {
            this.controller.IsSessionOpen = false;
            Assert.IsNull(this.controller.GetServerStartTime());

            this.controller.IsSessionOpen = true;
            Assert.AreEqual(new DateTime(2021, 1, 1), this.controller.GetServerStartTime());
            this.opcClient.Verify(x => x.ReadNode(Variables.Server_ServerStatus_StartTime), Times.Once);
        }

        [Test]
        public void VerifyGetCurrentServerTime()
        {
            this.controller.IsSessionOpen = false;
            Assert.IsNull(this.controller.GetCurrentServerTime());

            this.controller.IsSessionOpen = true;
            Assert.AreEqual(new DateTime(2021, 1, 3), this.controller.GetCurrentServerTime());
            this.opcClient.Verify(x => x.ReadNode(Variables.Server_ServerStatus_CurrentTime), Times.Once);
        }

        [Test]
        public void VerifyCreateExternalIdentifierMap()
        {
            var newExternalIdentifierMap = this.controller.CreateExternalIdentifierMap("Name");
            this.controller.ExternalIdentifierMap = newExternalIdentifierMap;
            Assert.AreEqual("Name", this.controller.ExternalIdentifierMap.Name);
            Assert.AreEqual("Name", this.controller.ExternalIdentifierMap.ExternalModelName);
        }

        [Test]
        public void VerifyAddToExternalIdentifierMap()
        {
            this.controller.ExternalIdentifierMap = this.controller.CreateExternalIdentifierMap("test");
            var internalId = Guid.NewGuid();
            this.controller.AddToExternalIdentifierMap(internalId, string.Empty);
            Assert.IsNotEmpty(this.controller.ExternalIdentifierMap.Correspondence);
            this.controller.AddToExternalIdentifierMap(internalId, string.Empty);
            this.controller.AddToExternalIdentifierMap(Guid.NewGuid(), string.Empty);
            Assert.AreEqual(1, this.controller.ExternalIdentifierMap.Correspondence.Count);
        }

        [Test]
        public void VerifyReadNode()
        {
            Assert.DoesNotThrow(() => this.controller.ReadNode(new ReferenceDescription() { DisplayName = new LocalizedText(string.Empty, "Mos.a") }));
            this.opcClient.Verify(x => x.ReadNode(It.IsAny<NodeId>()), Times.Exactly(2));
        }

        [Test]
        public void VerifyIsVariableWritable()
        {
            this.opcClient.Setup(x => x.WriteNode(It.IsAny<NodeId>(), It.IsAny<object>(), false)).Returns(true);
            this.opcClient.Setup(x => x.ReadNode(It.IsAny<NodeId>())).Returns(new DataValue(new Variant(42)));
            var referenceDescription = new ReferenceDescription() { NodeId = new ExpandedNodeId(Guid.NewGuid()), DisplayName = new LocalizedText(string.Empty, "Mos.a") };
            Assert.IsTrue(this.controller.IsVariableWritable(referenceDescription));
            this.opcClient.Setup(x => x.WriteNode(It.IsAny<NodeId>(), It.IsAny<object>(), false)).Returns(false);
            Assert.IsFalse(this.controller.IsVariableWritable(referenceDescription));
            this.opcClient.Verify(x => x.WriteNode(It.IsAny<NodeId>(), It.IsAny<object>(), false), Times.Exactly(2));
            this.opcClient.Verify(x => x.ReadNode(It.IsAny<NodeId>()), Times.Exactly(3));
        }

        [Test]
        public void VerifyUpdateValueSets()
        {
            var element = new ElementDefinition();
            
            var parameter = new Parameter()
            {
                ValueSet =
                {
                    new ParameterValueSet()
                    {
                        Manual = new ValueArray<string>(new [] {"nok"}),
                        ValueSwitch = ParameterSwitchKind.MANUAL
                    }
                }
            };

            var elementUsage = new ElementUsage()
            {
                ElementDefinition = element
            };

            var parameterOverride = new ParameterOverride()
            {
                Parameter = parameter,
                ValueSet =
                {
                    new ParameterOverrideValueSet()
                    {
                        Reference = new ValueArray<string>(new[] { "nokeither" }),
                        ValueSwitch = ParameterSwitchKind.REFERENCE
                    }
                }
            };

            element.Parameter.Add(parameter);
            elementUsage.ParameterOverride.Add(parameterOverride);
            element.ContainedElement.Add(elementUsage);

            this.hubController.Setup(x => x.GetThingById(
                It.IsAny<Guid>(), It.IsAny<Iteration>(), out parameter)).Returns(true);

            this.hubController.Setup(x => x.GetThingById(
                It.IsAny<Guid>(), It.IsAny<Iteration>(), out parameterOverride)).Returns(true);

            this.controller.DstMapResult.Add(element);

            Assert.DoesNotThrowAsync(async () => await this.controller.UpdateParametersValueSets());
            
            this.hubController.Verify(x => 
                x.Write(It.IsAny<ThingTransaction>()), Times.Once);
        }

        [Test]
        public void VerifyRetransferToDst()
        {
            this.controller.ReTransferMappedThingsToDst();
            this.VerifyTransferToDst();
            this.controller.ReTransferMappedThingsToDst();
            this.opcClient.Verify(x => x.WriteNode(It.IsAny<NodeId>(), It.IsAny<double>(), false), Times.Exactly(2));
        }

        [Test]
        public void VerifyReadAllNode()
        {
            this.opcClient.Setup(x => x.ReadNode(It.IsAny<NodeId>())).Returns(new DataValue(new Variant(42)));
            Assert.DoesNotThrow(() => this.controller.ReadAllNode(0));
            this.controller.Variables.Clear();
            Assert.DoesNotThrow(() => this.controller.ReadAllNode(0));
            this.opcClient.Verify(x => x.ReadNode(It.IsAny<NodeId>()), Times.Exactly(2));
        }

        [Test]
        public void VerifyResetVariables()
        {
            this.opcClient.Setup(x => x.ReadNode(It.IsAny<NodeId>())).Returns(new DataValue(new Variant(42)));
            Assert.DoesNotThrow(() => this.controller.ResetVariables());
            this.controller.Variables.Clear();
            Assert.DoesNotThrow(() => this.controller.ResetVariables());
            this.opcClient.Verify(x => x.ReadNode(It.IsAny<NodeId>()), Times.Exactly(2));
        }

        [Test]
        public void VerifyGetNextExperimentStep()
        {
            this.opcClient.Setup(x => x.References).Returns(new ReferenceDescriptionCollection(this.referenceDescriptionCollection));

            this.referenceDescriptionCollection.Where(r => r.NodeClass == NodeClass.Method)
                .ForEach(x => this.controller.Methods.Add(x));
            
            this.opcClient.Setup(x => x.ReadNode(It.IsAny<NodeId>())).Returns(new DataValue(new Variant(2)));
            Assert.DoesNotThrow(() => this.controller.GetNextExperimentStep());

            this.opcClient.Verify(x =>
                    x.CallMethod(It.IsAny<NodeId>(), It.IsAny<NodeId>(), It.IsAny<string>()),
                Times.Once);

            this.opcClient.Verify(x => x.ReadNode(It.IsAny<NodeId>()), Times.Exactly(3));
        }

        [Test]
        public void VerifyWriteToDst()
        {
            var nodeId = new NodeId(Guid.Empty);

            this.controller.WriteToDst(nodeId, 42);
            
            this.opcClient.Verify(x => 
                x.WriteNode(nodeId, 42d, true), Times.Once);
        }
    }
}
