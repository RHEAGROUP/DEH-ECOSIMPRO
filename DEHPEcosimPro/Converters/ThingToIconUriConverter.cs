// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ThingToIconUriConverter.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Converters
{
    using System;
    using System.Drawing;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Data;
    using System.Windows.Media.Imaging;

    using Autofac;

    using CDP4Common.CommonData;

    using DEHPEcosimPro.Enumerators;
    using DEHPEcosimPro.Services.IconCacheService;
    using DEHPEcosimPro.Utilities;

    using DevExpress.Xpf.Core;

    /// <summary>
    /// The purpose of the <see cref="ThingToIconUriConverter"/> is to return an icon based on the 
    /// provided <see cref="Thing"/>. The icon is returned as a string
    /// </summary>
    public class ThingToIconUriConverter : IMultiValueConverter
    {
        /// <summary>
        /// The <see cref="IIconCacheService"/>
        /// </summary>
        private IIconCacheService iconCacheService;

        /// <summary>
        /// Returns an GetImage (icon) based on the <see cref="Thing"/> that is provided
        /// </summary>
        /// <param name="values">An instance of <see cref="Thing"/> for which an Icon needs to be returned</param>
        /// <param name="targetType">The parameter is not used.</param>
        /// <param name="parameter">The parameter is not used.</param>
        /// <param name="culture">The parameter is not used.</param>
        /// <returns>
        /// A <see cref="Uri"/> to an GetImage
        /// </returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null)
            {
                return null;
            }

            var rowStatus = values.OfType<RowStatusKind>().SingleOrDefault();
            var thing = values.OfType<Thing>().SingleOrDefault();
            var thingStatus = values.OfType<ThingStatus>().SingleOrDefault();

            if (thing == null && thingStatus == null)
            {
                return null;
            }

            var classKind = thing?.ClassKind ?? thingStatus.Thing.ClassKind;

            var uri = rowStatus switch
            {
                RowStatusKind.Active => new Uri(IconUtilities.ImageUri(classKind).ToString()),
                RowStatusKind.Inactive => new Uri(this.GrayScaleImageUri(classKind).ToString()),
                _ => new Uri(IconUtilities.ImageUri(classKind).ToString())
            };

            if (thing != null)
            {
                return thing.ValidationErrors.Any() 
                    ? this.QueryIIconCacheService().QueryErrorOverlayBitmapSource(uri) 
                    : this.QueryIIconCacheService().QueryBitmapImage(uri);
            }

            if (thingStatus.HasError)
            {
                return this.QueryIIconCacheService().QueryErrorOverlayBitmapSource(uri);
            }

            if (thingStatus.IsLocked)
            {
                return this.QueryIIconCacheService().QueryOverlayBitmapSource(uri, IconUtilities.LockedOverlayUri);
            }

            if (thingStatus.IsHidden)
            {
                return this.QueryIIconCacheService().QueryOverlayBitmapSource(uri, IconUtilities.HiddenOverlayUri);
            }

            if (thingStatus.HasRelationship)
            {
                return this.QueryIIconCacheService().QueryOverlayBitmapSource(uri, IconUtilities.RelationshipOverlayUri, OverlayPositionKind.TopRight);
            }

            return thingStatus.IsFavorite 
                ? this.QueryIIconCacheService().QueryOverlayBitmapSource(uri, IconUtilities.FavoriteOverlayUri, OverlayPositionKind.BottomLeft) 
                : this.QueryIIconCacheService().QueryBitmapImage(uri);
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="value">The parameter is not used.</param>
        /// <param name="targetTypes">The parameter is not used.</param>
        /// <param name="parameter">The parameter is not used.</param>
        /// <param name="culture">The parameter is not used.</param>
        /// <returns>a <see cref="NotSupportedException"/> is thrown</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns an instance of <see cref="Image"/> based on the provided <see cref="ClassKind"/>
        /// </summary>
        /// <param name="classKind">
        /// the subject <see cref="ClassKind"/>
        /// </param>
        /// <param name="getsmallicon">
        /// Indicates whether a small or large icon should be returned.
        /// </param>
        /// <returns>
        /// An of <see cref="Image"/> that corresponds to the subject <see cref="ClassKind"/>
        /// </returns>
        public Image GetImage(ClassKind classKind, bool getsmallicon = true)
        {
            if (IconUtilities.ImageUri(classKind, getsmallicon) is string convertedstring)
            {
                var imageUri = new Uri(convertedstring);
                var image = new BitmapImage(imageUri);
                var bitmap = IconUtilities.BitmapImage2Bitmap(image);
                return bitmap;
            }

            if (IconUtilities.ImageUri(classKind, getsmallicon) is DXImageExtension convertedDXImageExtension)
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = convertedDXImageExtension.Image.MakeUri();
                image.EndInit();

                var bitmap = IconUtilities.BitmapImage2Bitmap(image);
                return bitmap;
            }

            return null;
        }

        /// <summary>
        /// Returns the <see cref="Uri"/> of the resource in grayscale
        /// </summary>
        /// <param name="classKind"> The <see cref="ClassKind"/> for which in icon needs to be provided </param>
        /// <param name="getsmallicon"> Indicates whether a small or large icon should be returned.</param>
        /// <returns> A <see cref="Uri"/> that points to a resource </returns>
        private object GrayScaleImageUri(ClassKind classKind, bool getsmallicon = true)
        {
            var packUri = $"{IconUtilities.RootResourcesPath}DEHPEcosimPro;component/Resources/Images/Thing/";
            var imageSize = getsmallicon ? "_16x16" : "_32x32";
            const string extension = ".png";

            var imagename = classKind switch
            {
                ClassKind.Participant => "grayscaleParticipant",
                ClassKind.Person => "grayscalePerson",
                ClassKind.IterationSetup => "grayscaleIterationSetup",
                _ => "grayscaleIterationSetup"
            };

            return $"{packUri}{imagename}{imageSize}{extension}";
        }

        /// <summary>
        /// Queries the instance of the <see cref="IIconCacheService"/> that is to be used
        /// </summary>
        /// <returns>
        /// An instance of <see cref="IIconCacheService"/>
        /// </returns>
        private IIconCacheService QueryIIconCacheService()
        {
            return this.iconCacheService ??= AppContainer.Container.Resolve<IIconCacheService>();
        }
    }
}
