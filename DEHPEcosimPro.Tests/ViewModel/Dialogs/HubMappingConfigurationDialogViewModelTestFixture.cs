// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubMappingConfigurationDialogViewModelTestFixture.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2021 RHEA System S.A.
// 
//    Author: Sam Gerené, Alex Vorobiev, Alexander van Delft, Nathanael Smiechowski, Arielle Petit.
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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reactive.Concurrency;

    using Castle.Components.DictionaryAdapter;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using CDP4Dal;
    using CDP4Dal.Permission;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Services.MappingConfiguration;
    using DEHPEcosimPro.ViewModel.Dialogs;
    using DEHPEcosimPro.ViewModel.Rows;

    using Moq;

    using NUnit.Framework;

    using Opc.Ua;

    using ReactiveUI;

    [TestFixture]
    public class HubMappingConfigurationDialogViewModelTestFixture
    {
        [SetUp]
        public void Setup()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;

            this.mappingConfigurationService = new Mock<IMappingConfigurationService>();

            this.hubController = new Mock<IHubController>();
            this.dstController = new Mock<IDstController>();
            this.SetupDstData();
            this.dstController.Setup(x => x.Variables).Returns(this.variables);

            this.dstController.Setup(x => x.ReadNode(It.IsAny<ReferenceDescription>()))
                .Returns(new DataValue(new Variant(42)));

            this.dstController.Setup(x => x.IsVariableWritable(It.IsAny<ReferenceDescription>()))
                .Returns(true);

            this.statusBar = new Mock<IStatusBarControlViewModel>();
            this.navigation = new Mock<INavigationService>();

            this.sitedir = new SiteDirectory(Guid.NewGuid(), null, null);
            this.srdl = new SiteReferenceDataLibrary(Guid.NewGuid(), null, null);
            this.mrdl = new ModelReferenceDataLibrary(Guid.NewGuid(), null, null) { RequiredRdl = this.srdl };

            this.iterationSetup = new IterationSetup(Guid.NewGuid(), null, null);

            var person = new Person(Guid.NewGuid(), null, null) { GivenName = "test", Surname = "test" };
            var participant = new Participant(Guid.NewGuid(), null, null) { Person = person, SelectedDomain = this.domain };

            this.engineeringSetup = new EngineeringModelSetup(Guid.NewGuid(), null, null)
            {
                IterationSetup = { this.iterationSetup },
                RequiredRdl = { this.mrdl }
            };

            this.sitedir.Model.Add(this.engineeringSetup);
            this.sitedir.SiteReferenceDataLibrary.Add(this.srdl);

            this.iteration = new Iteration(Guid.NewGuid(), null, null)
            {
                TopElement = this.element0,
                IterationSetup = this.iterationSetup
            };

            _ = new EngineeringModel(Guid.NewGuid(), null, null)
            {
                EngineeringModelSetup = this.engineeringSetup,
                Iteration = { this.iteration }
            };

            this.SetupElements();

            this.domain = new DomainOfExpertise(Guid.NewGuid(), null, null) { Name = "TestDomain", ShortName = "TD" };

            this.session.Setup(x => x.ActivePerson).Returns(person);
            this.engineeringSetup.Participant.Add(participant);

            this.iteration.Option.Add(this.option1);
            this.session = new Mock<ISession>();
            this.session.Setup(x => x.DataSourceUri).Returns("dataSourceUri");

            this.permissionService = new Mock<IPermissionService>();
            this.permissionService.Setup(x => x.Session).Returns(this.session.Object);
            this.permissionService.Setup(x => x.CanRead(It.IsAny<Thing>())).Returns(true);
            this.permissionService.Setup(x => x.CanWrite(It.IsAny<Thing>())).Returns(true);
            this.session.Setup(x => x.PermissionService).Returns(this.permissionService.Object);

            this.session.Setup(x => x.OpenIterations).Returns(new ConcurrentDictionary<Iteration, Tuple<DomainOfExpertise, Participant>>(
                new List<KeyValuePair<Iteration, Tuple<DomainOfExpertise, Participant>>>
                {
                    new KeyValuePair<Iteration, Tuple<DomainOfExpertise, Participant>>(this.iteration, new Tuple<DomainOfExpertise, Participant>(this.domain, new Participant()))
                }));

            this.viewModel = new HubMappingConfigurationDialogViewModel(this.hubController.Object, this.dstController.Object, this.statusBar.Object, this.mappingConfigurationService.Object, this.navigation.Object);

            var browser = new ElementDefinitionsBrowserViewModel(this.iteration, this.session.Object);
            this.elementDefinitionRows = new List<ElementDefinitionRowViewModel>();
            this.elementDefinitionRows.AddRange(browser.ContainedRows.OfType<ElementDefinitionRowViewModel>());
        }

        private HubMappingConfigurationDialogViewModel viewModel;
        private Mock<IHubController> hubController;
        private Mock<IDstController> dstController;
        private Mock<IStatusBarControlViewModel> statusBar;
        private Mock<INavigationService> navigation;
        private List<ElementDefinitionRowViewModel> elementDefinitionRows;
        private Mock<ISession> session;
        private ElementDefinition element0;
        private IterationSetup iterationSetup;
        private EngineeringModelSetup engineeringSetup;
        private Iteration iteration;
        private DomainOfExpertise domain;
        private Mock<IPermissionService> permissionService;
        private SiteReferenceDataLibrary srdl;
        private ModelReferenceDataLibrary mrdl;
        private SiteDirectory sitedir;
        private Option option1;
        private ActualFiniteStateList stateList;
        private PossibleFiniteState state1;
        private PossibleFiniteState state2;
        private PossibleFiniteStateList posStateList;
        private DomainOfExpertise domain2;
        private SimpleQuantityKind qqParamType;
        private ArrayParameterType apType;
        private CompoundParameterType cptType;
        private ElementDefinition element0ForUsage1;
        private ElementDefinition element0ForUsage2;
        private ElementUsage elementUsage1;
        private ElementUsage elementUsage2;
        private ParameterGroup parameterGroup1;
        private ParameterGroup parameterGroup2;
        private ParameterGroup parameterGroup3;
        private ParameterGroup parameterGroup1ForUsage1;
        private ParameterGroup parameterGroup2ForUsage2;
        private ParameterGroup parameterGroup3ForUsage1;
        private Parameter parameter1;
        private Parameter parameter4;
        private Parameter parameterCompoundForSubscription;
        private ParameterSubscription parameterSubscriptionCompound;
        private IList<(ReferenceDescription Reference, DataValue Node)> variables;
        private MeasurementScale measurementScale;
        private Mock<IMappingConfigurationService> mappingConfigurationService;

        private void SetupDstData()
        {
            this.variables = new List<(ReferenceDescription, DataValue)>
            {
                (new ReferenceDescription
                {
                    DisplayName = LocalizedText.ToLocalizedText("a"),
                    NodeId = new ExpandedNodeId(Guid.NewGuid(), 4), NodeClass = NodeClass.Variable
                }, new DataValue(new Variant(42))),
                (new ReferenceDescription
                {
                    DisplayName = LocalizedText.ToLocalizedText("b"),
                    NodeId = new ExpandedNodeId(Guid.NewGuid(), 4), NodeClass = NodeClass.Variable
                }, new DataValue(new Variant(1764))),
                (new ReferenceDescription
                {
                    DisplayName = LocalizedText.ToLocalizedText("c"),
                    NodeId = new ExpandedNodeId(Guid.NewGuid(), 4), NodeClass = NodeClass.Variable
                }, new DataValue(new Variant(74088)))
            };
        }

        private void SetupElements()
        {
            this.option1 = new Option(Guid.NewGuid(), null, null);

            this.stateList = new ActualFiniteStateList(Guid.NewGuid(), null, null);
            this.state1 = new PossibleFiniteState(Guid.NewGuid(), null, null);
            this.state2 = new PossibleFiniteState(Guid.NewGuid(), null, null);

            this.posStateList = new PossibleFiniteStateList(Guid.NewGuid(), null, null);
            this.posStateList.PossibleState.Add(this.state1);
            this.posStateList.PossibleState.Add(this.state2);
            this.posStateList.DefaultState = this.state1;

            this.stateList.ActualState.Add(new ActualFiniteState(Guid.NewGuid(), null, null)
            {
                PossibleState = new List<PossibleFiniteState> { this.state1 },
                Kind = ActualFiniteStateKind.MANDATORY
            });

            this.stateList.ActualState.Add(new ActualFiniteState(Guid.NewGuid(), null, null)
            {
                PossibleState = new List<PossibleFiniteState> { this.state2 },
                Kind = ActualFiniteStateKind.FORBIDDEN
            });

            this.domain2 = new DomainOfExpertise(Guid.NewGuid(), null, null);
            this.session = new Mock<ISession>();

            this.measurementScale = new RatioScale(Guid.NewGuid(), null, null)
                { Name = "a", ShortName = "a", NumberSet = NumberSetKind.REAL_NUMBER_SET };

            this.qqParamType = new SimpleQuantityKind(Guid.NewGuid(), null, null)
            {
                Name = "PTName",
                ShortName = "PTShortName",
                PossibleScale = { this.measurementScale },
                DefaultScale = this.measurementScale
            };

            // Array parameter type with components
            this.apType = new ArrayParameterType(Guid.NewGuid(), null, null)
            {
                Name = "APTName",
                ShortName = "APTShortName"
            };

            this.apType.Component.Add(new ParameterTypeComponent(Guid.NewGuid(), null, null)
            {
                Iid = Guid.NewGuid(),
                ParameterType = this.qqParamType
            });

            this.apType.Component.Add(new ParameterTypeComponent(Guid.NewGuid(), null, null)
            {
                Iid = Guid.NewGuid(),
                ParameterType = this.qqParamType
            });

            // compound parameter type with components
            this.cptType = new CompoundParameterType(Guid.NewGuid(), null, null)
            {
                Name = "APTName",
                ShortName = "APTShortName"
            };

            this.cptType.Component.Add(new ParameterTypeComponent(Guid.NewGuid(), null, null)
            {
                Iid = Guid.NewGuid(),
                ParameterType = this.qqParamType
            });

            this.cptType.Component.Add(new ParameterTypeComponent(Guid.NewGuid(), null, null)
            {
                Iid = Guid.NewGuid(),
                ParameterType = this.qqParamType
            });

            this.element0 = new ElementDefinition(Guid.NewGuid(), null, null)
            {
                Owner = this.domain
            };

            this.element0ForUsage1 = new ElementDefinition(Guid.NewGuid(), null, null)
            {
                Owner = this.domain2
            };

            this.element0ForUsage2 = new ElementDefinition(Guid.NewGuid(), null, null)
            {
                Owner = this.domain2
            };

            this.elementUsage1 = new ElementUsage(Guid.NewGuid(), null, null)
            {
                Owner = this.domain2
            };

            this.elementUsage2 = new ElementUsage(Guid.NewGuid(), null, null)
            {
                Owner = this.domain2
            };

            this.elementUsage1.ElementDefinition = this.element0ForUsage1;
            this.elementUsage2.ElementDefinition = this.element0ForUsage2;

            this.parameterGroup1 = new ParameterGroup(Guid.NewGuid(), null, null);
            this.parameterGroup2 = new ParameterGroup(Guid.NewGuid(), null, null);
            this.parameterGroup3 = new ParameterGroup(Guid.NewGuid(), null, null);

            this.parameterGroup1ForUsage1 = new ParameterGroup(Guid.NewGuid(), null, null);
            this.parameterGroup2ForUsage2 = new ParameterGroup(Guid.NewGuid(), null, null);
            this.parameterGroup3ForUsage1 = new ParameterGroup(Guid.NewGuid(), null, null);

            this.parameter1 = new Parameter(Guid.NewGuid(), null, null)
            {
                ParameterType = this.qqParamType,
                Owner = this.domain
            };

            this.parameter4 = new Parameter(Guid.NewGuid(), null, null)
            {
                ParameterType = this.qqParamType,
                Owner = this.domain2
            };

            this.parameterCompoundForSubscription = new Parameter(Guid.NewGuid(), null, null)
            {
                ParameterType = this.cptType,
                Owner = this.domain2
            };

            this.parameterSubscriptionCompound = new ParameterSubscription(Guid.NewGuid(), null, null)
            {
                Owner = this.domain
            };

            this.parameterCompoundForSubscription.ParameterSubscription.Add(this.parameterSubscriptionCompound);

            this.iteration.Element.Add(this.element0);
            this.element0.ParameterGroup.Add(this.parameterGroup1);
            this.element0.ParameterGroup.Add(this.parameterGroup2);
            this.element0.ParameterGroup.Add(this.parameterGroup3);
            this.element0ForUsage1.ParameterGroup.Add(this.parameterGroup1ForUsage1);
            this.element0ForUsage2.ParameterGroup.Add(this.parameterGroup2ForUsage2);
            this.element0ForUsage1.ParameterGroup.Add(this.parameterGroup3ForUsage1);

            this.iteration.Element.Add(this.element0ForUsage1);
            this.iteration.Element.Add(this.element0ForUsage2);

            this.parameterGroup3.ContainingGroup = this.parameterGroup1;
            this.parameterGroup3ForUsage1.ContainingGroup = this.parameterGroup1ForUsage1;

            this.parameter4.Group = this.parameterGroup3;
        }

        [Test]
        public void VerifyChoosingDialog()
        {
            var listOfVariableRow = new List<VariableRowViewModel>
            {
                new VariableRowViewModel((
                    new ReferenceDescription
                    {
                        DisplayName = new LocalizedText(string.Empty, "Mos.a[1,1]"),
                        NodeId = new NodeId(Guid.NewGuid())
                    },
                    new DataValue { Value = 6, ServerTimestamp = DateTime.MinValue })),
                new VariableRowViewModel((
                    new ReferenceDescription
                    {
                        DisplayName = new LocalizedText(string.Empty, "Mos.a[2,1]"),
                        NodeId = new NodeId(Guid.NewGuid())
                    },
                    new DataValue { Value = 5, ServerTimestamp = DateTime.MinValue }))
            };

            var array = new ArrayVariableRowViewModel("", listOfVariableRow);

            var parameter = new Parameter
            {
                Iid = Guid.NewGuid(),
                ParameterType = new SampledFunctionParameterType(Guid.NewGuid(), null, null)
                {
                    Name = "a",
                    IndependentParameterType =
                    {
                        new IndependentParameterTypeAssignment(Guid.NewGuid(), null, null)
                        {
                            ParameterType = new DateTimeParameterType(Guid.NewGuid(), null, null)
                            {
                                ShortName = "Timestamp"
                            }
                        }
                    },
                    DependentParameterType =
                    {
                        new DependentParameterTypeAssignment(Guid.NewGuid(), null, null)
                        {
                            ParameterType = new SimpleQuantityKind(Guid.NewGuid(), null, null)
                            {
                                ShortName = "Value"
                            }
                        }
                    }
                }
            };
            var elementDefinition = new ElementDefinition(Guid.NewGuid(), null, null)
            {
                Owner = this.domain,
                ShortName = "Element"
            };
            elementDefinition.Parameter.Add(parameter);


            parameter.ValueSet.Add(new ParameterValueSet
            {
                Computed = new ValueArray<string>(new[] { "20", "21" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED
            });

            var empTychoosingDialog = new ChooseMappingColumnsViewModel();
            var choosingDialog = new ChooseMappingColumnsViewModel(array, parameter);

            var b = choosingDialog.ListOfParameterToMatch;
            var c = choosingDialog.ListOfVariableToMap;
            var d = choosingDialog.VariableName;
            var e = choosingDialog.ParameterName;
            var a = choosingDialog.IsList;
            choosingDialog.VariableName = "";
            choosingDialog.ParameterName = "";
            choosingDialog.IsList = false;
            choosingDialog.ListOfParameterToMatch = new ReactiveList<string>();
            Assert.IsNotNull(c);
        }

        [Test]
        public void VerifyChoosingDialogBiggerArray()
        {
            var listOfVariableRow = new List<VariableRowViewModel>
            {
                new VariableRowViewModel((
                    new ReferenceDescription
                    {
                        DisplayName = new LocalizedText(string.Empty, "Mos.a[1,1]"),
                        NodeId = new NodeId(Guid.NewGuid())
                    },
                    new DataValue { Value = 6, ServerTimestamp = DateTime.MinValue })),
                new VariableRowViewModel((
                    new ReferenceDescription
                    {
                        DisplayName = new LocalizedText(string.Empty, "Mos.a[2,1]"),
                        NodeId = new NodeId(Guid.NewGuid())
                    },
                    new DataValue { Value = 5, ServerTimestamp = DateTime.MinValue })),
                new VariableRowViewModel((
                    new ReferenceDescription
                    {
                        DisplayName = new LocalizedText(string.Empty, "Mos.a[2,2]"),
                        NodeId = new NodeId(Guid.NewGuid())
                    },
                    new DataValue { Value = 4, ServerTimestamp = DateTime.MinValue })),
                new VariableRowViewModel((
                    new ReferenceDescription
                    {
                        DisplayName = new LocalizedText(string.Empty, "Mos.a[2,2]"),
                        NodeId = new NodeId(Guid.NewGuid())
                    },
                    new DataValue { Value = 7, ServerTimestamp = DateTime.MinValue }))
            };

            var array = new ArrayVariableRowViewModel("", listOfVariableRow);

            var parameter = new Parameter
            {
                Iid = Guid.NewGuid(),
                ParameterType = new SampledFunctionParameterType(Guid.NewGuid(), null, null)
                {
                    Name = "a",
                    IndependentParameterType =
                    {
                        new IndependentParameterTypeAssignment(Guid.NewGuid(), null, null)
                        {
                            ParameterType = new DateTimeParameterType(Guid.NewGuid(), null, null)
                            {
                                ShortName = "Timestamp"
                            }
                        }
                    },
                    DependentParameterType =
                    {
                        new DependentParameterTypeAssignment(Guid.NewGuid(), null, null)
                        {
                            ParameterType = new SimpleQuantityKind(Guid.NewGuid(), null, null)
                            {
                                ShortName = "Value"
                            }
                        }
                    }
                }
            };

            parameter.ValueSet.Add(new ParameterValueSet
            {
                Computed = new ValueArray<string>(new[] { "20", "21" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED
            });
            var elementDefinition = new ElementDefinition(Guid.NewGuid(), null, null)
            {
                Owner = this.domain,
                ShortName = "Element"
            };
            elementDefinition.Parameter.Add(parameter);

            var choosingDialog = new ChooseMappingColumnsViewModel(array, parameter);
            var a = choosingDialog.ListOfVariableToMap;

            var b = a.FirstOrDefault().Index;
            a.FirstOrDefault().Index = "[1]";
            var c = a.FirstOrDefault().SelectedColumnMatched;
            a.FirstOrDefault().SelectedColumnMatched = "Timestamp";

            Assert.IsNotNull(a);
        }

        [Test]
        public void VerifyChoosingRowDialog()
        {
            var listOfVariableRow = new List<VariableRowViewModel>
            {
                new VariableRowViewModel((
                    new ReferenceDescription
                    {
                        DisplayName = new LocalizedText(string.Empty, "Mos.a[1,1]"),
                        NodeId = new NodeId(Guid.NewGuid())
                    },
                    new DataValue { Value = 6, ServerTimestamp = DateTime.MinValue })),
                new VariableRowViewModel((
                    new ReferenceDescription
                    {
                        DisplayName = new LocalizedText(string.Empty, "Mos.a[2,1]"),
                        NodeId = new NodeId(Guid.NewGuid())
                    },
                    new DataValue { Value = 5, ServerTimestamp = DateTime.MinValue })),
                new VariableRowViewModel((
                    new ReferenceDescription
                    {
                        DisplayName = new LocalizedText(string.Empty, "Mos.a[2,2]"),
                        NodeId = new NodeId(Guid.NewGuid())
                    },
                    new DataValue { Value = 4, ServerTimestamp = DateTime.MinValue })),
                new VariableRowViewModel((
                    new ReferenceDescription
                    {
                        DisplayName = new LocalizedText(string.Empty, "Mos.a[2,2]"),
                        NodeId = new NodeId(Guid.NewGuid())
                    },
                    new DataValue { Value = 7, ServerTimestamp = DateTime.MinValue }))
            };

            var choosingRowEmpty = new ChooseMappingRowsViewModel();
            var choosingRow = new ChooseMappingRowsViewModel(listOfVariableRow);
        }

        [Test]
        public void VerifyContinueCommand()
        {
            var mappedElementDefinitionRowViewModel = new MappedElementDefinitionRowViewModel();

            this.viewModel.MappedElements.Add(mappedElementDefinitionRowViewModel);
            mappedElementDefinitionRowViewModel.VerifyValidity();
            Assert.IsFalse(mappedElementDefinitionRowViewModel.IsValid);
            mappedElementDefinitionRowViewModel.SelectedVariable = (VariableRowViewModel) this.viewModel.AvailableVariables.First();
            mappedElementDefinitionRowViewModel.VerifyValidity();
            Assert.IsFalse(mappedElementDefinitionRowViewModel.IsValid);
            mappedElementDefinitionRowViewModel.SelectedParameter = this.parameter1;
            this.viewModel.AvailableVariables.First().HasWriteAccess = true;
            mappedElementDefinitionRowViewModel.VerifyValidity();
            Assert.IsFalse(mappedElementDefinitionRowViewModel.IsValid);

            mappedElementDefinitionRowViewModel.SelectedValue = new ValueSetValueRowViewModel(
                new ParameterValueSet(Guid.NewGuid(), null, null), "15", new CyclicRatioScale());

            mappedElementDefinitionRowViewModel.VerifyValidity();
            Assert.IsTrue(mappedElementDefinitionRowViewModel.IsValid);
            Assert.IsFalse(this.viewModel.CanContinue);
            mappedElementDefinitionRowViewModel.VerifyValidity();
            this.viewModel.CheckCanExecute();
            Assert.IsTrue(this.viewModel.CanContinue);
            Assert.IsTrue(this.viewModel.ContinueCommand.CanExecute(null));
            Assert.DoesNotThrow(() => this.viewModel.ContinueCommand.Execute(null));
        }

        [Test]
        public void VerifyMappedElements()
        {
            this.viewModel.Elements.AddRange(this.elementDefinitionRows);
            Assert.DoesNotThrow(() => this.viewModel.CreateMappedElements());
            Assert.IsEmpty(this.viewModel.MappedElements);
        }

        [Test]
        public void VerifyProperties()
        {
            this.viewModel.Elements.AddRange(this.elementDefinitionRows);
            Assert.IsNotEmpty(this.viewModel.ElementDefinitions);
            Assert.IsNotEmpty(this.viewModel.Elements);
            Assert.IsEmpty(this.viewModel.Values);
            Assert.IsEmpty(this.viewModel.MappedElements);
            Assert.AreEqual(3, this.viewModel.AvailableVariables.Count);
            Assert.IsEmpty(this.viewModel.ElementUsages);
            Assert.IsNull(this.viewModel.SelectedElementUsage);
            Assert.IsNull(this.viewModel.SelectedElementDefinition);
        }

        [Test]
        public void VerifyTuples()
        {
            var parameter = new Parameter
            {
                Iid = Guid.NewGuid(),
                ParameterType = new SampledFunctionParameterType(Guid.NewGuid(), null, null)
                {
                    Name = "a",
                    IndependentParameterType =
                    {
                        new IndependentParameterTypeAssignment(Guid.NewGuid(), null, null)
                        {
                            ParameterType = new DateTimeParameterType(Guid.NewGuid(), null, null)
                            {
                                ShortName = "Timestamp"
                            }
                        }
                    },
                    DependentParameterType =
                    {
                        new DependentParameterTypeAssignment(Guid.NewGuid(), null, null)
                        {
                            ParameterType = new SimpleQuantityKind(Guid.NewGuid(), null, null)
                            {
                                ShortName = "Value"
                            }
                        }
                    }
                }
            };

            parameter.ValueSet.Add(new ParameterValueSet
            {
                Published = new ValueArray<string>(new[] { "20", "21" }),
                Computed = new ValueArray<string>(new[] { "20", "21" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED
            });

            var empty = new ValueSetsToTableViewModel();
            var table = new ValueSetsToTableViewModel(parameter, this.option1, null);
            var a = table.PublishedValueTable;
            table.PublishedValueTable = new DataTable();
            var b = table.ListOfTuple;
            table.ListOfTuple = new EditableList<(string name, List<string> list)>();
        }

        [Test]
        public void VerifyVariableTypesAsArrayAreCompatible()
        {
            var listOfVariableRow = new List<VariableRowViewModel>
            {
                new VariableRowViewModel((
                    new ReferenceDescription
                    {
                        DisplayName = new LocalizedText(string.Empty, "Mos.a[1]"),
                        NodeId = new NodeId(Guid.NewGuid())
                    },
                    new DataValue { Value = 6, ServerTimestamp = DateTime.MinValue })),
                new VariableRowViewModel((
                    new ReferenceDescription
                    {
                        DisplayName = new LocalizedText(string.Empty, "Mos.a[2]"),
                        NodeId = new NodeId(Guid.NewGuid())
                    },
                    new DataValue { Value = 5, ServerTimestamp = DateTime.MinValue }))
            };

            var array = new ArrayVariableRowViewModel("", listOfVariableRow);

            var parameter = new Parameter
            {
                Iid = Guid.NewGuid(),
                ParameterType = new SampledFunctionParameterType(Guid.NewGuid(), null, null)
                {
                    Name = "a",
                    IndependentParameterType =
                    {
                        new IndependentParameterTypeAssignment(Guid.NewGuid(), null, null)
                        {
                            ParameterType = new DateTimeParameterType(Guid.NewGuid(), null, null)
                            {
                                Name = "Timestamp"
                            }
                        }
                    },
                    DependentParameterType =
                    {
                        new DependentParameterTypeAssignment(Guid.NewGuid(), null, null)
                        {
                            ParameterType = new SimpleQuantityKind(Guid.NewGuid(), null, null)
                            {
                                Name = "Value"
                            }
                        }
                    }
                }
            };

            parameter.ValueSet.Add(new ParameterValueSet
            {
                Computed = new ValueArray<string>(new[] { "20", "21", "20", "21" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED
            });
            var elementDefinition = new ElementDefinition(Guid.NewGuid(), null, null)
            {
                Owner = this.domain,
                ShortName = "Element"
            };
            elementDefinition.Parameter.Add(parameter);

            var mappedElementDefinitionRowViewModel = new MappedElementDefinitionRowViewModel();
            this.viewModel.SelectedMappedElement = mappedElementDefinitionRowViewModel;
            this.viewModel.SelectedMappedElement.SelectedParameter = parameter;
            this.viewModel.SelectedAvailableVariables = array;
            Assert.DoesNotThrow(() => this.viewModel.AreVariableTypesAreCompatible());
        }

        [Test]
        public void VerifyVerifyVariableTypesAreCompatible()
        {
            this.viewModel.SelectedMappedElement = null;
            Assert.DoesNotThrow(() => this.viewModel.AreVariableTypesAreCompatible());

            var mappedElementDefinitionRowViewModel = new MappedElementDefinitionRowViewModel();
            this.viewModel.SelectedMappedElement = mappedElementDefinitionRowViewModel;
            Assert.DoesNotThrow(() => this.viewModel.AreVariableTypesAreCompatible());

            this.viewModel.SelectedAvailableVariables = (VariableRowViewModel) this.viewModel.AvailableVariables.First();
            Assert.DoesNotThrow(() => this.viewModel.AreVariableTypesAreCompatible());

            this.viewModel.SelectedMappedElement.SelectedParameter = this.parameter1;
            this.viewModel.SelectedAvailableVariables = (VariableRowViewModel) this.viewModel.AvailableVariables.First();
            Assert.DoesNotThrow(() => this.viewModel.AreVariableTypesAreCompatible());

            this.parameter1.Scale = this.measurementScale;
            this.viewModel.SelectedMappedElement.SelectedParameter = this.parameter1;
            this.viewModel.SelectedAvailableVariables = (VariableRowViewModel) this.viewModel.AvailableVariables.First();
            Assert.DoesNotThrow(() => this.viewModel.AreVariableTypesAreCompatible());

            this.statusBar.Verify(x => x.Append(It.IsAny<string>(), StatusBarMessageSeverity.Error), Times.Exactly(1));
        }

        [Test]
        public void VerifyVerifyVariableTypesAsArrayAreCompatible()
        {
            var listOfVariableRow = new List<VariableRowViewModel>
            {
                new VariableRowViewModel((
                    new ReferenceDescription
                    {
                        DisplayName = new LocalizedText(string.Empty, "Mos.a"),
                        NodeId = new NodeId(Guid.NewGuid())
                    },
                    new DataValue { Value = 6, ServerTimestamp = DateTime.MinValue })),
                new VariableRowViewModel((
                    new ReferenceDescription
                    {
                        DisplayName = new LocalizedText(string.Empty, "Mos.a"),
                        NodeId = new NodeId(Guid.NewGuid())
                    },
                    new DataValue { Value = 5, ServerTimestamp = DateTime.MinValue }))
            };

            var array = new ArrayVariableRowViewModel("", listOfVariableRow);

            var parameter = new Parameter
            {
                Iid = Guid.NewGuid(),
                ParameterType = new SampledFunctionParameterType(Guid.NewGuid(), null, null)
                {
                    Name = "a",
                    IndependentParameterType =
                    {
                        new IndependentParameterTypeAssignment(Guid.NewGuid(), null, null)
                        {
                            ParameterType = new DateTimeParameterType(Guid.NewGuid(), null, null)
                            {
                                Name = "Timestamp"
                            }
                        }
                    },
                    DependentParameterType =
                    {
                        new DependentParameterTypeAssignment(Guid.NewGuid(), null, null)
                        {
                            ParameterType = new SimpleQuantityKind(Guid.NewGuid(), null, null)
                            {
                                Name = "Value"
                            }
                        }
                    }
                }
            };

            parameter.ValueSet.Add(new ParameterValueSet
            {
                Computed = new ValueArray<string>(new[] { "20", "21" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED
            });

            var mappedElementDefinitionRowViewModel = new MappedElementDefinitionRowViewModel();
            this.viewModel.SelectedMappedElement = mappedElementDefinitionRowViewModel;
            this.viewModel.SelectedMappedElement.SelectedParameter = parameter;
            this.viewModel.SelectedAvailableVariables = array;
            this.viewModel.Values = new ReactiveList<ValueSetValueRowViewModel>();
            Assert.DoesNotThrow(() => this.viewModel.AreVariableTypesAreCompatible());
        }
    }
}
