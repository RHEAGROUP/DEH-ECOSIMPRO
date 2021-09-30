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
        /// The represented <see cref="ReferenceDescription" /> corresponding <see cref="DataValue" />
        /// </summary>
        private readonly DataValue data;

        /// <summary>
        /// The represented <see cref="ReferenceDescription" />
        /// </summary>
        public readonly ReferenceDescription Reference;

        /// <summary>
        /// Backing field for <see cref="Name" />
        /// </summary>
        private string name;

        /// <summary>
        /// Constructor of <see cref="ArrayVariableRowViewModel" />
        /// </summary>
        /// <param name="key">indice of the array</param>
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
            var arrayName = this.Variables.Select(x => x.Name);

            if (arrayName != null)
            {
                var isDimension = this.SetDimension(arrayName, out var result);

                if (isDimension)
                {
                    return result;
                }
            }

            return "/";
        }

        /// <summary>
        /// If the Variable row is an array, return the dimension of the array
        /// </summary>
        /// <param name="arrayName">list of name of the data, should have [x] or [x,y]</param>
        /// <param name="result">a string of the dimension, like [AxB]</param>
        /// <returns></returns>
        private bool SetDimension(IEnumerable<string> arrayName, out string result)
        {
            if (arrayName.FirstOrDefault().Contains('['))
            {
                var rowMax = 0;
                var colMax = 0;

                foreach (var variable in arrayName)
                {
                    var splitedName = variable.Split('[', ']');

                    var numberAsString = splitedName.Length != 2 && splitedName.Length >= 1
                        ? splitedName[1]
                        : null;

                    var isArray = numberAsString?.Length >= 1
                        ? numberAsString.Split(',')
                        : null;

                    if (isArray != null && isArray.Length == 1)
                    {
                        colMax = 1;

                        rowMax = this.IsMax(rowMax, isArray[0]);
                    }
                    else if (isArray != null && isArray.Length > 1)
                    {
                        rowMax = this.IsMax(rowMax, isArray[0]);
                        colMax = this.IsMax(colMax, isArray[1]);
                    }
                }

                if (colMax == 1)
                {
                    this.IsList = true;
                }

                if (rowMax > 0 && colMax > 0)
                {
                    result = $"[{rowMax}x{colMax}]";
                    return true;
                }
            }

            result = "";
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
                row.Name = this.GetDimension();
            }
        }
    }
}
