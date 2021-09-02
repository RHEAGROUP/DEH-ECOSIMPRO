// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ParameterDifferenceRowViewModel.cs" company="RHEA System S.A.">
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
    using CDP4Common.EngineeringModelData;

    using ReactiveUI;

    /// <summary>
    /// Object to display on MainWindow, Value Diff
    /// </summary>
    public class ParameterDifferenceRowViewModel : ReactiveObject
    {
        /// <summary>
        /// The Thing already on the data hub
        /// </summary>
        private Parameter oldThing;

        /// <summary>
        /// The Thing already on the data hub
        /// </summary>
        public Parameter OldThing
        {
            get => this.oldThing;
            set => this.oldThing = value;
        }

        /// <summary>
        /// The thing from Ecosimpro
        /// </summary>
        private Parameter newThing;

        /// <summary>
        /// The thing from Ecosimpro
        /// </summary>
        public Parameter NewThing
        {
            get => this.newThing;
            set => this.newThing = value;
        }

        /// <summary>
        /// The value the data hub had, string
        /// </summary>
        private object oldValue;

        /// <summary>
        /// The value the data hub had, string
        /// </summary>
        public object OldValue
        {
            get => this.oldValue;
            set => this.RaiseAndSetIfChanged(ref this.oldValue, value);
        }

        /// <summary>
        /// The new value from Ecosimpro, string
        /// </summary>
        private object newValue;

        /// <summary>
        /// The new value from Ecosimpro, string
        /// </summary>
        public object NewValue
        {
            get => this.newValue;
            set => this.RaiseAndSetIfChanged(ref this.newValue, value);
        }

        /// <summary>
        /// Name of the Value, string
        /// </summary>
        private object name;

        /// <summary>
        /// Name of the Value, string
        /// </summary>
        public object Name
        {
            get => this.name;
            set => this.RaiseAndSetIfChanged(ref this.name, value);
        }

        /// <summary>
        /// Difference, positive or negative, of the two value <see cref="NewValue"/> and <see cref="OldValue"/>, string
        /// </summary>
        private object difference;

        /// <summary>
        /// Difference, positive or negative, of the two value <see cref="NewValue"/> and <see cref="OldValue"/>, string
        /// </summary>
        public object Difference
        {
            get => this.difference;
            set => this.RaiseAndSetIfChanged(ref this.difference, value);
        }

        /// <summary>
        /// Difference, positive or negative, of the two value <see cref="NewValue"/> and <see cref="OldValue"/>, string
        /// </summary>
        private object percentDiff;

        /// <summary>
        /// Difference, positive or negative, of the two value <see cref="NewValue"/> and <see cref="OldValue"/>, string
        /// </summary>
        public object PercentDiff
        {
            get => this.percentDiff;
            set => this.RaiseAndSetIfChanged(ref this.percentDiff, value);
        }
        
        /// <summary>
        /// Object to display on MainWindow, Value Diff
        /// </summary>
        /// <param name="OldThing"><see cref="OldThing"/></param>
        /// <param name="NewThing"><see cref="NewThing"/></param>
        /// <param name="Name">Name of the data, with options aand/or states if applicable</param>
        /// <param name="OldValue">number or dataset</param>
        /// <param name="NewValue">number or dataset</param>
        /// <param name="Difference">number, positive or negative</param>
        /// <param name="PercentDiff">percentage, positive or negative</param>
        public ParameterDifferenceRowViewModel(Parameter OldThing, Parameter NewThing, object Name, object OldValue, object NewValue, object Difference, object PercentDiff)
        {
            this.OldThing = OldThing;
            this.NewThing = NewThing;
            this.Name = Name;
            this.OldValue = OldValue;
            this.NewValue = NewValue;
            this.Difference = Difference;
            this.PercentDiff = PercentDiff;
        }
        
    }
}
