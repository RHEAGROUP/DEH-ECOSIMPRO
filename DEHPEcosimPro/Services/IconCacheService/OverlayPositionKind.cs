// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OverlayPositionKind.cs" company="RHEA System S.A.">
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
    /// <summary>
    /// Assertion on the overlay position
    /// </summary>
    public enum OverlayPositionKind
    {
        /// <summary>
        /// Asserts that the overlay shall be placed on the top left corner
        /// </summary>
        TopLeft,

        /// <summary>
        /// Asserts that the overlay shall be placed on the top right corner
        /// </summary>
        TopRight,

        /// <summary>
        /// Asserts that the overlay shall be placed in the bottom left corner
        /// </summary>
        BottomLeft,

        /// <summary>
        /// Asserts that the overlay shall be palced on the bottom right corner
        /// </summary>
        BottomRight
    }
}
