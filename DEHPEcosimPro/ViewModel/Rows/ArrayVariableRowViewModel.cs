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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    
    using ReactiveUI;

    /// <summary>
    /// Array of <see cref="VariableBaseRowViewModel" />
    /// </summary>
    public class ArrayVariableRowViewModel : VariableBaseRowViewModel
    {
        /// <summary>
        /// Gets or sets the dimensions of the represented array
        /// </summary>
        public List<int> Dimensions { get; set; } = new();
        
        /// <summary>
        /// Initializes a new <see cref="ArrayVariableRowViewModel" />
        /// </summary>
        public ArrayVariableRowViewModel()
        {
        }

        /// <summary>
        /// Constructor of <see cref="ArrayVariableRowViewModel" />
        /// </summary>
        /// <param name="name">The name of this array variable</param>
        /// <param name="variableItems">The collection of of <see cref="VariableRowViewModel" /> that belongs to this array</param>
        public ArrayVariableRowViewModel(string name, IEnumerable<VariableRowViewModel> variableItems)
        {
            var variableRowViewModels = variableItems
                .Select(x =>
                {
                    x.SetsArrayItemProperties();
                    return x;
                })
                .OrderBy(x => x.Index, Comparer<List<int>>.Create(this.CompareIndexes)).ToList();

            this.Variables.AddRange(variableRowViewModels);

            this.Name = name;
            this.Dimensions = this.Variables.Last().Index;
            this.ActualValue = $"[{string.Join("x", this.Variables.Last().Index)}]";
        }
        
        /// <summary>
        /// Defines how to compare two collections of <see cref="int"/> of equal size 
        /// </summary>
        /// <param name="list0">The first <see cref="List{T}"/></param>
        /// <param name="list1">The second <see cref="List{T}"/></param>
        /// <returns></returns>
        private int CompareIndexes(List<int> list0, List<int> list1)
        {
            var result = 0;

            for (var index = 0; index < list0.Count && result == 0; index++)
            {
                result = index switch
                {
                    _ when index >= list1.Count => 1,
                    _ when list0[index] > list1[index] => 1,
                    _ when list0[index] < list1[index] => -1,
                    _ when index + 1 == list0.Count => list1.Count > list0.Count ? -1 : 0,
                    _ => result
                };
            }

            return result;
        }

        /// <summary>
        /// list of <see cref="VariableRowViewModel" />
        /// </summary>
        public ReactiveList<VariableRowViewModel> Variables { get; set; } = new ReactiveList<VariableRowViewModel>();

        /// <summary>
        /// Is the array a table with multiple columns or is it a list (a table with only one column)
        /// </summary>
        public bool HasOnlyOneDimension => this.Variables.FirstOrDefault()?.Index.Count == 1;

        /// <summary>
        /// Gets the number of values assignable in this represented array variable
        /// </summary>
        public int NumberOfValues => this.Dimensions.Aggregate((dimension0, dimension1) => dimension0 * dimension1);
    }
}
