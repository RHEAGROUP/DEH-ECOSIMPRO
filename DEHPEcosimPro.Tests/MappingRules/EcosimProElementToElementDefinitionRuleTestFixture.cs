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

    using CDP4Dal;

    using DEHPCommon;
    using DEHPCommon.HubController.Interfaces;

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

        [SetUp]
        public void Setup()
        {
            this.uri = new Uri("https://test.test");
            this.assembler = new Assembler(this.uri);
            this.domain = new DomainOfExpertise(Guid.NewGuid(), this.assembler.Cache, this.uri);
            this.session = new Mock<ISession>();
            this.session.Setup(x => x.Assembler).Returns(this.assembler);
            this.session.Setup(x => x.DataSourceUri).Returns(this.uri.AbsoluteUri);
            this.iteration = new Iteration(Guid.NewGuid(), this.assembler.Cache, this.uri);

            this.hubController = new Mock<IHubController>();
            this.hubController.Setup(x => x.CurrentDomainOfExpertise).Returns(this.domain);
            this.hubController.Setup(x => x.Session).Returns(this.session.Object);
            this.hubController.Setup(x => x.OpenIteration).Returns(this.iteration);

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(this.hubController.Object).As<IHubController>();
            AppContainer.Container = containerBuilder.Build();

            this.rule = new EcosimProElementToElementDefinitionRule();

            this.variables = new List<VariableRowViewModel>()
            {
                new VariableRowViewModel((
                    new ReferenceDescription() {DisplayName = new LocalizedText(string.Empty, "Mos.a")}, 
                    new DataValue() {Value = 5, ServerTimestamp = DateTime.MinValue}))
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
                SelectedValues = { timeTaggedValueRowViewModel }
            });

            var (elements, maps) = this.rule.Transform(this.variables);
            Assert.AreEqual("0.2", elements.Last().Parameter.First().ValueSet.First().ActualValue.First());
            Assert.IsNotEmpty(maps);
        }
        
        [Test]
        public void VerifyMapToElementUsageParameter()
        {
            var timeTaggedValueRowViewModel = new TimeTaggedValueRowViewModel(.2, DateTime.MinValue);

            var parameter = new Parameter()
            {
                ParameterType = new CompoundParameterType()
                {
                    ShortName = "TimeTaggedValue",
                    Name = "TimeTaggedValue",
                    Symbol = "ttv",
                    Component =
                    {
                        new ParameterTypeComponent() { ParameterType = new DateTimeParameterType()},
                        new ParameterTypeComponent() { ParameterType = new SimpleQuantityKind()},
                    }
                }
            };

            var elementDefinition = new ElementDefinition() { ContainedElement = { new ElementUsage() { ParameterOverride = { new ParameterOverride() { Parameter = parameter }}}}};

            this.variables.Add(new VariableRowViewModel((
                new ReferenceDescription() { DisplayName = new LocalizedText(string.Empty, "Cap.a") },
                new DataValue() { Value = 5, ServerTimestamp = DateTime.MinValue }))
            {
                Values = { timeTaggedValueRowViewModel },
                SelectedValues = { timeTaggedValueRowViewModel },
                SelectedElementDefinition = elementDefinition,
                SelectedElementUsages = { elementDefinition.ContainedElement.First() }
            });

            var (elements, maps) = this.rule.Transform(this.variables);
            Assert.AreEqual("0.2", elements.Last().ContainedElement.First().ParameterOverride.First().ValueSet.First().ActualValue.First());
            Assert.IsNotEmpty(maps);
        }
    }
}
