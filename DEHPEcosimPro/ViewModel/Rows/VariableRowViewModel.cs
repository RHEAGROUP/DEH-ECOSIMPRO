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

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using CDP4Dal;

    using DEHPEcosimPro.Events;
    using DEHPEcosimPro.Views;

    using Opc.Ua;

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
        public ReactiveList<TimeTaggedValueRowViewModel> Values { get; } = new ReactiveList<TimeTaggedValueRowViewModel>();

        /// <summary>
        /// Gets the values that has been selected to map
        /// </summary>
        public ReactiveList<TimeTaggedValueRowViewModel> SelectedValues { get; set; } = new ReactiveList<TimeTaggedValueRowViewModel>();

        /// <summary>
        /// Gets or sets the collection of value collection to display in the chart view
        /// </summary>
        public ReactiveList<object> ChartValues { get; private set; }

        /// <summary>
        /// Backing field for <see cref="InitialValue"/>
        /// </summary>
        private object initialValue;

        /// <summary>
        /// Gets the initial value of the represented reference
        /// </summary>
        public object InitialValue
        {
            get => this.initialValue;
            set => this.RaiseAndSetIfChanged(ref this.initialValue, value);
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
        /// Backing field for <see cref="SelectedOption"/>
        /// </summary>
        private Option selectedOption;

        /// <summary>
        /// Gets or sets the selected <see cref="Option"/>
        /// </summary>
        public Option SelectedOption
        {
            get => this.selectedOption;
            set => this.RaiseAndSetIfChanged(ref this.selectedOption, value);
        }

        /// <summary>
        /// Backing field for <see cref="SelectedParameter"/>
        /// </summary>
        private Parameter selectedParameter;

        /// <summary>
        /// Gets or sets the selected <see cref="Parameter"/>
        /// </summary>
        public Parameter SelectedParameter
        {
            get => this.selectedParameter;
            set => this.RaiseAndSetIfChanged(ref this.selectedParameter, value);
        }

        /// <summary>
        /// Backing field for <see cref="SelectedParameterType"/>
        /// </summary>
        private ParameterType selectedParameterType;

        /// <summary>
        /// Gets or sets the selected <see cref="Parameter"/>
        /// </summary>
        public ParameterType SelectedParameterType
        {
            get => this.selectedParameterType;
            set => this.RaiseAndSetIfChanged(ref this.selectedParameterType, value);
        }

        /// <summary>
        /// Backing field for <see cref="SelectedElementDefinition"/>
        /// </summary>
        private ElementDefinition selectedElementDefinition;

        /// <summary>
        /// Gets or sets the selected <see cref="ElementDefinition"/>
        /// </summary>
        public ElementDefinition SelectedElementDefinition
        {
            get => this.selectedElementDefinition;
            set => this.RaiseAndSetIfChanged(ref this.selectedElementDefinition, value);
        }

        /// <summary>
        /// Backing field for <see cref="SelectedActualFiniteState"/>
        /// </summary>
        private ActualFiniteState selectedActualFiniteState;

        /// <summary>
        /// Gets or sets the selected <see cref="ActualFiniteState"/>
        /// </summary>
        public ActualFiniteState SelectedActualFiniteState
        {
            get => this.selectedActualFiniteState;
            set => this.RaiseAndSetIfChanged(ref this.selectedActualFiniteState, value);
        }

        /// <summary>
        /// Gets or sets the collection of selected <see cref="ElementUsage"/>s
        /// </summary>
        public ReactiveList<ElementUsage> SelectedElementUsages { get; set; } = new ReactiveList<ElementUsage>();

        /// <summary>
        /// Gets or sets the mapping configurations
        /// </summary>
        public ReactiveList<IdCorrespondence> MappingConfigurations { get; set; } = new ReactiveList<IdCorrespondence>();

        /// <summary>
        /// Gets this represented ElementName
        /// </summary>
        public string ElementName => this.Name.Split('.')[0];

        /// <summary>
        /// Gets this reprensented ParameterName
        /// </summary>
        public string ParameterName => string.Join(".", this.Name.Split('.').Skip(1));
        
        /// <summary>
        /// Backing field fopr <see cref="IsValid"/>
        /// </summary>
        private bool isValid;

        /// <summary>
        /// Gets a value indicating whether this <see cref="VariableRowViewModel"/> is ready to be mapped
        /// </summary>
        public bool IsValid
        {
            get => this.isValid;
            set => this.RaiseAndSetIfChanged(ref this.isValid, value);
        }

        /// <summary>
        /// Backing field fopr <see cref="HasWriteAccess"/>
        /// </summary>
        private bool? hasWriteAccess;

        /// <summary>
        /// Gets a value indicating whether this <see cref="Reference"/> has write access
        /// </summary>
        public bool? HasWriteAccess
        {
            get => this.hasWriteAccess;
            set => this.RaiseAndSetIfChanged(ref this.hasWriteAccess, value);
        }

        /// <summary>
        /// Backing field for <see cref="IsHighlighted"/>
        /// </summary>
        private bool isHiglighted;

        /// <summary>
        /// Gets or sets a value indicating whether this row is highlighted
        /// </summary>
        public bool IsHighlighted
        {
            get => this.isHiglighted;
            set => this.RaiseAndSetIfChanged(ref this.isHiglighted, value);
        }

        /// <summary>
        /// A value indicating whether this view model should subscribe for <see cref="OpcVariableChangedEvent"/>
        /// </summary>
        public bool ShouldListenToChangeMessage { get; set; }
        
        /// <summary>
        /// Initializes a new <see cref="VariableRowViewModel"/>
        /// </summary>
        /// <param name="referenceDescriptionAndData">The represented <see cref="ReferenceDescription"/> and its <see cref="DataValue"/></param>
        /// <param name="shouldListenToChangeMessage">A value indicating whether this view model should subscribe for <see cref="OpcVariableChangedEvent"/></param>
        public VariableRowViewModel((ReferenceDescription, DataValue) referenceDescriptionAndData, bool shouldListenToChangeMessage = true)
        {
            var (referenceDescriptionValue, dataValue) = referenceDescriptionAndData;
            this.Reference = referenceDescriptionValue;
            this.data = dataValue;
            this.ShouldListenToChangeMessage = shouldListenToChangeMessage;
            this.SetProperties();

            CDPMessageBus.Current.Listen<OpcVariableChangedEvent>()
                .Where(x => this.ShouldListenToChangeMessage && x.Id == this.Reference.NodeId.Identifier && x.TimeStamp > this.Values.Last().TimeStamp)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(this.OnNotification);

            CDPMessageBus.Current.Listen<DstHighlightEvent>()
                .Where(x => x.TargetThingId == this.Reference.NodeId.Identifier)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => this.IsHighlighted = x.ShouldHighlight);
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
                this.Values.Add(new TimeTaggedValueRowViewModel(this.data.Value, this.data.ServerTimestamp));
            }
        }
        
        /// <summary>
        /// Occurs when the opc server sends an update when the represented variable reference has been subscribed to
        /// </summary>
        private void OnNotification(OpcVariableChangedEvent update)
        {
            this.UpdateValueCollection(update.Value, update.TimeStamp);

            this.ActualValue = update.Value;
            this.AverageValue = this.ComputeAverageValue();

            this.actualValue = update.Value;
            this.LastNotificationTime = update.TimeStamp.ToString("yy-MM-dd HH:mm:ss.fffffff");
        }

        /// <summary>
        /// Updates the <see cref="Values"/> collection
        /// </summary>
        /// <param name="value">The value to add</param>
        /// <param name="timeStamp">The <see cref="DateTime"/> time stamp associated with the <paramref name="value"/></param>
        private void UpdateValueCollection(object value, DateTime timeStamp)
        {
            if (!this.Values.Any(x => x.Value == value && x.TimeStamp == timeStamp))
            {
                this.Values.Add(new TimeTaggedValueRowViewModel(value, timeStamp));
            }
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

        /// <summary>
        /// Updates the <see cref="ChartValues"/> properties
        /// </summary>
        public void SetChartValues()
        {
            this.ChartValues = new ReactiveList<object>(new List<object>()
            {
                new { this.Name, this.Values }
            });
        }
    }
}
