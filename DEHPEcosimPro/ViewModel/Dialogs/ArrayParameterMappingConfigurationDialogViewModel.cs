// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChooseMappingColumnsViewModel.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2021 RHEA System S.A.
// 
//    Author: Sam Gerené, Alex Vorobiev, Alexander van Delft, Nathanael Smiechowski, Arielle Petit.
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

namespace DEHPEcosimPro.ViewModel.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.ViewModel.Rows;

    using ReactiveUI;

    /// <summary>
    /// ViewModel of the dialog to match columns of parameter to a variable
    /// </summary>
    public class ArrayParameterMappingConfigurationDialogViewModel : ReactiveObject, ICloseWindowViewModel
    {
        /// <summary>
        /// Initializes a new <see cref="ArrayParameterMappingConfigurationDialogViewModel"/>
        /// </summary>
        public ArrayParameterMappingConfigurationDialogViewModel()
        {
        }

        /// <summary>
        /// Initializes a new <see cref="ArrayParameterMappingConfigurationDialogViewModel"/>
        /// <param name="variable">The <see cref="ArrayVariableRowViewModel"/></param>
        /// <param name="parameter">The <see cref="ParameterBase"/></param>
        /// </summary>
        public ArrayParameterMappingConfigurationDialogViewModel(ArrayVariableRowViewModel variable, ParameterOrOverrideBase parameter)
        {
            if (parameter.ParameterType is SampledFunctionParameterType parameterType)
            {
                this.ParameterNames.AddRange(parameterType.IndependentParameterType
                    .Select(x => x.ParameterType.ShortName));

                this.ParameterNames.AddRange(parameterType.DependentParameterType
                    .Select(x => x.ParameterType.ShortName));
            }
            
            this.hasOnlyOneDimension = variable.HasOnlyOneDimension;

            if (variable.HasOnlyOneDimension)
            {
                this.MappingRows.Add(new ArrayParameterMappingConfigurationRowViewModel(variable.Variables.First().Index, variable.Variables));
            }
            else
            {
                foreach (var group in variable.Variables.GroupBy(x => x.Index.Skip(1)))
                {
                    this.MappingRows.Add(new ArrayParameterMappingConfigurationRowViewModel(group.Key, group));
                }
            }
        
            this.ParameterName = parameter.ModelCode();
        }

        /// <summary>
        /// Gets or sets the <see cref="ArrayVariableRowViewModel"/> that is to be mapped
        /// </summary>
        public ArrayVariableRowViewModel ArrayVariable { get; set; }

        /// <summary>
        /// Name of the parameter to map from
        /// </summary>
        public string ParameterName { get; set; }

        /// <summary>
        /// is the array a table with only one column, so a list ? like data[x] and not value[x,y]
        /// </summary>
        public bool hasOnlyOneDimension { get; set; }

        /// <summary>
        /// Gets or sets a collection of <see cref="ArrayParameterMappingConfigurationRowViewModel"/>
        /// </summary>
        public ReactiveList<ArrayParameterMappingConfigurationRowViewModel> MappingRows { get; } = new();

        /// <summary>
        /// Gets or sets a collection of <see cref="string"/> that will feed the dropdown of the dialog, from parameter
        /// </summary>
        public ReactiveList<string> ParameterNames { get; set; } = new();

        /// <summary>
        /// Interface to close the dialog
        /// </summary>
        public ICloseWindowBehavior CloseWindowBehavior { get; set; }
    }
}
