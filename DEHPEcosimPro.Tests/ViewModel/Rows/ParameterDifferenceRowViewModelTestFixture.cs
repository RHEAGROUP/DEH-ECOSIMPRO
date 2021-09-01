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
        private Guid Iid;

        [Test]
        public void VerifyParameterDifferenceRowViewModel()
        {

            #region Both
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
            #endregion

            #region OldThing

            var valueset =
                new ParameterValueSet()
                {
                    Computed = new ValueArray<string>(new[] { "21" }),
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

            #region NewThing

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
            #endregion

            #region ViewModel
            object Name = this.elementDefinition.Name;
            object OldValue = this.OldThing.QueryParameterBaseValueSet(null, null).ActualValue.FirstOrDefault();
            object NewValue = this.NewThing.QueryParameterBaseValueSet(null, null).ActualValue.FirstOrDefault();
            this.CalculateDiff( OldValue, NewValue, out string Difference, out string PercentDiff);
            this.viewModel = new ParameterDifferenceRowViewModel(this.OldThing, this.NewThing, Name, OldValue, NewValue, Difference, PercentDiff);
            #endregion

            Assert.IsNotNull(this.viewModel);
            Assert.AreEqual("-9", this.viewModel.Difference);
        }


        /// <summary>
        /// Calculate the difference between the old and new value, if possible
        /// </summary>
        /// <param name="OldValue"></param>
        /// <param name="NewValue"></param>
        /// <param name="Difference">a number, positive or negative (with + or - sign)</param>
        /// <param name="PercentDiff">a number in percent, positive or negative (with + or - sign)</param>
        private void CalculateDiff(object OldValue, object NewValue, out string Difference, out string PercentDiff)
        {
            Difference = "0";
            PercentDiff = "0";

            NumberStyles style = NumberStyles.Number | NumberStyles.AllowDecimalPoint;
            CultureInfo culture = CultureInfo.InvariantCulture;

            var isOldValueDecimal = decimal.TryParse(OldValue.ToString(), style, culture, out decimal decimalOldValue);
            var isNewValueDecimal = decimal.TryParse(NewValue.ToString(), style, culture, out decimal decimalNewValue);

            if (isOldValueDecimal && isNewValueDecimal)
            {
                var diff = decimalNewValue - decimalOldValue;
                var sign = Math.Sign(diff);
                var abs = Math.Abs(diff);
                var percentChange = Math.Round(Math.Abs(diff / Math.Abs(decimalOldValue) * 100), 2);

                if (sign > 0)
                {
                    Difference = $"+{abs}";
                    PercentDiff = $"+{percentChange}%";
                }
                else if (sign < 0)
                {
                    Difference = $"-{abs}";
                    PercentDiff = $"-{percentChange}%";
                }
            }
            else
            {
                Difference = $"/";
                PercentDiff = $"/";
            }
        }
    }
}
