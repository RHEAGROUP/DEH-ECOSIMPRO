// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TypeComparerService.cs" company="RHEA System S.A.">
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
    using System.Linq;

    using CDP4Common.SiteDirectoryData;

    using DEHPEcosimPro.Extensions;
    using DEHPEcosimPro.Services.TypeResolver.Interfaces;
    
    using NLog;

    /// <summary>
    /// The <see cref="TypeComparerService"/>
    /// </summary>
    public class TypeComparerService : ITypeComparerService
    {
        /// <summary>
        /// The NLog <see cref="Logger"/>
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="IParameterTypeTypeResolverService"/>
        /// </summary>
        private readonly IParameterTypeTypeResolverService parameterTypeTypeResolver;

        /// <summary>
        /// The <see cref="IObjectTypeResolverService"/>
        /// </summary>
        private readonly IObjectTypeResolverService variableTypeResolver;

        /// <summary>
        /// Initializes a new <see cref="TypeComparerService"/>
        /// </summary>
        /// <param name="parameterTypeTypeResolver">The <see cref="IParameterTypeTypeResolverService"/></param>
        /// <param name="variableTypeResolver">The <see cref="IObjectTypeResolverService"/></param>
        public TypeComparerService(IParameterTypeTypeResolverService parameterTypeTypeResolver, IObjectTypeResolverService variableTypeResolver)
        {
            this.parameterTypeTypeResolver = parameterTypeTypeResolver;
            this.variableTypeResolver = variableTypeResolver;
        }

        /// <summary>
        /// Verify if the <paramref name="parameterType"/> is compatible with the <paramref name="variable"/>
        /// </summary>
        /// <param name="parameterType">The <see cref="ParameterType"/></param>
        /// <param name="variable">The <see cref="object"/> variable value</param>
        /// <returns>A value indicating whether the <paramref name="parameterType"/> is compatible with the <paramref name="variable"/></returns>
        public bool AreCompatible(ParameterType parameterType, object variable)
        {
            try
            {
                return !this.IsAnyParameterNull(parameterType, variable)
                       && this.parameterTypeTypeResolver.Resolve(parameterType) == this.variableTypeResolver.Resolve(variable);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                this.logger.Error(exception);
                return false;
            }
        }

        /// <summary>
        /// Verify if the <paramref name="parameterType"/> is compatible with the <paramref name="variable"/>
        /// </summary>
        /// <param name="parameterType">The <see cref="ArrayParameterType"/></param>
        /// <param name="variable">The <see cref="object"/> variable value</param>
        /// <returns>A value indicating whether the <paramref name="parameterType"/> is compatible with the <paramref name="variable"/></returns>
        public bool AreCompatible(ArrayParameterType parameterType, object variable)
        {
            if (this.IsAnyParameterNull(parameterType, variable))
            {
                return false;
            }

            return parameterType.HasSingleComponentType
                    ? this.AreCompatible(parameterType.Component.FirstOrDefault(), variable) 
                    : parameterType.Component.Any(x => this.AreCompatible(x, variable));
        }
        
        /// <summary>
        /// Verify if the <paramref name="parameterType"/> is compatible with the <paramref name="variable"/>
        /// </summary>
        /// <param name="parameterType">The <see cref="CompoundParameterType"/></param>
        /// <param name="variable">The <see cref="object"/> variable value</param>
        /// <returns>A value indicating whether the <paramref name="parameterType"/> is compatible with the <paramref name="variable"/></returns>
        public bool AreCompatible(CompoundParameterType parameterType, object variable)
        {
            return !this.IsAnyParameterNull(parameterType, variable)
                   && parameterType.Component.Any(x => this.AreCompatible(x, variable));
        }

        /// <summary>
        /// Verify if the <paramref name="parameterType"/> is compatible with the <paramref name="variable"/>
        /// </summary>
        /// <param name="parameterType">The <see cref="EnumerationParameterType"/></param>
        /// <param name="variable">The <see cref="object"/> variable value</param>
        /// <returns>A value indicating whether the <paramref name="parameterType"/> is compatible with the <paramref name="variable"/></returns>
        public bool AreCompatible(EnumerationParameterType parameterType, object variable)
        {
            return !this.IsAnyParameterNull(parameterType, variable) 
                   && parameterType.ValueDefinition.Any(x => 
                string.Equals(x.ShortName, variable.ToString(), StringComparison.CurrentCultureIgnoreCase));
        }
        
        /// <summary>
        /// Verify if the <paramref name="parameterTypeComponent"/> is compatible with the <paramref name="variable"/>
        /// </summary>
        /// <param name="parameterTypeComponent">The <see cref="ParameterTypeComponent"/></param>
        /// <param name="variable">The <see cref="object"/> variable value</param>
        /// <returns>A value indicating whether the <paramref name="parameterTypeComponent"/> is compatible with the <paramref name="variable"/></returns>
        public bool AreCompatible(ParameterTypeComponent parameterTypeComponent, object variable)
        {
            return this.AreCompatible(parameterTypeComponent?.ParameterType, variable);
        }

        /// <summary>
        /// Verify if the <paramref name="parameterType"/> is compatible with the <paramref name="variable"/>
        /// </summary>
        /// <param name="parameterType">The <see cref="SampledFunctionParameterType"/></param>
        /// <param name="variable">The <see cref="object"/> variable value</param>
        /// <returns>A value indicating whether the <paramref name="parameterType"/> is compatible with the <paramref name="variable"/></returns>
        public bool AreCompatible(SampledFunctionParameterType parameterType, object variable)
        {
            if (!this.IsAnyParameterNull(parameterType, variable)
                && parameterType.HasTheRightNumberOfParameterType(out var independentParameterType, out var dependentParameterType) 
                && independentParameterType.IsQuantityKindOrText())
            {
                return this.AreCompatible(dependentParameterType, variable);
            }

            return false;
        }

        /// <summary>
        /// Assert that any of the parameter is null
        /// </summary>
        /// <param name="parameterType">The <see cref="ParameterType"/></param>
        /// <param name="variable">The <see cref="object"/> variable value</param>
        /// <returns>A value indicating whether the <paramref name="parameterType"/> is null or the <paramref name="variable"/> is null</returns>
        private bool IsAnyParameterNull(ParameterType parameterType, object variable)
        {
            if (parameterType is null || variable is null)
            {
                this.logger.Warn($"Trying to verify compatibility of one parameter type with one object " +
                                 $"with one null argument, the {nameof(parameterType)} or the {nameof(variable)} is null");

                return true;
            }

            return false;
        }
    }
}
