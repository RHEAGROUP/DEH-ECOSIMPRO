// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectTypeResolverService.cs" company="RHEA System S.A.">
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
    using NLog;

    using System;
    using System.Globalization;

    using DEHPEcosimPro.Services.TypeResolver.Interfaces;

    /// <summary>
    /// The <see cref="ObjectTypeResolverService"/> provides a way to resolve opc variable type and translate it to .Net type
    /// </summary>
    public class ObjectTypeResolverService : IObjectTypeResolverService
    {
        /// <summary>
        /// The <see cref="NLog.Logger"/>
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Resolves the <see cref="Type"/> of the <paramref name="value"/> expressed as a string
        /// </summary>
        /// <param name="value">The <see cref="object"/> value</param>
        /// <returns>The <see cref="Type"/> of the <paramref name="value"/></returns>
        public Type Resolve(object value)
        {
            if (value is string valueString)
            {
                return this.Resolve(valueString).GetType();
            }

            return value.GetType();
        }

        /// <summary>
        /// Resolves the <see cref="Type"/> of the <paramref name="value"/> expressed as a string
        /// </summary>
        /// <param name="value">The <see cref="string"/> value</param>
        /// <returns>The <paramref name="value"/> as its apropriate <see cref="Type"/></returns>
        public object Resolve(string value)
        {
            if (uint.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var valueAsUInt))
            {
                return valueAsUInt;
            }

            if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var valueAsInt))
            {
                return valueAsInt;
            }

            if (double.TryParse(value,NumberStyles.Any, CultureInfo.InvariantCulture, out var valueAsDouble))
            {
                return valueAsDouble;
            }

            if (bool.TryParse(value, out var valueAsBoolean))
            {
                return valueAsBoolean;
            }

            this.logger.Info($"Value {{{value}}} could not be resolved in any other .net built-in type other than string");
            return value;
        }
        
        /// <summary>
        /// Validates if the provided <paramref name="value"/> is of the specified <see cref="TType"/>
        /// </summary>
        /// <typeparam name="TType">The supposed type of the <paramref name="value"/></typeparam>
        /// <param name="value">The <see cref="string"/> value</param>
        /// <returns>A value indicating whether the value is of the <typeparamref name="TType"/></returns>
        public bool Is<TType>(string value)
        {
            return this.Resolve(value).GetType() == typeof(TType);
        }

        /// <summary>
        /// Validates if the provided <paramref name="value"/> is of the specified <see cref="TType"/>
        /// </summary>
        /// <typeparam name="TType">The supposed type of the <paramref name="value"/></typeparam>
        /// <param name="value">The <see cref="string"/> value</param>
        /// <returns>A value indicating whether the value is of the <typeparamref name="TType"/></returns>
        public bool Is<TType>(object value)
        {
            return this.Resolve(value) == typeof(TType);
        }
    }
}
