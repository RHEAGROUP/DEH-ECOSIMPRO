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
    using System;
    using System.Linq;

    using DEHPEcosimPro.DstController;

    using ReactiveUI;

    /// <summary>
    /// TODO
    /// </summary>
    public class ParameterDifferenceRowViewModel : ReactiveObject
    {

        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The Thing already on the data hub
        /// </summary>
        public Parameter OldThing;
        /// <summary>
        /// The thing from Ecosimpro
        /// </summary>
        public Parameter NewThing;

        /// <summary>
        /// The value the data hud had, string
        /// </summary>
        private object oldValue;

        /// <summary>
        /// The value the data hud had, string
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
        /// 
        /// </summary>
        protected object actualFiniteStateName;

        /// <summary>
        /// 
        /// </summary>
        public object ActualFiniteStateName
        {
            get => this.actualFiniteStateName;
            set => this.RaiseAndSetIfChanged(ref this.actualFiniteStateName, value);
        }

        /// <summary>
        /// 
        /// </summary>
        protected object optionName;

        /// <summary>
        /// 
        /// </summary>
        public object OptionName
        {
            get => this.optionName;
            set => this.RaiseAndSetIfChanged(ref this.optionName, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:DEHPEcosimPro.ViewModel.Rows.ParameterDifferenceRowViewModel" /> class.
        /// </summary>
        /// <param name="OldThing"><see cref="Parameter"/></param>
        /// <param name="NewThing"><see cref="Parameter"/></param>
        public ParameterDifferenceRowViewModel(Parameter OldThing, Parameter NewThing, IDstController dstController)
        {
            this.dstController = dstController;

            this.OldThing = OldThing;
            this.NewThing = NewThing;

            this.ComputeParametersValues(NewThing, OldThing);

            this.Name = this.NewThing.ParameterType.Name;
            this.Difference = this.CalculateDiff();

        }

        /// <summary>
        /// Calculate the difference between the old and new value, if possible
        /// </summary>
        /// <returns>a number, positive or negative (with + or - sign)</returns>
        private string CalculateDiff()
        {
            string result = "0";

            var isOldValueDecimal = decimal.TryParse(this.OldValue.ToString(), out decimal decimalOldValue);
            var isNewValueDecimal = decimal.TryParse(this.NewValue.ToString(), out decimal decimalNewValue);

            if (isOldValueDecimal && isNewValueDecimal)
            {
                var diff = decimalNewValue - decimalOldValue;
                var sign = Math.Sign(diff);
                var abs = Math.Abs(decimalNewValue - decimalOldValue);

                if (sign > 0)
                {
                    result = $"+{abs}";
                }
                else if (sign < 0)
                {
                    result = $"-{abs}";
                }
            }

            return result;
        }

        /// <summary>
        /// Get the real values for <see cref="OldValue"/> and <see cref="NewValue"/>
        /// </summary>
        /// <param name="newParameterOrOverride"><see cref="ParameterOrOverrideBase"/></param>
        /// <param name="oldParameterOrOverride"><see cref="ParameterOrOverrideBase"/></param>
        public void ComputeParametersValues(ParameterOrOverrideBase newParameterOrOverride, ParameterOrOverrideBase oldParameterOrOverride)
        {
            var variableRowViewModel = this.dstController.ParameterVariable
                .FirstOrDefault(x => x.Key.Iid == newParameterOrOverride.Iid
                                     && x.Key.ParameterType.Iid == newParameterOrOverride.ParameterType.Iid).Value;

            var (option, actualFiniteState) = newParameterOrOverride.IsOptionDependent switch
            {
                false when newParameterOrOverride.StateDependence == null => (null, null),
                true when newParameterOrOverride.StateDependence == null => (variableRowViewModel.SelectedOption, null),
                false when newParameterOrOverride.StateDependence != null => (null, variableRowViewModel.SelectedActualFiniteState),
                true when newParameterOrOverride.StateDependence != null => (variableRowViewModel.SelectedOption, variableRowViewModel.SelectedActualFiniteState),
                _ => default((Option option, ActualFiniteState actualFiniteState))
            };

            this.ActualFiniteStateName = variableRowViewModel?.SelectedActualFiniteState?.Name ?? null;
            this.OptionName = variableRowViewModel?.SelectedOption?.Name ?? null;

            this.NewValue = newParameterOrOverride.QueryParameterBaseValueSet(option, actualFiniteState).ActualValue.FirstOrDefault();
            this.OldValue = oldParameterOrOverride.QueryParameterBaseValueSet(option, actualFiniteState).ActualValue.FirstOrDefault();
        }
    }
}
