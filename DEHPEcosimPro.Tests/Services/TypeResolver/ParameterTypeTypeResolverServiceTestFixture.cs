// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ParameterTypeTypeResolverServiceTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Tests.Services.TypeResolver
{
    using System;

    using CDP4Common.SiteDirectoryData;

    using DEHPEcosimPro.Services.TypeResolver;

    using NUnit.Framework;

    [TestFixture]
    public class ParameterTypeTypeResolverServiceTestFixture
    {
        private ParameterTypeTypeResolverService service;
        private BooleanParameterType booleanParameterType;
        private DateTimeParameterType dateTimeParameterType;
        private DateParameterType dateParameterType;
        private TimeOfDayParameterType timeOfDayParameterType;
        private TextParameterType textParameterType;
        private RatioScale intScale;
        private RatioScale realScale;
        private RatioScale naturalScale;
        private RatioScale rationalScale;
        private SimpleQuantityKind quantityKind;

        [SetUp]
        public void Setup()
        {
            this.service = new ParameterTypeTypeResolverService();

            this.booleanParameterType = new BooleanParameterType();
            this.dateTimeParameterType = new DateTimeParameterType();
            this.dateParameterType = new DateParameterType();
            this.timeOfDayParameterType = new TimeOfDayParameterType();
            this.textParameterType = new TextParameterType();

            this.intScale = new RatioScale() { NumberSet = NumberSetKind.INTEGER_NUMBER_SET };
            this.realScale = new RatioScale() { NumberSet = NumberSetKind.REAL_NUMBER_SET };
            this.naturalScale = new RatioScale() { NumberSet = NumberSetKind.NATURAL_NUMBER_SET };
            this.rationalScale = new RatioScale() { NumberSet = NumberSetKind.RATIONAL_NUMBER_SET };

            this.quantityKind = new SimpleQuantityKind() { PossibleScale = { this.intScale, this.realScale, this.naturalScale, this.rationalScale }, DefaultScale = this.intScale };
        }

        [Test]
        public void VerifyResolve()
        {
            this.quantityKind.DefaultScale = this.intScale;
            Assert.AreEqual(typeof(int), this.service.Resolve(this.quantityKind));
            this.quantityKind.DefaultScale = this.realScale;
            Assert.AreEqual(typeof(double), this.service.Resolve(this.quantityKind));
            this.quantityKind.DefaultScale = this.naturalScale;
            Assert.AreEqual(typeof(uint), this.service.Resolve(this.quantityKind));
            this.quantityKind.DefaultScale = this.realScale;
            Assert.AreEqual(typeof(double), this.service.Resolve(this.quantityKind));
            this.quantityKind.DefaultScale = null;
            Assert.AreEqual(typeof(double), this.service.Resolve(this.quantityKind));

            Assert.AreEqual(typeof(DateTime), this.service.Resolve(this.dateParameterType));
            Assert.AreEqual(typeof(DateTime), this.service.Resolve(this.dateTimeParameterType));
            Assert.AreEqual(typeof(DateTime), this.service.Resolve(this.timeOfDayParameterType));

            Assert.AreEqual(typeof(bool), this.service.Resolve(this.booleanParameterType));

            Assert.AreEqual(typeof(string), this.service.Resolve(this.textParameterType));

            Assert.Throws<ArgumentOutOfRangeException>(() => this.service.Resolve(new CompoundParameterType()));
        }
    }
}
