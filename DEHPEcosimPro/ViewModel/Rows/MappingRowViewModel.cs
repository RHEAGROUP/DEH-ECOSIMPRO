// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappingRowViewModel.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.ViewModel.Rows
{
    using System.Linq;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using DEHPCommon.Enumerators;

    using ReactiveUI;

    /// <summary>
    /// Represents a row of mapped <see cref="ParameterOrOverrideBase"/> and <see cref="VariableRowViewModel"/>
    /// </summary>
    public class MappingRowViewModel : ReactiveObject
    {
        public MappedThing HubThing { get; set; }

        public MappedThing DstThing { get; set; }

        /// <summary>
        /// Backing field for <see cref="direction"/>
        /// </summary>
        private MappingDirection direction;

        /// <summary>
        /// Gets or sets the value
        /// </summary>
        public MappingDirection Direction
        {
            get => this.direction;
            set => this.RaiseAndSetIfChanged(ref this.direction, value);
        }

        /// <summary>
        /// Backing field for <see cref="VisualDirection"/>
        /// </summary>
        private MappingDirection visualDirection;

        /// <summary>
        /// Gets or sets the value
        /// </summary>
        public MappingDirection VisualDirection
        {
            get => this.visualDirection;
            set => this.RaiseAndSetIfChanged(ref this.visualDirection, value);
        }

        /// <summary>
        /// Initializes a new <see cref="MappingRowViewModel"/> from a <see cref="MappedElementDefinitionRowViewModel"/>
        /// </summary>
        /// <param name="mappedElement">The <see cref="MappedElementDefinitionRowViewModel"/></param>
        public MappingRowViewModel(MappedElementDefinitionRowViewModel mappedElement)
        {
            this.Direction = MappingDirection.FromHubToDst;
            
            this.DstThing = new MappedThing() 
            {
                Name = mappedElement.SelectedVariable.Name, 
                Value = mappedElement.SelectedVariable.ActualValue
            };

            this.HubThing = new MappedThing()
            {
                Name = mappedElement.SelectedParameter.ModelCode(),
                Value = mappedElement.SelectedValue.Representation
            };
        }

        /// <summary>
        /// Initializes a new <see cref="MappingRowViewModel"/> from a mapped <see cref="ParameterOrOverrideBase"/> and <see cref="VariableRowViewModel"/>
        /// </summary>
        /// <param name="parameter">The <see cref="ParameterOrOverrideBase"/></param>
        /// <param name="variable">The <see cref="VariableRowViewModel"/></param>
        public MappingRowViewModel(ParameterBase parameter, VariableRowViewModel variable)
        {
            this.Direction = MappingDirection.FromDstToHub;
            
            this.DstThing = new MappedThing() 
            {
                Name = variable.Name, 
                Value = variable.SelectedValues.Count > 1 ? $"[{variable.SelectedValues.Count}x2]" : variable.SelectedValues.FirstOrDefault()?.Value 
            };

            object value;

            var valueSet = parameter.QueryParameterBaseValueSet(variable.SelectedOption, variable.SelectedActualFiniteState);

            if (parameter.ParameterType is SampledFunctionParameterType)
            {
                var cols = parameter.ParameterType.NumberOfValues;
                value = $"[{valueSet.Computed.Count / cols}x{cols}]";
            }
            else
            {
                value = valueSet.Computed[0] ?? "-";
            }

            this.HubThing = new MappedThing()
            {
                Name = parameter.ModelCode(),
                Value = value
            };
        }
    }
}
