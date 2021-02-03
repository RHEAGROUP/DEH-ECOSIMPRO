// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappingDirectionToBooleanConverter.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Converters
{
    using System;
    using System.Windows;
    using System.Windows.Data;

    using DEHPCommon.Enumerators;

    /// <summary>
    /// Convert a <see cref="MappingDirection"/> to a boolean depending on the caller name.
    /// this converter is used to determine
    /// </summary>
    public class MappingDirectionToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a <see cref="MappingDirection"/> to a boolean
        /// </summary>
        /// <param name="value">An instance of an object which needs to be converted.</param>
        /// <param name="targetType">The parameter is not used.</param>
        /// <param name="parameter">The parameter is used to pass the information about the caller
        /// i.e. if the caller identify itself as being the hub preview panel then if the mapping direction allows it this method should return true</param>
        /// <param name="culture">The parameter is not used.</param>
        /// <returns>A <see cref="bool"/></returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var result = false;

            if (value is MappingDirection mappingDirection && parameter is string caller)
            {
                result = (caller.Contains("Hub") && mappingDirection == MappingDirection.FromDstToHub)
                             || (caller.Contains("Dst") && mappingDirection == MappingDirection.FromHubToDst);
            }

            return result;
        }

        /// <summary>
        /// not supported
        /// </summary>
        /// <param name="value">The parameter is not used.</param>
        /// <param name="targetType">The parameter is not used.</param>
        /// <param name="parameter">The parameter is not used.</param>
        /// <param name="culture">The parameter is not used.</param>
        /// <returns>A <see cref="NotSupportedException"/> is thrown</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
