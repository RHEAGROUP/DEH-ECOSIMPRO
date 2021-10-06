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
    using System.Collections.Generic;
    using System.Linq;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.ViewModel.Rows;

    using DevExpress.Mvvm.Native;

    using ReactiveUI;

    /// <summary>
    /// ViewModel of the dialog to match columns of parameter to a variable
    /// </summary>
    public class ChooseMappingColumnsViewModel : ReactiveObject, ICloseWindowViewModel
    {
        /// <summary>
        /// ViewModel of the dialog to match columns of parameter to a variable
        /// </summary>
        public ChooseMappingColumnsViewModel()
        {
        }

        /// <summary>
        /// ViewModel of the dialog to match columns of parameter to a variable
        /// </summary>
        public ChooseMappingColumnsViewModel(VariableBaseRowViewModel variable, ParameterOrOverrideBase parameter)
        {
            if (parameter.ParameterType is SampledFunctionParameterType parameterType)
            {
                this.ListOfParameterToMatch.AddRange(parameterType.IndependentParameterType.Select(x => x.ParameterType.ShortName));
                this.ListOfParameterToMatch.AddRange(parameterType.DependentParameterType.Select(x => x.ParameterType.ShortName));
            }

            if (variable is ArrayVariableRowViewModel array)
            {
                this.VariableName = array.Name;
                this.IsList = array.IsList;

                if (array.IsList)
                {
                    this.ListOfVariableToMap.Add(new ChooseMappingRowsViewModel(array.Variables.ToList(), array.IsList));
                }
                else
                {
                    var groupby = array.Variables.GroupBy(this.GetColumnNumberFromVariableRow).ToList();
                    foreach (var group in groupby)
                    {
                        this.ListOfVariableToMap.Add(new ChooseMappingRowsViewModel(group.ToList(), array.IsList));
                    }
                }
            }

            this.ParameterName = parameter.ModelCode();
        }

        /// <summary>
        /// Name of the variable to map to
        /// </summary>
        public string VariableName { get; set; }

        /// <summary>
        /// Name of the parameter to map from
        /// </summary>
        public string ParameterName { get; set; }

        /// <summary>
        /// is the array a table with only one column, so a list ? like data[x] and not value[x,y]
        /// </summary>
        public bool IsList { get; set; }

        /// <summary>
        /// List of the column of the variable we want to map
        /// </summary>
        public ReactiveList<ChooseMappingRowsViewModel> ListOfVariableToMap { get; } = new ReactiveList<ChooseMappingRowsViewModel>();

        /// <summary>
        /// List of value that will feed the dropdown of the dialog, from parameter
        /// </summary>
        public ReactiveList<string> ListOfParameterToMatch { get; set; } = new ReactiveList<string>();

        /// <summary>
        /// Interface to close the dialog
        /// </summary>
        public ICloseWindowBehavior CloseWindowBehavior { get; set; }
        
        /// <summary>
        /// Get the column number of the data from its index list
        /// </summary>
        /// <param name="variable"><see cref="VariableRowViewModel"/></param>
        /// <returns>as a string, a number or a comma then a number</returns>
        private string GetColumnNumberFromVariableRow(VariableRowViewModel variable)
        {
            if (this.IsList || variable.IndexOfThisRow.Count == 1)
            {
                return variable.IndexOfThisRow.FirstOrDefault();
            }
            else
            {
                var copyOfList = new string[variable.IndexOfThisRow.Count];
                variable.IndexOfThisRow.CopyTo(copyOfList);
                var trimedList = copyOfList.ToList();
                trimedList.RemoveAt(0);
                return "," + string.Join(",", trimedList);
            }
        }
    }
}
