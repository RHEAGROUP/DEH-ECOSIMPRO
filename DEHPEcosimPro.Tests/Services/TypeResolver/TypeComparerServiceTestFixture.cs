// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TypeComparerServiceTestFixture.cs" company="RHEA System S.A.">
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
    using DEHPEcosimPro.Services.TypeResolver.Interfaces;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class TypeComparerServiceTestFixture
    {
        private Mock<IObjectTypeResolverService> variableTypeResolver;
        private Mock<IParameterTypeTypeResolverService> parameterTypeResolver;
        private TypeComparerService service;
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
        private CompoundParameterType compoundParameterType;
        private ArrayParameterType arrayParameterType;
        private SampledFunctionParameterType incompatibleSampledFunctionParameterType;
        private SampledFunctionParameterType compatibleSampledFunctionParameterType;
        private EnumerationParameterType enumerationParameterType;

        [SetUp]
        public void Setup()
        {
            this.variableTypeResolver = new Mock<IObjectTypeResolverService>();
            this.parameterTypeResolver = new Mock<IParameterTypeTypeResolverService>();
            this.service = new TypeComparerService(this.parameterTypeResolver.Object, this.variableTypeResolver.Object);
            
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

            this.compoundParameterType = new CompoundParameterType()
            {
                Component =
                {
                    new ParameterTypeComponent() { ParameterType = this.booleanParameterType},
                    new ParameterTypeComponent() { ParameterType = this.timeOfDayParameterType}
                }
            };

            this.arrayParameterType = new ArrayParameterType()
            {
                Component =
                {
                    new ParameterTypeComponent() { ParameterType = this.booleanParameterType},
                    new ParameterTypeComponent() { ParameterType = this.timeOfDayParameterType}
                }
            };

            this.compatibleSampledFunctionParameterType = new SampledFunctionParameterType()
            {
                IndependentParameterType =
                {
                    new IndependentParameterTypeAssignment() { ParameterType = this.textParameterType }
                },
                DependentParameterType =
                {
                    new DependentParameterTypeAssignment() { ParameterType = this.quantityKind }
                }
            };

            this.incompatibleSampledFunctionParameterType = new SampledFunctionParameterType()
            {
                IndependentParameterType =
                {
                    new IndependentParameterTypeAssignment() { ParameterType = this.booleanParameterType }
                },
                DependentParameterType =
                {
                    new DependentParameterTypeAssignment() { ParameterType = this.quantityKind }
                }
            };

            this.enumerationParameterType = new EnumerationParameterType()
            {
                ValueDefinition =
                {
                    new EnumerationValueDefinition() {ShortName = "Apple"},
                    new EnumerationValueDefinition() {ShortName = "Banana"}
                }
            };
        }

        [Test]
        public void VerifyAreCompatibleOnNull()
        {
            Assert.IsFalse(this.service.AreCompatible(default(TextParameterType), string.Empty));
            Assert.IsFalse(this.service.AreCompatible(this.booleanParameterType, null));
            Assert.IsFalse(this.service.AreCompatible(default(CompoundParameterType), string.Empty));
            Assert.IsFalse(this.service.AreCompatible(this.compoundParameterType, null));
            Assert.IsFalse(this.service.AreCompatible(default(ArrayParameterType), string.Empty));
            Assert.IsFalse(this.service.AreCompatible(this.arrayParameterType, null));
            Assert.IsFalse(this.service.AreCompatible(default(SampledFunctionParameterType), string.Empty));
            Assert.IsFalse(this.service.AreCompatible(this.compatibleSampledFunctionParameterType, null));
            Assert.IsFalse(this.service.AreCompatible(default(EnumerationParameterType), string.Empty));
            Assert.IsFalse(this.service.AreCompatible(this.enumerationParameterType, null));
        }

        [Test]
        public void VerifyAreCompatibleOnArrayParameterType()
        {
            this.variableTypeResolver.Setup(x => x.Resolve(It.IsAny<int>())).Returns(typeof(int));
            this.parameterTypeResolver.Setup(x => x.Resolve(It.IsAny<BooleanParameterType>())).Returns(typeof(bool));
            this.parameterTypeResolver.Setup(x => x.Resolve(It.IsAny<TimeOfDayParameterType>())).Returns(typeof(DateTime));
            this.variableTypeResolver.Setup(x => x.Resolve(It.IsAny<DateTime>())).Returns(typeof(DateTime));
            this.variableTypeResolver.Setup(x => x.Resolve(It.IsAny<bool>())).Returns(typeof(bool));
            Assert.IsFalse(this.service.AreCompatible(this.arrayParameterType, 2));
            Assert.IsTrue(this.service.AreCompatible(this.arrayParameterType, DateTime.Now));
            Assert.IsTrue(this.service.AreCompatible(this.arrayParameterType, true));
            this.arrayParameterType.Component.Clear();
            Assert.IsFalse(this.service.AreCompatible(this.arrayParameterType, DateTime.Now));
            this.arrayParameterType.Component.Add(new ParameterTypeComponent() {ParameterType = this.booleanParameterType});
            Assert.IsTrue(this.service.AreCompatible(this.arrayParameterType, true));
        }

        [Test]
        public void VerifyAreCompatibleOnCompoundParameterType()
        {
            this.variableTypeResolver.Setup(x => x.Resolve(It.IsAny<int>())).Returns(typeof(int));
            this.parameterTypeResolver.Setup(x => x.Resolve(It.IsAny<BooleanParameterType>())).Returns(typeof(bool));
            this.parameterTypeResolver.Setup(x => x.Resolve(It.IsAny<TimeOfDayParameterType>())).Returns(typeof(DateTime));
            this.variableTypeResolver.Setup(x => x.Resolve(It.IsAny<DateTime>())).Returns(typeof(DateTime));
            Assert.IsFalse(this.service.AreCompatible(this.compoundParameterType, 2));
            Assert.IsTrue(this.service.AreCompatible(this.compoundParameterType, DateTime.Now));
            this.compoundParameterType.Component.Clear();
            Assert.IsFalse(this.service.AreCompatible(this.compoundParameterType, DateTime.Now));
        }
        
        [Test]
        public void VerifyAreCompatibleOnEnumerationParameterType()
        {
            this.variableTypeResolver.Setup(x => x.Resolve(It.IsAny<int>())).Returns(typeof(int));
            this.parameterTypeResolver.Setup(x => x.Resolve(It.IsAny<BooleanParameterType>())).Returns(typeof(bool));
            this.parameterTypeResolver.Setup(x => x.Resolve(It.IsAny<TimeOfDayParameterType>())).Returns(typeof(DateTime));
            this.variableTypeResolver.Setup(x => x.Resolve(It.IsAny<string>())).Returns(typeof(string));
            Assert.IsFalse(this.service.AreCompatible(this.enumerationParameterType, 2));
            Assert.IsTrue(this.service.AreCompatible(this.enumerationParameterType, "ApPle"));
            Assert.IsFalse(this.service.AreCompatible(this.enumerationParameterType, "rEd"));
        }
        
        [Test]
        public void VerifyAreCompatibleOnSampledFunctionParameterType()
        {
            Assert.IsFalse(this.service.AreCompatible(this.incompatibleSampledFunctionParameterType, string.Empty));
            this.incompatibleSampledFunctionParameterType.IndependentParameterType.Add(new IndependentParameterTypeAssignment() {ParameterType = this.dateParameterType});
            Assert.IsFalse(this.service.AreCompatible(this.incompatibleSampledFunctionParameterType, string.Empty));
            Assert.IsTrue(this.service.AreCompatible(this.compatibleSampledFunctionParameterType, string.Empty));
            Assert.IsTrue(this.service.AreCompatible(this.compatibleSampledFunctionParameterType, 2));
        }

        [Test]
        public void VerifyAreCompatibleOnString()
        {
            this.variableTypeResolver.Setup(x => x.Resolve(It.IsAny<string>())).Returns(typeof(string));
            this.variableTypeResolver.Setup(x => x.Resolve(It.IsAny<object>())).Returns(typeof(string));
            this.parameterTypeResolver.Setup(x => x.Resolve(It.IsAny<TextParameterType>())).Returns(typeof(string));
            Assert.IsTrue(this.service.AreCompatible(this.textParameterType, "st"));
            Assert.IsTrue(this.service.AreCompatible(this.textParameterType, (object)"st"));
            Assert.IsFalse(this.service.AreCompatible(this.dateTimeParameterType, "a"));
        }

        [Test]
        public void VerifyAreCompatibleOnBoolean()
        {
            this.variableTypeResolver.Setup(x => x.Resolve(It.IsAny<bool>())).Returns(typeof(bool));
            this.variableTypeResolver.Setup(x => x.Resolve(It.IsAny<object>())).Returns(typeof(bool));
            this.parameterTypeResolver.Setup(x => x.Resolve(It.IsAny<BooleanParameterType>())).Returns(typeof(bool));
            Assert.IsTrue(this.service.AreCompatible(this.booleanParameterType, "TrUe"));
            Assert.IsTrue(this.service.AreCompatible(this.booleanParameterType, true));
            Assert.IsFalse(this.service.AreCompatible(this.dateTimeParameterType, "true"));
        }

        [Test]
        public void VerifyAreCompatibleOnDateTime()
        {
            this.variableTypeResolver.Setup(x => x.Resolve(It.IsAny<object>())).Returns(typeof(DateTime));
            this.variableTypeResolver.Setup(x => x.Resolve(It.IsAny<DateTime>())).Returns(typeof(DateTime));
            this.parameterTypeResolver.Setup(x => x.Resolve(It.IsAny<DateTimeParameterType>())).Returns(typeof(DateTime));
            this.parameterTypeResolver.Setup(x => x.Resolve(It.IsAny<DateParameterType>())).Returns(typeof(DateTime));
            this.parameterTypeResolver.Setup(x => x.Resolve(It.IsAny<TimeOfDayParameterType>())).Returns(typeof(DateTime));
            Assert.IsTrue(this.service.AreCompatible(this.dateTimeParameterType, "1984-02-07T02:22:22.158-05:00"));
            Assert.IsTrue(this.service.AreCompatible(this.dateParameterType, "1984-02-07T02:22:22.158-05:00"));
            Assert.IsTrue(this.service.AreCompatible(this.timeOfDayParameterType, "1984-02-07T02:22:22.158-05:00"));
            Assert.IsTrue(this.service.AreCompatible(this.dateTimeParameterType, DateTime.Now.AddDays(-80065)));
            this.variableTypeResolver.Setup(x => x.Resolve(It.IsAny<object>())).Returns(typeof(string));
            Assert.IsFalse(this.service.AreCompatible(this.dateParameterType, "i"));
            Assert.IsFalse(this.service.AreCompatible(this.booleanParameterType, DateTime.MaxValue));
        }

        [Test]
        public void VerifyAreCompatibleOnQuantityKind()
        {
            this.variableTypeResolver.Setup(x => x.Resolve(It.IsAny<object>())).Returns(typeof(double));
            this.variableTypeResolver.Setup(x => x.Resolve(It.IsAny<int>())).Returns(typeof(int));
            this.variableTypeResolver.Setup(x => x.Resolve(It.IsAny<uint>())).Returns(typeof(uint));
            this.variableTypeResolver.Setup(x => x.Resolve(It.IsAny<double>())).Returns(typeof(double));
            this.parameterTypeResolver.Setup(x => x.Resolve(It.IsAny<QuantityKind>())).Returns(typeof(double));
            Assert.IsTrue(this.service.AreCompatible(this.quantityKind, "1.5"));
            Assert.IsTrue(this.service.AreCompatible(this.quantityKind, .5));
            this.parameterTypeResolver.Setup(x => x.Resolve(It.IsAny<QuantityKind>())).Returns(typeof(int));
            this.variableTypeResolver.Setup(x => x.Resolve(It.IsAny<object>())).Returns(typeof(int));
            Assert.IsTrue(this.service.AreCompatible(this.quantityKind, "1"));
            Assert.IsTrue(this.service.AreCompatible(this.quantityKind, 1));
            this.parameterTypeResolver.Setup(x => x.Resolve(It.IsAny<QuantityKind>())).Returns(typeof(uint));
            this.variableTypeResolver.Setup(x => x.Resolve(It.IsAny<object>())).Returns(typeof(uint));
            Assert.IsTrue(this.service.AreCompatible(this.quantityKind, "1"));
            Assert.IsTrue(this.service.AreCompatible(this.quantityKind, 1u));

            this.variableTypeResolver.Setup(x => x.Resolve(It.IsAny<object>())).Returns(typeof(string));
            Assert.IsFalse(this.service.AreCompatible(this.quantityKind, "true"));
            Assert.IsFalse(this.service.AreCompatible(this.booleanParameterType, 1));
        }
    }
}
