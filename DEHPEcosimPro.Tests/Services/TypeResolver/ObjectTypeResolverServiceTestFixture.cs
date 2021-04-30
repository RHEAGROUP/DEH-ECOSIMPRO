// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectTypeResolverServiceTestFixture.cs" company="RHEA System S.A.">
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
    using DEHPEcosimPro.Services.TypeResolver;

    using NUnit.Framework;

    [TestFixture]
    public class ObjectTypeResolverServiceTestFixture
    {
        private ObjectTypeResolverService service;

        [SetUp]
        public void Setup()
        {
            this.service = new ObjectTypeResolverService();
        }

        [Test]
        public void VerifyResolveFromString()
        {
            Assert.AreEqual(2, this.service.Resolve("2"));
            Assert.AreEqual(-2, this.service.Resolve("-2"));
            Assert.AreEqual("-2u", this.service.Resolve("-2u"));
            Assert.AreEqual(.5, this.service.Resolve("0.5"));
            Assert.AreEqual(true, this.service.Resolve("true"));
        }

        [Test]
        public void VerifyResolveFromObject()
        {
            Assert.AreEqual(typeof(uint), this.service.Resolve(2u));
            Assert.AreEqual(typeof(int), this.service.Resolve(-2));
            Assert.AreEqual(typeof(long), this.service.Resolve(-2u));
            Assert.AreEqual(typeof(double), this.service.Resolve(.5));
            Assert.AreEqual(typeof(bool), this.service.Resolve(true));
            Assert.AreEqual("str", this.service.Resolve("str"));
        }

        [Test]
        public void VerifyIsOfType()
        {
            Assert.IsTrue(this.service.Is<int>("-2"));
            Assert.IsTrue(this.service.Is<int>(-2));
            Assert.IsFalse(this.service.Is<int>("true"));
            Assert.IsFalse(this.service.Is<int>(true));
        }
    }
}
