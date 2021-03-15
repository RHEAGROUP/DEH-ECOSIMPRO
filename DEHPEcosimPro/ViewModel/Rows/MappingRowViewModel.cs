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
        /// <summary>
        /// Gets or sets the hub <see cref="MappedThing"/>
        /// </summary>
        public MappedThing HubThing { get; set; }
        
        /// <summary>
        /// Gets or sets the dst <see cref="MappedThing"/>
        /// </summary>
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
        /// Backing field for <see cref="ArrowDirection"/>
        /// </summary>
        private double arrowDirection;

        /// <summary>
        /// Gets or sets the value
        /// </summary>
        public double ArrowDirection
        {
            get => this.arrowDirection;
            set => this.RaiseAndSetIfChanged(ref this.arrowDirection, value);
        }

        /// <summary>
        /// Initializes a new <see cref="MappingRowViewModel"/> from a <see cref="MappedElementDefinitionRowViewModel"/>
        /// </summary>
        /// <param name="currentMappingDirection">The current <see cref="MappingDirection"/></param>
        /// <param name="mappedElement">The <see cref="MappedElementDefinitionRowViewModel"/></param>
        public MappingRowViewModel(MappingDirection currentMappingDirection, MappedElementDefinitionRowViewModel mappedElement)
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

            this.UpdateDirection(currentMappingDirection);
        }

        /// <summary>
        /// Initializes a new <see cref="MappingRowViewModel"/> from a mapped <see cref="ParameterOrOverrideBase"/> and <see cref="VariableRowViewModel"/>
        /// </summary>
        /// <param name="currentMappingDirection">The current <see cref="MappingDirection"/></param>
        /// <param name="parameterVariable">The (<see cref="ParameterOrOverrideBase"/>, <see cref="VariableRowViewModel"/>)</param>
        public MappingRowViewModel(MappingDirection currentMappingDirection, (ParameterBase parameter, VariableRowViewModel variable) parameterVariable)
        {
            var (parameter, variable) = parameterVariable;

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

            this.UpdateDirection(currentMappingDirection);
        }

        /// <summary>
        /// Updates the arrow angle factor <see cref="ArrowDirection"/>, and the <see cref="HubThing"/> and the <see cref="DstThing"/> <see cref="MappedThing.GridColumnIndex"/>
        /// </summary>
        /// <param name="actualMappingDirection">The actual <see cref="MappingDirection"/></param>
        public void UpdateDirection(MappingDirection actualMappingDirection)
        {
            switch (this.Direction)
            {
                case MappingDirection.FromDstToHub when actualMappingDirection is MappingDirection.FromDstToHub:
                    this.HubThing.GridColumnIndex = 2;
                    this.DstThing.GridColumnIndex = 0;
                    this.ArrowDirection = 0;
                    break;
                case MappingDirection.FromDstToHub when actualMappingDirection is MappingDirection.FromHubToDst:
                    this.HubThing.GridColumnIndex = 0;
                    this.DstThing.GridColumnIndex = 2;
                    this.ArrowDirection = 180;
                    break;
                case MappingDirection.FromHubToDst when actualMappingDirection is MappingDirection.FromHubToDst:
                    this.HubThing.GridColumnIndex = 0;
                    this.DstThing.GridColumnIndex = 2;
                    this.ArrowDirection = 0;
                    break;
                case MappingDirection.FromHubToDst when actualMappingDirection is MappingDirection.FromDstToHub:
                    this.HubThing.GridColumnIndex = 2;
                    this.DstThing.GridColumnIndex = 0;
                    this.ArrowDirection = 180;
                    break;
            }
        }
    }
}
