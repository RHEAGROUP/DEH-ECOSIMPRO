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
    using System.Collections.Generic;
    using System.Linq;

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
        /// Backing field for <see cref="Id"/>
        /// </summary>
        private string id;

        /// <summary>
        /// Gets the unique identifier of the represented reference
        /// </summary>
        public string Id
        {
            get => this.id;
            set => this.RaiseAndSetIfChanged(ref this.id, value);
        }
        
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
        public List<object> Values { get; } = new List<object>();

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
        /// Backing field for <see cref="NodeType"/>
        /// </summary>
        private string nodeType;

        /// <summary>
        /// Gets the value of the represented reference
        /// </summary>
        public string NodeType
        {
            get => this.nodeType;
            set => this.RaiseAndSetIfChanged(ref this.nodeType, value);
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
                this.Values.Add(this.data.Value);
            }

            this.Id = this.Reference.NodeId.Identifier.ToString();
            this.NodeType = this.Reference.NodeClass.ToString();
        }

        /// <summary>
        /// Occurs when the opc server gets an update when the represented variable reference has been subscribed to
        /// </summary>
        /// <param name="monitoredItem">The <see cref="MonitoredItem"/></param>
        /// <param name="e">The <see cref="MonitoredItemNotificationEventArgs"/></param>
        public void OnNotification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            var value = (monitoredItem.LastValue as MonitoredItemNotification)?.Value;
            var notificationTime = monitoredItem.Subscription.LastNotificationTime.ToString("T");

            if (notificationTime != this.LastNotificationTime)
            {
                this.Values.Add(value);
                this.ActualValue = value;
                this.AverageValue = this.ComputeAverageValue();
            }

            this.actualValue = value;
            this.LastNotificationTime = this.lastNotificationTime;
        }

        /// <summary>
        /// Computes the average value for this represented variable
        /// </summary>
        /// <returns>An <see cref="object"/> holding the average</returns>
        public object ComputeAverageValue()
        {
            if (this.Values.All(x => x switch
            {
                sbyte _ => true,
                byte _ => true,
                short _ => true,
                ushort _ => true,
                int _ => true,
                uint _ => true,
                long _ => true,
                ulong _ => true,
                float _ => true,
                double _ => true,
                decimal _ => true,
                _ => false
            }))
            {
                return this.Values.Cast<double>().Sum() / this.Values.Count;
            }

            return "-";
        }
    }
}
