// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IOpcVariableTypeResolverService.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Services.TypeResolver.Interfaces
{
    using System;

    /// <summary>
    /// Interface definition for the <see cref="ObjectTypeResolverService"/>
    /// </summary>
    public interface IObjectTypeResolverService
    {
        /// <summary>
        /// Resolves the <see cref="Type"/> of the <paramref name="value"/> expressed as a string
        /// </summary>
        /// <param name="value">The <see cref="object"/> value</param>
        /// <returns>The <see cref="Type"/> of the <paramref name="value"/></returns>
        Type Resolve(object value);

        /// <summary>
        /// Resolves the <see cref="Type"/> of the <paramref name="value"/> expressed as a string
        /// </summary>
        /// <param name="value">The <see cref="string"/> value</param>
        /// <returns>The <paramref name="value"/> as its apropriate <see cref="Type"/></returns>
        object Resolve(string value);

        /// <summary>
        /// Validates if the provided <paramref name="value"/> is of the specified <see cref="TType"/>
        /// </summary>
        /// <typeparam name="TType">The supposed type of the <paramref name="value"/></typeparam>
        /// <param name="value">The <see cref="string"/> value</param>
        /// <returns>A value indicating whether the value is of the <typeparamref name="TType"/></returns>
        bool Is<TType>(string value);

        /// <summary>
        /// Validates if the provided <paramref name="value"/> is of the specified <see cref="TType"/>
        /// </summary>
        /// <typeparam name="TType">The supposed type of the <paramref name="value"/></typeparam>
        /// <param name="value">The <see cref="string"/> value</param>
        /// <returns>A value indicating whether the value is of the <typeparamref name="TType"/></returns>
        bool Is<TType>(object value);
    }
}
