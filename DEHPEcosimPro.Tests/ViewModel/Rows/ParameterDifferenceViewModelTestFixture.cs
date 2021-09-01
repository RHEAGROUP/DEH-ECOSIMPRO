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
    using System.Linq;

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


            #region Both

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
            #endregion
            #region Parameter1

            var valueset = new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "20" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED
            };

            this.OldThing = new Parameter(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                ParameterType = this.qqParamType,
                Owner = this.activeDomain,
                ValueSet =
                {
                    valueset
                }
            };
            this.elementDefinition.Parameter.Add(this.OldThing);

            #endregion

            #region Parameter2

            var valueset2 = new ParameterValueSet()
            {
                Computed = new ValueArray<string>(new[] { "12" }),
                ValueSwitch = ParameterSwitchKind.COMPUTED
            };
            this.NewThing = new Parameter(this.Iid, this.assembler.Cache, this.uri)
            {
                ParameterType = this.qqParamType,
                Owner = this.activeDomain,
                ValueSet =
                {
                    valueset2
                }
            };

            this.elementDefinition.Parameter.Add(this.NewThing);

            #endregion
            this.viewModel = new ParameterDifferenceViewModel(OldThing, NewThing, this.dstController.Object);
        }


        [Test]
        public void VerifyParameterDifferenceViewModel()
        {
            var listOfParameters = this.viewModel.ListOfParameters;
            Assert.IsNotNull(listOfParameters);
            Assert.AreEqual("-8", listOfParameters.FirstOrDefault().Difference);
        }
    }
}
