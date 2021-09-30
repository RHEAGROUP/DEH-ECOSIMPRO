// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VariableRowViewModel.cs" company="RHEA System S.A.">
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
    using System.Reactive.Linq;

    using CDP4Dal;

    using DEHPEcosimPro.Events;

    using Opc.Ua;

    using ReactiveUI;

    /// <summary>
    /// The <see cref="VariableRowViewModel"/> represents on reference row into the <see cref="DstVariablesControl"/>
    /// </summary>
    public class VariableRowViewModel : VariableBaseRowViewModel
    {
        /// <summary>
        /// The represented <see cref="ReferenceDescription" /> corresponding <see cref="DataValue" />
        /// </summary>
        private readonly DataValue data;

        /// <summary>
        /// The represented <see cref="ReferenceDescription" />
        /// </summary>
        public readonly ReferenceDescription Reference;

        /// <summary>
        /// Backing field for <see cref="Name" />
        /// </summary>
        private string index;

        /// <summary>
        /// Backing field for <see cref="IsHighlighted" />
        /// </summary>
        private bool isHiglighted;

        /// <summary>
        /// Backing field for <see cref="Name" />
        /// </summary>
        private string name;

        /// <summary>
        /// Backing field for <see cref="SelectedTimeStep" />
        /// </summary>
        private double selectedTimeStep;

        /// <summary>
        /// Initializes a new <see cref="VariableRowViewModel"/>
        /// </summary>
        public VariableRowViewModel()
        {
        }

        /// <summary>
        /// Initializes a new <see cref="VariableRowViewModel"/>
        /// </summary>
        /// <param name="referenceDescriptionAndData">The represented <see cref="ReferenceDescription"/> and its <see cref="DataValue"/></param>
        /// <param name="shouldListenToChangeMessage">A value indicating whether this view model should subscribe for <see cref="OpcVariableChangedEvent"/></param>
        public VariableRowViewModel((ReferenceDescription, DataValue) referenceDescriptionAndData, bool shouldListenToChangeMessage = true) : base(referenceDescriptionAndData, shouldListenToChangeMessage)
        {
            var (referenceDescriptionValue, dataValue) = referenceDescriptionAndData;
            this.Reference = referenceDescriptionValue;
            this.data = dataValue;
            this.SetProperties();

            CDPMessageBus.Current.Listen<DstHighlightEvent>()
                .Where(x => x.TargetThingId == this.Reference.NodeId.Identifier)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => this.IsHighlighted = x.ShouldHighlight);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this row is highlighted
        /// </summary>
        public bool IsHighlighted
        {
            get => this.isHiglighted;
            set => this.RaiseAndSetIfChanged(ref this.isHiglighted, value);
        }

        /// <summary>
        /// Gets or sets the timeStep step value
        /// </summary>
        public double SelectedTimeStep
        {
            get => this.selectedTimeStep;
            set => this.RaiseAndSetIfChanged(ref this.selectedTimeStep, value);
        }

        /// <summary>
        /// Gets the name of the represented reference
        /// </summary>
        public override string Name
        {
            get => this.name;
            set => this.RaiseAndSetIfChanged(ref this.name, value);
        }

        /// <summary>
        /// Gets the name of the represented reference
        /// </summary>
        public string Index
        {
            get => this.index;
            set => this.RaiseAndSetIfChanged(ref this.index, value);
        }

        /// <summary>
        /// Gets or sets the collection of value collection to display in the chart view
        /// </summary>
        public ReactiveList<object> ChartValues { get; private set; }

        /// <summary>
        /// Updates the <see cref="SelectedValues" /> based on <see cref="SelectedTimeStep" />
        /// and <see cref="SelectedTimeStep" />
        /// </summary>
        public void ApplyTimeStep()
        {
            this.SelectedValues.Clear();

            if (this.SelectedTimeStep is 0)
            {
                this.SelectedValues.AddRange(this.Values);
                return;
            }

            var lastValue = .0;

            this.SelectedValues.Add(this.Values.FirstOrDefault());

            foreach (var timeTaggedValueRowViewModel in this.Values)
            {
                var lastValuePlusTimeStep = lastValue + this.SelectedTimeStep;

                if (Math.Abs(timeTaggedValueRowViewModel.TimeStep) >= Math.Abs(lastValuePlusTimeStep))
                {
                    this.SelectedValues.Add(timeTaggedValueRowViewModel);
                    lastValue = timeTaggedValueRowViewModel.TimeStep;
                }
            }
        }

        /// <summary>
        /// Updates the <see cref="ChartValues" /> properties
        /// </summary>
        public void SetChartValues()
        {
            this.ChartValues = new ReactiveList<object>(new List<object>
            {
                new { this.Name, this.Values }
            });
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
            }
        }
    }
}
