// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ParameterDifferenceViewModelTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Tests.ViewModel.Rows
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Castle.Components.DictionaryAdapter;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using CDP4Dal;

    using DEHPCommon.HubController.Interfaces;
    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel.Rows;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class ParameterDifferenceViewModelTestFixture
    {
        private Parameter OldThing;
        private Parameter NewThing;

        private Mock<IHubController> hubController;
        private Mock<IDstController> dstController;
        private ParameterDifferenceViewModel viewModel;
        private Assembler assembler;
        private DomainOfExpertise activeDomain;
        private readonly Uri uri = new Uri("http://test.com");
        private ElementDefinition elementDefinition;
        private SimpleQuantityKind qqParamType;
        private Guid Iid;

        [SetUp]
        public void SetUp()
        {
            this.hubController = new Mock<IHubController>();
            this.dstController = new Mock<IDstController>();
        }


        [Test]
        public void VerifyParameterDifferenceViewModel()
        {
            this.assembler = new Assembler(this.uri);

            this.Iid = Guid.NewGuid();

            this.activeDomain = new DomainOfExpertise(Guid.NewGuid(), this.assembler.Cache, this.uri) { Name = "active", ShortName = "active" };
            this.elementDefinition = new ElementDefinition(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                Owner = this.activeDomain,
                ShortName = "Element"
            };

            this.qqParamType = new SimpleQuantityKind(Guid.NewGuid(), this.assembler.Cache, this.uri);
            
            this.OldThing = new Parameter(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                Owner = this.activeDomain,
                ValueSet =
                {
                    new ParameterValueSet()
                    {
                        Computed = new ValueArray<string>(new[] { "20" }),
                        ValueSwitch = ParameterSwitchKind.COMPUTED
                    }
                }
            };
            this.elementDefinition.Parameter.Add(this.OldThing);

            this.NewThing = new Parameter(this.Iid, this.assembler.Cache, this.uri)
            {
                ParameterType = this.qqParamType,
                Owner = this.activeDomain,
                ValueSet =
                {
                    new ParameterValueSet()
                    {
                        Computed = new ValueArray<string>(new[] { "12" }),
                        ValueSwitch = ParameterSwitchKind.COMPUTED
                    }
                }
            };

            this.elementDefinition.Parameter.Add(this.NewThing);

            this.viewModel = new ParameterDifferenceViewModel(this.OldThing, this.NewThing, this.dstController.Object);

            var listOfParameters = this.viewModel.ListOfParameters;

            Assert.IsNotNull(listOfParameters);
            Assert.AreEqual("-8", listOfParameters.FirstOrDefault().Difference);
        }

        [Test]
        public void VerifyParameterDifferenceViewModelWithStringValue()
        {
            this.assembler = new Assembler(this.uri);

            this.Iid = Guid.NewGuid();

            this.activeDomain = new DomainOfExpertise(Guid.NewGuid(), this.assembler.Cache, this.uri) ;
            this.elementDefinition = new ElementDefinition(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                Owner = this.activeDomain
            };

            this.qqParamType = new SimpleQuantityKind(Guid.NewGuid(), this.assembler.Cache, this.uri);
           
            this.OldThing = new Parameter(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                ParameterType = this.qqParamType,
                Owner = this.activeDomain,
                ValueSet =
                {
                    new ParameterValueSet()
                    {
                        Computed = new ValueArray<string>(new[] { "12", "13", "14" }),
                        ValueSwitch = ParameterSwitchKind.COMPUTED
                    }
                }
            };
            this.elementDefinition.Parameter.Add(this.OldThing);

            this.NewThing = new Parameter(this.Iid, this.assembler.Cache, this.uri)
            {
                ParameterType = this.qqParamType,
                Owner = this.activeDomain,
                ValueSet =
                {
                    new ParameterValueSet()
                    {
                        Computed = new ValueArray<string>(new[] { "20", "21", "22" }),
                        ValueSwitch = ParameterSwitchKind.COMPUTED
                    }
                }
            };

            this.elementDefinition.Parameter.Add(this.NewThing);

            this.viewModel = new ParameterDifferenceViewModel(this.OldThing, this.NewThing, this.dstController.Object);

            var listOfParameters = this.viewModel.ListOfParameters;
            Assert.IsNotNull(listOfParameters);
        }

        [Test]
        public void VerifyThrowEception()
        {
            this.viewModel.ListOfParameters = new List<ParameterDifferenceRowViewModel>();
            this.viewModel.ListOfParameters.Clear();

            this.assembler = new Assembler(this.uri);

            this.Iid = Guid.NewGuid();

            this.activeDomain = new DomainOfExpertise(Guid.NewGuid(), this.assembler.Cache, this.uri) { Name = "active", ShortName = "active" };
            this.elementDefinition = new ElementDefinition(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                Owner = this.activeDomain,
                ShortName = "Element"
            };

            this.qqParamType = new SimpleQuantityKind(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                Name = "PTName",
                ShortName = "PTShortName"
            };
            
            this.OldThing = new Parameter(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                ParameterType = this.qqParamType,
                Owner = this.activeDomain,
                ValueSet =
                {
                    new ParameterValueSet()
                    {
                        Computed = new ValueArray<string>(new[] { "20" }),
                        ValueSwitch = ParameterSwitchKind.COMPUTED
                    }
                }
            };
            this.elementDefinition.Parameter.Add(this.OldThing);
            
            this.NewThing = new Parameter(this.Iid, this.assembler.Cache, this.uri);

            this.elementDefinition.Parameter.Add(this.NewThing);
            
            Assert.Throws<NullReferenceException>(() => new ParameterDifferenceViewModel(this.OldThing, this.NewThing, this.dstController.Object));
        }

    }
}
