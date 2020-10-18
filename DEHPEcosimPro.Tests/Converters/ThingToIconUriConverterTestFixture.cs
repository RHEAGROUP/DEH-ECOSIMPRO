// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ThingToIconUriConverterTestFixture.cs" company="RHEA System S.A.">
//    Copyright (c) 2015-2020 RHEA System S.A.
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

namespace DEHPEcosimPro.Tests.Converters
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Windows;
    using System.Windows.Media.Imaging;

    using Autofac;

    using CDP4Common;
    using CDP4Common.CommonData;
    using CDP4Common.SiteDirectoryData;

    using DEHPEcosimPro.Converters;
    using DEHPEcosimPro.Enumerators;
    using DEHPEcosimPro.Services.IconCacheService;
    using DEHPEcosimPro.Utilities;

    using Moq;

    using NUnit.Framework;

    /// <summary>
    /// suite of tests for the <see cref="ThingToIconUriConverter"/>
    /// </summary>
    [TestFixture, Apartment(ApartmentState.STA)]
    public class ThingToIconUriConverterTestFixture
    {
        /// <summary>
        /// the <see cref="ThingToIconUriConverter"/> under test
        /// </summary>
        private ThingToIconUriConverter converter;

        private IconCacheService iconCacheService;

        [SetUp]
        public void SetUp()
        {
            this.iconCacheService = new IconCacheService();

            this.converter = new ThingToIconUriConverter();
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(this.iconCacheService).As<IIconCacheService>();
            AppContainer.BuildContainer(containerBuilder);
            Application.ResourceAssembly = Assembly.GetAssembly(typeof(App));
        }

        [Test]
        public void VerifyThatConvertingNullReturnsNull()
        {
            var icon = this.converter.Convert(null, null, null, null);
            Assert.IsNull(icon);
            icon = this.converter.Convert(new ThingStatus[0], null, null, null);
            Assert.IsNull(icon);
        }

        [Test]
        public void VerifyThatConvertProvidesTheExpectedIcon()
        {
            const string naturalLanguageIcon = "pack://application:,,,/Resources/Images/Thing/naturallanguage.png";
            var naturalLanguage = new NaturalLanguage();
            var firstConverterResult = (BitmapImage)this.converter.Convert(new object[]{naturalLanguage}, null, null, null);

            Assert.AreEqual(naturalLanguageIcon, firstConverterResult.UriSource.ToString());

            var secondConverterResult = (BitmapImage)this.converter.Convert(new object[] { naturalLanguage }, null, null, null);

            Assert.AreSame(firstConverterResult, secondConverterResult);
        }

        [Test]
        public void VerifyThatConvertPersonProvidesTheExpectedIcon()
        {
            const string personGrayIcon = "pack://application:,,,/DEHPEcosimPro;component/Resources/Images/Thing/grayscalePerson_16x16.png";
            var person = new Person();
            var converterResult = (BitmapImage)this.converter.Convert(new object[] { person }, null, null, null);

            Assert.AreNotEqual(personGrayIcon, converterResult.UriSource.ToString());

            converterResult = (BitmapImage)this.converter.Convert(new object[] { person, RowStatusKind.Inactive }, null, true, null);
            Assert.AreEqual(personGrayIcon, converterResult.UriSource.ToString());
        }

        [Test]
        public void VerifyThatConvertParticipantProvidesTheExpectedIcon()
        {
            const string participantGrayIcon = "pack://application:,,,/DEHPEcosimPro;component/Resources/Images/Thing/grayscaleParticipant_16x16.png";
            var participant = new Participant();
            var converterResult = (BitmapImage)this.converter.Convert(new object[] { participant }, null, null, null);

            Assert.AreNotEqual(participantGrayIcon, converterResult.UriSource.ToString());

            converterResult = (BitmapImage)this.converter.Convert(new object[] { participant, RowStatusKind.Inactive }, null, null, null);
            Assert.AreEqual(participantGrayIcon, converterResult.UriSource.ToString());
        }

        [Test]
        public void VerifyThatConvertIterationSetupProvidesTheExpectedIcon()
        {
            const string iterationSetupGrayIcon = "pack://application:,,,/DEHPEcosimPro;component/Resources/Images/Thing/grayscaleIterationSetup_16x16.png";
            var iterationSetup = new IterationSetup();
            var converterResult = (BitmapImage)this.converter.Convert(new object[] { iterationSetup }, null, null, null);

            Assert.AreNotEqual(iterationSetupGrayIcon, converterResult.UriSource.ToString());

            converterResult = (BitmapImage)this.converter.Convert(new object[] { iterationSetup, RowStatusKind.Inactive }, null, null, null);
            Assert.AreEqual(iterationSetupGrayIcon, converterResult.UriSource.ToString());
        }

        /// <summary>
        /// coverage test
        /// </summary>
        [Test]
        public void VerifyThatIconIsReturnedForAllClassKind()
        {
            var assembly = Assembly.GetAssembly(typeof(Thing));
            var values = Enum.GetValues(typeof(ClassKind)).Cast<ClassKind>().Select(x => x.ToString()).ToList();
            foreach (var type in assembly.GetTypes())
            {
                if (!values.Contains(type.Name) || type.FullName.Contains("DTO"))
                {
                    continue;
                }

                Thing thing;
                if (type.Name == "NotThing")
                {
                    thing = new NotThing("a");
                }
                else
                {
                    if (type.IsAbstract)
                    {
                        continue;
                    }

                    thing = (Thing)Activator.CreateInstance(type, Guid.NewGuid(), null, null);
                }

                var converterResult = this.converter.Convert(new object[] { thing }, null, null, null);
                Assert.IsNotNull(converterResult);
            }
        }

        /// <summary>
        /// coverage test
        /// </summary>
        [Test]
        public void VerifyThatIconIsReturnedForAllClassKindGrayScale()
        {
            var assembly = Assembly.GetAssembly(typeof(Thing));
            var values = Enum.GetValues(typeof(ClassKind)).Cast<ClassKind>().Select(x => x.ToString()).ToList();
            
            foreach (var type in assembly.GetTypes())
            {
                if (!values.Contains(type.Name) || type.FullName.Contains("DTO"))
                {
                    continue;
                }

                Thing thing;

                if (type.Name == "NotThing")
                {
                    thing = new NotThing("a");
                }
                else
                {
                    if (type.IsAbstract)
                    {
                        continue;
                    }

                    thing = (Thing)Activator.CreateInstance(type, Guid.NewGuid(), null, null);
                }

                var converterResult = this.converter.Convert(new object[] { thing, RowStatusKind.Inactive }, null, null, null);
                Assert.IsNotNull(converterResult);
            }
        }

        [Test]
        public void VerifyThatLargeIconIsReturnedForAllClassKind()
        {
            var values = Enum.GetValues(typeof(ClassKind)).Cast<ClassKind>();

            foreach (var classKind in values)
            {
                var converterResult = this.converter.GetImage(classKind, false);
                Assert.IsNotNull(converterResult);
            }
        }

        [Test]
        public void VerifyThatConvertBackThrowsException()
        {
            Assert.Throws<NotSupportedException>(() => this.converter.ConvertBack(null, null, null, null));
        }

        [Test]
        public void VerifyThatThingWithErrorReturnsIconWithErrorOverlay()
        {
            var constant = new Constant();
            constant.ValidatePoco();
            var converterResult = (System.Windows.Interop.InteropBitmap)this.converter.Convert(new object[] { constant }, null, null, null);

            Assert.NotNull(converterResult);
        }

        [Test]
        public void VerifyThingsStatusKind()
        {
            var service = new Mock<IIconCacheService>();
            service.Setup(x => x.QueryOverlayBitmapSource(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<OverlayPositionKind>())).Returns(new BitmapImage());
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(service.Object).As<IIconCacheService>().SingleInstance();
            AppContainer.Container = containerBuilder.Build();
            var thing = new BooleanParameterType();
            var thingStatus = new ThingStatus(thing) { IsLocked = true, IsHidden = true, IsFavorite = true };
            var result = this.converter.Convert(new object[]{thingStatus}, null, null, null);
            Assert.IsNotNull(result);
            thingStatus.IsLocked = false;
            result = this.converter.Convert(new object[]{thingStatus}, null, null, null);
            Assert.IsNotNull(result);
            thingStatus.IsHidden = false;
            result = this.converter.Convert(new object[]{thingStatus}, null, null, null);
            Assert.IsNotNull(result);
            service.Verify(x => x.QueryOverlayBitmapSource(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<OverlayPositionKind>()), Times.Exactly(3));
        }
    }
}
