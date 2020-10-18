// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IconCacheServiceTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Tests.Services
{
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Windows;
    using System.Windows.Media.Imaging;

    using DEHPEcosimPro.Services.IconCacheService;

    using NUnit.Framework;

    /// <summary>
    /// Suite of tests for the <see cref="IconCacheService"/> class
    /// </summary>
    [TestFixture, Apartment(ApartmentState.STA)]
    public class IconCacheServiceTestFixture
    {
        private IconCacheService iconCacheService;
        
        [SetUp]
        public void SetUp()
        {
            this.iconCacheService = new IconCacheService();
            Application.ResourceAssembly = Assembly.GetAssembly(typeof(App));
        }

        [Test]
        public void VerifyThatSameIconIsReturned()
        {
            const string naturalLanguageIcon = "pack://application:,,,/DEHPEcosimPro;component/Resources/Images/Thing/naturallanguage.png";
            var imageUri = new Uri(naturalLanguageIcon);

            var firstBitmapImage = this.iconCacheService.QueryBitmapImage(imageUri);

            Assert.IsInstanceOf<BitmapImage>(firstBitmapImage);

            var secondBitmapImage = this.iconCacheService.QueryBitmapImage(imageUri);

            Assert.IsInstanceOf<BitmapImage>(secondBitmapImage);

            Assert.AreSame(firstBitmapImage , secondBitmapImage);
        }

        [Test]
        public void VerityThatSameBitmapSourceIsReturned()
        {
            const string naturalLanguageIcon = "pack://application:,,,/DEHPEcosimPro;component/Resources/Images/Thing/naturallanguage.png";
            var imageUri = new Uri(naturalLanguageIcon);

            var firstBitmapSource = this.iconCacheService.QueryErrorOverlayBitmapSource(imageUri);

            Assert.IsInstanceOf<BitmapSource>(firstBitmapSource);

            var secondBitmapSource = this.iconCacheService.QueryErrorOverlayBitmapSource(imageUri);

            Assert.IsInstanceOf<BitmapSource>(secondBitmapSource);

            Assert.AreSame(firstBitmapSource, secondBitmapSource);
        }
        
        [Test]
        public void VerityThatOverlayIconGetCached()
        {
            const string naturalLanguageIcon = "pack://application:,,,/DEHPEcosimPro;component/Resources/Images/Thing/naturallanguage.png";
            const string relationshipOverlayUri = "pack://application:,,,/DEHPEcosimPro;component/Resources/Images/linkgreen_16x16.png";
            
            var imageUri = new Uri(naturalLanguageIcon);
            var overlayUri = new Uri(relationshipOverlayUri);

            var firstBitmapSource = this.iconCacheService.QueryOverlayBitmapSource(imageUri, overlayUri);

            Assert.IsInstanceOf<BitmapSource>(firstBitmapSource);

            var secondBitmapSource = this.iconCacheService.QueryOverlayBitmapSource(imageUri, overlayUri);
            Assert.IsInstanceOf<BitmapSource>(secondBitmapSource);

            Assert.AreSame(firstBitmapSource, secondBitmapSource);
        }
    }
}
