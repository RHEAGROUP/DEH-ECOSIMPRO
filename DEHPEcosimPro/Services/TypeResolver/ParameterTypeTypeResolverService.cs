// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ParameterTypeTypeResolverService.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Services.TypeResolver
{
    using System;

    using CDP4Common.SiteDirectoryData;

    using DEHPEcosimPro.Services.TypeResolver.Interfaces;

    /// <summary>
    /// The <see cref="ParameterTypeTypeResolverService"/> provides an easy way to determine the corresponding .net type
    /// </summary>
    public class ParameterTypeTypeResolverService : IParameterTypeTypeResolverService
    {
        /// <summary>
        /// Determine what .net type the <paramref name="parameterType"/> corresponds to
        /// </summary>
        /// <param name="parameterType">The <see cref="ParameterType"/></param>
        /// <returns>A <see cref="Type"/></returns>
        public Type Resolve(ParameterType parameterType) =>
            parameterType switch
            {
                QuantityKind quantityKind =>
                quantityKind.DefaultScale?.NumberSet switch
                {
                    NumberSetKind.INTEGER_NUMBER_SET => typeof(int),
                    NumberSetKind.NATURAL_NUMBER_SET => typeof(uint),
                    _ => typeof(double),
                },
                TextParameterType _ => typeof(string),
                var x when x is TimeOfDayParameterType || x is DateParameterType || x is DateTimeParameterType
                => typeof(DateTime),
                BooleanParameterType _ => typeof(bool),
                _ => throw new ArgumentOutOfRangeException($"The {nameof(parameterType)} of type {parameterType.ClassKind} " +
                                                           $"is not resolvable to any .Net built in value type or to a string")
            };
    }
}
