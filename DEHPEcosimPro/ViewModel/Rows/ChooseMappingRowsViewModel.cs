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

namespace DEHPEcosimPro.ViewModel.Rows
{
    using System.Collections.Generic;
    using System.Linq;

    using ReactiveUI;
    
    /// <summary>
    /// Represent a row for the dialog <see cref="ChooseMappingColumnsViewModel"/>
    /// </summary>
    public class ChooseMappingRowsViewModel : ReactiveObject
    {
        /// <summary>
        /// represent the index of the column we want to map, like [x] 
        /// </summary>
        private string index;

        /// <summary>
        /// Name of the parameter column the user selected
        /// </summary>
        private string selectedColumnMatched;

        /// <summary>
        /// Represent a row for the dialog <see cref="ChooseMappingColumnsViewModel"/>
        /// </summary>
        public ChooseMappingRowsViewModel()
        {
        }

        /// <summary>
        /// Represent a row for the dialog <see cref="ChooseMappingColumnsViewModel"/>
        /// </summary>
        /// <param name="variable">list of variable to match as columns</param>
        /// <param name="isList">is the array a table with only one column, so a list ? like data[x] and not value[x,y]</param>
        public ChooseMappingRowsViewModel(List<VariableRowViewModel> variable, bool isList = false)
        {
            this.Index = variable.Count != 0 ? this.GetIndexOfRow(variable, isList) : "";
            this.SelectedColumnMatched = null;
        }

        /// <summary>
        /// represent the index of the column we want to map, like [x] 
        /// </summary>
        public string Index
        {
            get => this.index;
            set => this.RaiseAndSetIfChanged(ref this.index, value);
        }

        /// <summary>
        /// Name of the parameter column the user selected
        /// </summary>
        public string SelectedColumnMatched
        {
            get => this.selectedColumnMatched;
            set => this.RaiseAndSetIfChanged(ref this.selectedColumnMatched, value);
        }

        /// <summary>
        /// take the number of the column of the variable
        /// </summary>
        /// <param name="list">list of variable </param>
        /// <param name="isList">is the array a table with only one column, so a list ? like data[x] and not value[x,y]</param>
        /// <returns>a string like [x] that represent the index of the column we want to map</returns>
        private string GetIndexOfRow(List<VariableRowViewModel> list, bool isList = false)
        {
            if (isList)
            {
                return "[1]";
            }

            if (list.FirstOrDefault().Name.Contains('['))
            {
                var colMax = 0;

                foreach (var variable in list)
                {
                    var splitedName = variable.Name.Split('[', ',', ']');

                    if (splitedName.Length >= 3)
                    {
                        var num = splitedName.Length - 2;
                        var isNumberRow = int.TryParse(splitedName[num], out var numberRow);
                        colMax = isNumberRow && numberRow > colMax ? numberRow : colMax;
                    }
                }

                if (colMax > 0)
                {
                    return $"[{colMax}]";
                }
            }

            return "";
        }
    }
}
