// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IIconCacheService.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Services.IconCacheService
{
    using System;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// The purpose of the <see cref="IIconCacheService"/> is to provide a caching mechanism
    /// for icons that ar frequently queried
    /// </summary>
    public interface IIconCacheService
    {
        /// <summary>
        /// Queries the <see cref="BitmapImage"/> from the cache if the cache contains it else the <see cref="BitmapImage"/>
        /// is created, added to the cache and then returned.
        /// </summary>
        /// <param name="uri">
        /// The uri of the <see cref="BitmapImage"/>
        /// </param>
        /// <returns>
        /// An instance of <see cref="BitmapImage"/>
        /// </returns>
        BitmapImage QueryBitmapImage(Uri uri);

        /// <summary>
        /// Queries the <see cref="BitmapSource"/> with an error icon overlayed from the cache if the cache contains it else the <see cref="BitmapSource"/>
        /// is created, added to the cache and then returned.
        /// </summary>
        /// <param name="uri">
        /// The uri of the <see cref="BitmapSource"/>
        /// </param>
        /// <returns>
        /// An instance of <see cref="BitmapSource"/>
        /// </returns>
        BitmapSource QueryErrorOverlayBitmapSource(Uri uri);

        /// <summary>
        /// Queries the <see cref="BitmapSource"/> with an icon overlayed from the cache if the cache contains it else the <see cref="BitmapSource"/>
        /// is created, added to the cache and then returned.
        /// </summary>
        /// <param name="uri">
        /// The uri of the <see cref="BitmapSource"/>
        /// </param>
        /// <param name="overlayUri">
        /// The uri of the overlay <see cref="BitmapSource"/>
        /// </param>
        /// <param name="overlayPosition">The overlay position</param>
        /// <returns>
        /// An instance of <see cref="BitmapSource"/>
        /// </returns>
        BitmapSource QueryOverlayBitmapSource(Uri uri, Uri overlayUri, OverlayPositionKind overlayPosition = OverlayPositionKind.TopLeft);
    }
}
