﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SampledFunctionParameterTypeExtensions.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Extensions
{
    using System.Linq;

    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Validation;

    /// <summary>
    /// Provides extension methods for <see cref="SampledFunctionParameterType"/>
    /// </summary>
    public static class SampledFunctionParameterTypeExtensions
    {
        /// <summary>
        /// Verify that the <paramref name="parameterType"/> is compatible with this dst adapter
        /// </summary>
        /// <param name="parameterType">The <see cref="SampledFunctionParameterType"/></param>
        /// <param name="value">The <see cref="object"/> value</param>
        /// <param name="scale">The <see cref="MeasurementScale"/></param>
        /// <returns>A value indicating if the <paramref name="parameterType"/> is compliant</returns>
        public static bool Validate(this SampledFunctionParameterType parameterType, object value, MeasurementScale scale = null)
        {
            if (!parameterType.HasTheRightNumberOfParameterType(out var independantParameterType, out var dependantParameterType))
            {
                return false;
            }

            var independentValidation = independantParameterType.IsQuantityKindOrText();
            var measurementScale = scale ?? (dependantParameterType as QuantityKind)?.DefaultScale;
            var validate = dependantParameterType.Validate(value, measurementScale);
            var dependentValidation = validate.ResultKind == ValidationResultKind.Valid;

            return independentValidation && dependentValidation;

        }

        /// <summary>
        /// Verify that the <paramref name="parameterType"/> has the right number of IndependentParameterType and DependentParameterType
        /// </summary>
        /// <param name="parameterType">The <see cref="SampledFunctionParameterType"/></param>
        /// <param name="independantParameterType">The single IndependantParameterType if the output is true</param>
        /// <param name="dependantParameterType">The single dependantParameterType if the output is true</param>
        /// <returns>A value indicating if <see cref="SampledFunctionParameterType"/> has the right number of IndependentParameterType and DependentParameterType</returns>
        public static bool HasTheRightNumberOfParameterType(this SampledFunctionParameterType parameterType, out ParameterType independantParameterType, out ParameterType dependantParameterType)
        {
            independantParameterType = null;
            dependantParameterType = null;

            if (parameterType.IndependentParameterType.Count == 1 && parameterType.DependentParameterType.Count == 1
                && parameterType.IndependentParameterType.FirstOrDefault()?.ParameterType is { } iParameterType
                && parameterType.DependentParameterType.FirstOrDefault()?.ParameterType is { } dParameterType)
            {
                independantParameterType = iParameterType;
                dependantParameterType = dParameterType;
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Verify that the <paramref name="parameterType"/> is either of <see cref="TextParameterType"/> || <see cref="QuantityKind"/>
        /// </summary>
        /// <param name="parameterType">The <see cref="ParameterType"/></param>
        /// <returns>An value indicating if the <paramref name="parameterType"/> matches</returns>
        public static bool IsQuantityKindOrText(this ParameterType parameterType)
        {
            return parameterType is TextParameterType || parameterType is QuantityKind;
        }
    }
}
