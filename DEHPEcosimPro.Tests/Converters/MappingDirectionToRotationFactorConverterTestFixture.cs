﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappingDirectionToRotationFactorConverterTestFixture.cs" company="RHEA System S.A.">
//    Copyright (c) 2015-2020 RHEA System S.A.
//
//    Author: Sam Gerené, Alex Vorobiev, Alexander van Delft, Nathanael Smiechowski, Kamil Wojnowski
//
//    This file is part of CDP4-IME Community Edition. 
//    The CDP4-IME Community Edition is the RHEA Concurrent Design Desktop Application and Excel Integration
//    compliant with ECSS-E-TM-10-25 Annex A and Annex C.
//
//    The CDP4-IME Community Edition is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Affero General Public
//    License as published by the Free Software Foundation; either
//    version 3 of the License, or any later version.
//
//    The CDP4-IME Community Edition is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//    GNU Affero General Public License for more details.
//
//    You should have received a copy of the GNU Affero General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DEHPEcosimPro.Tests.Converters
{
    using System;
    using System.Windows;

    using DEHPEcosimPro.Converters;
    using DEHPEcosimPro.Enumerators;

    using NUnit.Framework;

    [TestFixture]
    public class MappingDirectionToRotationFactorConverterTestFixture
    {
        private MappingDirectionToRotationFactorConverter converter;

        [SetUp]
        public void SetUp()
        {
            this.converter = new MappingDirectionToRotationFactorConverter();
        }

        [Test]
        public void VerifyThatConvertReturnsExpectedResult()
        {
            Assert.AreEqual(0, this.converter.Convert(null, null, null, null));

            Assert.AreEqual(180, this.converter.Convert(MappingDirection.Left, null, null, null));
            Assert.AreEqual(0, this.converter.Convert(MappingDirection.Right, null, null, null));
            Assert.AreEqual(0, this.converter.Convert(2, null, null, null));
        }

        [Test]
        public void VerifyThatConvertBackIsNotSupported()
        {
            Assert.Throws<NotSupportedException>(() => this.converter.ConvertBack(null, null, null, null));
        }
    }
}
