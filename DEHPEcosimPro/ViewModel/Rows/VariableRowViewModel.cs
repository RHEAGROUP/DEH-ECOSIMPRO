// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VariableRowViewModel.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2020 RHEA System S.A.
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;

    using CDP4Dal;

    using DEHPEcosimPro.Events;
    using DEHPEcosimPro.Views;

    using Opc.Ua;
    using Opc.Ua.Client;

    using ReactiveUI;

    /// <summary>
    /// The <see cref="VariableRowViewModel"/> represents on reference row into the <see cref="DstVariablesControl"/>
    /// </summary>
    public class VariableRowViewModel : ReactiveObject
    {
        /// <summary>
        /// The represented <see cref="ReferenceDescription"/>
        /// </summary>
        public readonly ReferenceDescription Reference;

        /// <summary>
        /// The represented <see cref="ReferenceDescription"/> corresponding <see cref="DataValue"/>
        /// </summary>
        private readonly DataValue data;

        /// <summary>
        /// Backing field for <see cref="Name"/>
        /// </summary>
        private string name;

        /// <summary>
        /// Gets the name of the represented reference
        /// </summary>
        public string Name
        {
            get => this.name;
            set => this.RaiseAndSetIfChanged(ref this.name, value);
        }

        /// <summary>
        /// Gets the values that the represented variable has held
        /// </summary>
        public List<(object Value, DateTime TimeStamp)> Values { get; } = new List<(object Value, DateTime TimeStamp)>();

        /// <summary>
        /// Backing field for <see cref="InitialValue"/>
        /// </summary>
        private object initialCurrentValue;

        /// <summary>
        /// Gets the initial value of the represented reference
        /// </summary>
        public object InitialValue
        {
            get => this.initialCurrentValue;
            set => this.RaiseAndSetIfChanged(ref this.initialCurrentValue, value);
        }

        /// <summary>
        /// Backing field for <see cref="ActualValue"/>
        /// </summary>
        private object actualValue;

        /// <summary>
        /// Gets the actual value of the represented reference
        /// </summary>
        public object ActualValue
        {
            get => this.actualValue;
            set => this.RaiseAndSetIfChanged(ref this.actualValue, value);
        }

        /// <summary>
        /// Backing field for <see cref="AverageValue"/>
        /// </summary>
        private object averageValue;

        /// <summary>
        /// Gets the average value of the represented reference
        /// </summary>
        public object AverageValue
        {
            get => this.averageValue;
            set => this.RaiseAndSetIfChanged(ref this.averageValue, value);
        }

        /// <summary>
        /// Backing field for <see cref="LastNotificationTime"/>
        /// </summary>
        private string lastNotificationTime;

        /// <summary>
        /// Gets the value of the represented reference
        /// </summary>
        public string LastNotificationTime
        {
            get => this.lastNotificationTime;
            set => this.RaiseAndSetIfChanged(ref this.lastNotificationTime, value);
        }

        /// <summary>
        /// Initializes a new <see cref="VariableRowViewModel"/>
        /// </summary>
        /// <param name="referenceDescriptionAndData">The represented <see cref="ReferenceDescription"/> and its <see cref="DataValue"/></param>
        public VariableRowViewModel((ReferenceDescription, DataValue) referenceDescriptionAndData)
        {
            var (referenceDescriptionValue, dataValue) = referenceDescriptionAndData;
            this.Reference = referenceDescriptionValue;
            this.data = dataValue;
            this.SetProperties();

            CDPMessageBus.Current.Listen<OpcVariableChangedEvent>()
                .Where(x => x.Id == this.Reference.NodeId.Identifier && x.TimeStamp > this.Values.Last().TimeStamp)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(this.OnNotification);
        }

        /// <summary>
        /// Sets the properties of this view model
        /// </summary>
        private void SetProperties()
        {
            this.Name = this.Reference.DisplayName.Text;

            if (this.data != null)
            {
                this.InitialValue = this.data.Value;
                this.ActualValue = this.data.Value;
                this.Values.Add((this.data.Value, this.data.ServerTimestamp));
            }
        }
        
        /// <summary>
        /// Occurs when the opc server sends an update when the represented variable reference has been subscribed to
        /// </summary>
        private void OnNotification(OpcVariableChangedEvent update)
        {
            this.Values.Add((update.Value, update.TimeStamp));
            this.ActualValue = update.Value;
            this.AverageValue = this.ComputeAverageValue();
        
            this.actualValue = update.Value;
            this.LastNotificationTime = update.TimeStamp.ToLongTimeString();
        }

        /// <summary>
            /// Computes the average value for this represented variable
            /// </summary>
            /// <returns>An <see cref="object"/> holding the average</returns>
        public object ComputeAverageValue()
        {
            var valuesInDouble = new List<double>();

            foreach (var value in this.Values.Select(x => x.Value))
            {
                if (double.TryParse(value.ToString(), out var valueInDouble))
                {
                    valuesInDouble.Add(valueInDouble);
                }
                else
                {
                    return "-";
                }
            }

            return valuesInDouble.Sum() / this.Values.Count;
        }
    }
}
