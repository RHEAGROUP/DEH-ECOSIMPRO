// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ParameterDifferenceRowViewModelTestFixture.cs" company="RHEA System S.A.">
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
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.Linq;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using CDP4Dal;

    using DEHPCommon.HubController.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Events;
    using DEHPEcosimPro.ViewModel;
    using DEHPEcosimPro.ViewModel.Rows;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class ParameterDifferenceRowViewModelTestFixture
    {
        private ParameterDifferenceRowViewModel viewModel;
        private Parameter OldThing;

        private Parameter NewThing;
        
        private Assembler assembler;
        private DomainOfExpertise activeDomain;
        private readonly Uri uri = new Uri("http://test.com");
        private ElementDefinition elementDefinition;
        private SimpleQuantityKind qqParamType;

        [Test]
        public void VerifyParameterDifferenceRowViewModel()
        {
            this.assembler = new Assembler(this.uri);

            this.activeDomain = new DomainOfExpertise(Guid.NewGuid(), this.assembler.Cache, this.uri) { Name = "active", ShortName = "active" };

            this.qqParamType = new SimpleQuantityKind(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                Name = "PTName",
                ShortName = "PTShortName"
            };

            this.elementDefinition = new ElementDefinition(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                Owner = this.activeDomain,
                ShortName = "Element"
            };
            
            this.OldThing = new Parameter(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                ParameterType = this.qqParamType,
                Owner = this.activeDomain,
                ValueSet =
                {
                    new ParameterValueSet()
                    {
                        Computed = new ValueArray<string>(new[] { "21" }),
                        ValueSwitch = ParameterSwitchKind.COMPUTED
                    }
                }
            };
            this.elementDefinition.Parameter.Add(this.OldThing);

            this.NewThing = new Parameter(this.OldThing.Iid, this.assembler.Cache, this.uri)
            {
                ParameterType = this.qqParamType,
                Owner = this.activeDomain,
                ValueSet =
                {
                    new ParameterValueSet()
                    {
                        Computed = new ValueArray<string>(new [] {"12"}),
                        ValueSwitch = ParameterSwitchKind.COMPUTED
                    }
                }
            };
            this.elementDefinition.Parameter.Add(this.NewThing);
            
            object Name = this.elementDefinition.Name;
            object OldValue = this.OldThing.QueryParameterBaseValueSet(null, null).ActualValue.FirstOrDefault();
            object NewValue = this.NewThing.QueryParameterBaseValueSet(null, null).ActualValue.FirstOrDefault();
            this.viewModel = new ParameterDifferenceRowViewModel(this.OldThing, this.NewThing, Name, OldValue, NewValue, "-9", "42,86%");
            var oldvalue = this.viewModel.OldValue;
            var newvalue = this.viewModel.NewValue;
            var name = this.viewModel.Name;
            var percent = this.viewModel.PercentDiff;
            var diff = this.viewModel.Difference;

            Assert.IsNotNull(this.viewModel);
            Assert.AreEqual("-9", this.viewModel.Difference);
        }


    }
}
