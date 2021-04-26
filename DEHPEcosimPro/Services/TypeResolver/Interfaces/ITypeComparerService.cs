// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ITypeComparerService.cs" company="RHEA System S.A.">
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
    using CDP4Common.SiteDirectoryData;

    /// <summary>
    /// Interface definition for the <see cref="TypeComparerService"/>
    /// </summary>
    public interface ITypeComparerService
    {
        /// <summary>
        /// Verify if the <paramref name="parameterType"/> is compatible with the <paramref name="variable"/>
        /// </summary>
        /// <param name="parameterType">The <see cref="ParameterType"/></param>
        /// <param name="variable">The <see cref="object"/> variable value</param>
        /// <returns>A value indicating whether the <paramref name="parameterType"/> is compatible with the <paramref name="variable"/></returns>
        bool AreCompatible(ParameterType parameterType, object variable);

        /// <summary>
        /// Verify if the <paramref name="parameterType"/> is compatible with the <paramref name="variable"/>
        /// </summary>
        /// <param name="parameterType">The <see cref="ArrayParameterType"/></param>
        /// <param name="variable">The <see cref="object"/> variable value</param>
        /// <returns>A value indicating whether the <paramref name="parameterType"/> is compatible with the <paramref name="variable"/></returns>
        bool AreCompatible(ArrayParameterType parameterType, object variable);

        /// <summary>
        /// Verify if the <paramref name="parameterType"/> is compatible with the <paramref name="variable"/>
        /// </summary>
        /// <param name="parameterType">The <see cref="CompoundParameterType"/></param>
        /// <param name="variable">The <see cref="object"/> variable value</param>
        /// <returns>A value indicating whether the <paramref name="parameterType"/> is compatible with the <paramref name="variable"/></returns>
        bool AreCompatible(CompoundParameterType parameterType, object variable);

        /// <summary>
        /// Verify if the <paramref name="parameterType"/> is compatible with the <paramref name="variable"/>
        /// </summary>
        /// <param name="parameterType">The <see cref="EnumerationParameterType"/></param>
        /// <param name="variable">The <see cref="object"/> variable value</param>
        /// <returns>A value indicating whether the <paramref name="parameterType"/> is compatible with the <paramref name="variable"/></returns>
        bool AreCompatible(EnumerationParameterType parameterType, object variable);

        /// <summary>
        /// Verify if the <paramref name="parameterTypeComponent"/> is compatible with the <paramref name="variable"/>
        /// </summary>
        /// <param name="parameterTypeComponent">The <see cref="ParameterTypeComponent"/></param>
        /// <param name="variable">The <see cref="object"/> variable value</param>
        /// <returns>A value indicating whether the <paramref name="parameterTypeComponent"/> is compatible with the <paramref name="variable"/></returns>
        bool AreCompatible(ParameterTypeComponent parameterTypeComponent, object variable);

        /// <summary>
        /// Verify if the <paramref name="parameterType"/> is compatible with the <paramref name="variable"/>
        /// </summary>
        /// <param name="parameterType">The <see cref="SampledFunctionParameterType"/></param>
        /// <param name="variable">The <see cref="object"/> variable value</param>
        /// <returns>A value indicating whether the <paramref name="parameterType"/> is compatible with the <paramref name="variable"/></returns>
        bool AreCompatible(SampledFunctionParameterType parameterType, object variable);
    }
}
