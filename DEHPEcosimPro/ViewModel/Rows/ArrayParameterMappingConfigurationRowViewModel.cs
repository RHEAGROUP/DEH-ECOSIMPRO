// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChooseMappingRowsViewModel.cs" company="RHEA System S.A.">
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
    /// Represent a row for the dialog <see cref="ArrayParameterMappingConfigurationRowViewModel"/>
    /// </summary>
    public class ArrayParameterMappingConfigurationRowViewModel : ReactiveObject
    {
        /// <summary>
        /// Backing field for <see cref="IndexRepresentation"/>
        /// </summary>
        private string indexRepresentation;

        /// <summary>
        /// Backing field for <see cref="SelectedParameterType"/>
        /// </summary>
        private string selectedParameterType;

        /// <summary>
        /// Initializes a new <see cref="ArrayParameterMappingConfigurationRowViewModel"/>
        /// </summary>
        public ArrayParameterMappingConfigurationRowViewModel()
        {
        }

        /// <summary>
        /// Represent a row for the dialog <see cref="ArrayParameterMappingConfigurationRowViewModel"/>
        /// </summary>
        /// <param name="index">The collection of int representind the represented index by this row view model</param>
        /// <param name="variables">The <see cref="IEnumerable{T}"/> of variable to match as columns</param>
        public ArrayParameterMappingConfigurationRowViewModel(IEnumerable<int> index, IEnumerable<VariableRowViewModel> variables)
        {
            this.Variables = variables;

            this.Index = index.ToList() switch
            {
                { Count: 1 } x => x,
                { Count: > 1} x => x.Skip(1).ToList(),
                _ => throw new ArgumentException("The index of the represented variables cannot be empty")
            };

            this.IndexRepresentation = $"[{string.Join(",", this.Index)}]";
        }

        /// <summary>
        /// Gets the collection of <see cref="VariableRowViewModel"/> where the <see cref="IndexRepresentation"/> matches
        /// </summary>
        public IEnumerable<VariableRowViewModel> Variables { get; }

        /// <summary>
        /// Gets the index represented by this row
        /// </summary>
        public List<int> Index { get; private set; }

        /// <summary>
        /// represent the index of the column we want to map, like [x] 
        /// </summary>
        public string IndexRepresentation
        {
            get => this.indexRepresentation;
            set => this.RaiseAndSetIfChanged(ref this.indexRepresentation, value);
        }

        /// <summary>
        /// Gets or sets the ShortName of the selected parameter type
        /// </summary>
        public string SelectedParameterType
        {
            get => this.selectedParameterType;
            set => this.RaiseAndSetIfChanged(ref this.selectedParameterType, value);
        }
    }
}
