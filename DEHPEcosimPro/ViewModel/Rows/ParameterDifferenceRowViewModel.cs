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
        public Parameter OldThing { get; set; }

        /// <summary>
        /// The thing from Ecosimpro
        /// </summary>
        public Parameter NewThing { get; set; }

        /// <summary>
        /// The value the data hub had
        /// </summary>
        private string oldValue;

        /// <summary>
        /// The value the data hub had
        /// </summary>
        public string OldValue
        {
            get => this.oldValue;
            set => this.RaiseAndSetIfChanged(ref this.oldValue, value);
        }

        /// <summary>
        /// The new value from Ecosimpro
        /// </summary>
        private string newValue;

        /// <summary>
        /// The new value from Ecosimpro
        /// </summary>
        public string NewValue
        {
            get => this.newValue;
            set => this.RaiseAndSetIfChanged(ref this.newValue, value);
        }

        /// <summary>
        /// Name of the Value
        /// </summary>
        private string name;

        /// <summary>
        /// Name of the Value
        /// </summary>
        public string Name
        {
            get => this.name;
            set => this.RaiseAndSetIfChanged(ref this.name, value);
        }

        /// <summary>
        /// Difference, positive or negative, of the two value <see cref="NewValue"/> and <see cref="OldValue"/>
        /// </summary>
        private string difference;

        /// <summary>
        /// Difference, positive or negative, of the two value <see cref="NewValue"/> and <see cref="OldValue"/>
        /// </summary>
        public string Difference
        {
            get => this.difference;
            set => this.RaiseAndSetIfChanged(ref this.difference, value);
        }

        /// <summary>
        /// Difference, positive or negative, of the two value <see cref="NewValue"/> and <see cref="OldValue"/>
        /// </summary>
        private string percentDiff;

        /// <summary>
        /// Difference, positive or negative, of the two value <see cref="NewValue"/> and <see cref="OldValue"/>
        /// </summary>
        public string PercentDiff
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
            this.Name = Name.ToString();
            this.OldValue = OldValue.ToString();
            this.NewValue = NewValue.ToString();
            this.Difference = Difference.ToString();
            this.PercentDiff = PercentDiff.ToString();
        }
    }
}
