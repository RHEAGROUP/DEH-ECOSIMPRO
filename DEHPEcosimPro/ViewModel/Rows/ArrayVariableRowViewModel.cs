// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayVariableRowViewModel.cs" company="RHEA System S.A.">
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

    using Opc.Ua;

    using ReactiveUI;

    /// <summary>
    /// Array of <see cref="VariableBaseRowViewModel" />
    /// </summary>
    public class ArrayVariableRowViewModel : VariableBaseRowViewModel
    {
        /// <summary>
        /// Backing field for <see cref="Name" />
        /// </summary>
        private string name;

        /// <summary>
        /// list of int that gave the maximum dimension of the array, like [3], [2x2], [2x2x3], ...
        /// </summary>
        public List<int> DimensionOfTheArray { get; set; } = new List<int>();

        /// <summary>
        /// Number of dimension of the array, 1 for a list, 2 for a table, 3 for x,y,x, etc
        /// </summary>
        public int Dimensions { get; set; }

        /// <summary>
        /// Constructor of <see cref="ArrayVariableRowViewModel" />
        /// </summary>
        public ArrayVariableRowViewModel()
        { }

        /// <summary>
        /// Constructor of <see cref="ArrayVariableRowViewModel" />
        /// </summary>
        /// <param name="key">key of the grouping, correspond to the name of the variable</param>
        /// <param name="enumerable">list of <see cref="VariableRowViewModel" /> to add</param>
        public ArrayVariableRowViewModel(string key, IEnumerable<VariableRowViewModel> enumerable)
        {
            this.Variables.AddRange(enumerable.OrderByDescending(x => x.Index));
            this.Name = key;
            this.ActualValue = this.GetDimension();
            this.SetVariableRowName();
        }

        /// <summary>
        /// list of <see cref="VariableRowViewModel" />
        /// </summary>
        public ReactiveList<VariableRowViewModel> Variables { get; set; } = new ReactiveList<VariableRowViewModel>();

        /// <summary>
        /// Is the array a table with multiple columns or is it a list (a table with only one column)
        /// </summary>
        public bool IsList { get; private set; }

        /// <summary>
        /// Gets the name of the represented reference
        /// </summary>
        public override string Name
        {
            get => this.name;
            set => this.RaiseAndSetIfChanged(ref this.name, value);
        }

        /// <summary>
        /// Get the dimension of the array
        /// </summary>
        /// <returns>string presented like [AxB]</returns>
        private string GetDimension()
        {
            var arrayName = this.Variables.Select(x => x.Name).ToList();

            if (arrayName.Count != 0)
            {
                var isDimension = this.SetDimension(arrayName);

                if (isDimension)
                {
                    this.IsList = this.DimensionOfTheArray.Count == 1 ;
                    return "[" + string.Join("x", this.DimensionOfTheArray) + "]" ;
                }
            }

            return "/";
        }

        /// <summary>
        /// If the Variable row is an array, return the dimension of the array
        /// </summary>
        /// <param name="arrayName">list of name of the data, should have [x] or [x,y]</param>
        /// <returns>the variable is an array</returns>
        public bool SetDimension(List<string> arrayName)
        {
            if (arrayName.FirstOrDefault() != null && arrayName.FirstOrDefault().Contains('['))
            {
                var dim1 = arrayName.FirstOrDefault().Split('[', ']').Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                dim1.RemoveAt(0);
                var dim2 = dim1[0].Split(',').Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                
                foreach (var dim in dim2)
                {
                    this.DimensionOfTheArray.Add(0);
                }

                this.Dimensions = this.DimensionOfTheArray.Count;

                foreach (var variable in arrayName)
                {
                    var isArray = this.GetIndexFromName(variable);

                    if (isArray != null && isArray.Length >= 1)
                    {
                        for(var i = 0; i < isArray.Length; i++)
                        {
                            this.DimensionOfTheArray[i] = this.IsMax(this.DimensionOfTheArray[i], isArray[i]);
                        }
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Compare two number and return the bigger
        /// </summary>
        /// <param name="max">the bigger number</param>
        /// <param name="number">the number we want to compare</param>
        /// <returns>the bigger number</returns>
        private int IsMax(int max, string number)
        {
            var isNumberRow = int.TryParse(number, out var numberRow);
            return isNumberRow && numberRow > max ? numberRow : max;
        }

        /// <summary>
        /// set the dimension as the name for each row
        /// </summary>
        private void SetVariableRowName()
        {
            foreach (var row in this.Variables)
            {
                row.SetArrayIndexAndName(row.Name);
                row.Name = "[" + string.Join("x", row.IndexOfThisRow) + "]";
            }
        }

        /// <summary>
        /// Get the rows and column from the variable name
        /// </summary>
        /// <param name="variable">variable name as Syst[x] or Syst[x,y]</param>
        /// <returns>a list of string with number as the rows and columns</returns>
        private string[] GetIndexFromName(string variable)
        {

            var splitedName = variable.Split('[', ']').Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            splitedName.RemoveAt(0);

            var numberAsString = splitedName.Count >= 1
                ? splitedName[0]
                : null;

            return numberAsString?.Length >= 1
                ? numberAsString.Split(',')
                : null;
        }
    }
}
