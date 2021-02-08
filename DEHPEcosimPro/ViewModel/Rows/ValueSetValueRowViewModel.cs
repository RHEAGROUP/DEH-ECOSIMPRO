// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ValueSetValueRowViewModel.cs" company="RHEA System S.A.">
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
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using ReactiveUI;

    /// <summary>
    /// The <see cref="ValueSetValueRowViewModel"/> represents a single value from a <see cref="IValueSet"/>
    /// </summary>
    public class ValueSetValueRowViewModel : ReactiveObject
    {
        /// <summary>
        /// Backing field for <see cref="Value"/>
        /// </summary>
        private string value;

        /// <summary>
        /// gets or sets the represented value
        /// </summary>
        public string Value
        {
            get => this.value;
            set => this.RaiseAndSetIfChanged(ref this.value, value);
        }

        /// <summary>
        /// Backing field for <see cref="Option"/>
        /// </summary>
        private Option option;

        /// <summary>
        /// Gets or sets the option that this represented value depends on
        /// </summary>
        public Option Option
        {
            get => this.option;
            set => this.RaiseAndSetIfChanged(ref this.option, value);
        }

        /// <summary>
        /// Backing field for <see cref="ActualState"/>
        /// </summary>
        private ActualFiniteState actualState;

        /// <summary>
        /// gets or sets the represented value
        /// </summary>
        public ActualFiniteState ActualState
        {
            get => this.actualState;
            set => this.RaiseAndSetIfChanged(ref this.actualState, value);
        }

        /// <summary>
        /// Backing field for <see cref="Scale"/>
        /// </summary>
        private MeasurementScale scale;

        /// <summary>
        /// Gets the string associated <see cref="MeasurementScale"/> of this value
        /// </summary>
        public MeasurementScale Scale
        {
            get => this.scale;
            set => this.RaiseAndSetIfChanged(ref this.scale, value);
        }

        /// <summary>
        /// Backing field for <see cref="Representation"/>
        /// </summary>
        private string representation;

        /// <summary>
        /// Gets the string representation of this value
        /// </summary>
        public string Representation
        {
            get => this.representation;
            set => this.RaiseAndSetIfChanged(ref this.representation, value);
        }

        /// <summary>
        /// Initializes a new <see cref="ValueSetValueRowViewModel"/>
        /// </summary>
        /// <param name="value">The value</param>
        /// <param name="option">The <see cref="Option"/> that this represented value depends on</param>
        /// <param name="state">The <see cref="ActualFiniteState"/> that this represented value depends on</param>
        public ValueSetValueRowViewModel(string value, Option option, ActualFiniteState state, MeasurementScale scale)
        {
            this.Value = value;
            this.Option = option;
            this.ActualState = state;
            this.Scale = scale;
            this.Representation = $"{(this.Option is null ? string.Empty : $" Option: {this.Option.Name}")}" +
                                  $"{(this.ActualState is null ? string.Empty : $" State: {this.ActualState.Name} ")}" +
                                  $"{this.Value} [{(this.Scale is null ? "-" : this.Scale.ShortName)}]";
        }
    }
}
