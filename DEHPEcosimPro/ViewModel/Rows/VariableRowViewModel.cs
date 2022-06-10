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
        /// The represented <see cref="ReferenceDescription" />
        /// </summary>
        public new readonly ReferenceDescription Reference;

        /// <summary>
        /// Backing field for <see cref="IsHighlighted" />
        /// </summary>
        private bool isHiglighted;

        /// <summary>
        /// Backing field for <see cref="SelectedTimeStep" />
        /// </summary>
        private double selectedTimeStep;

        /// <summary>
        /// Backing field for <see cref="IsAveraged" />
        /// </summary>
        private bool isAveraged;

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
        public VariableRowViewModel((ReferenceDescription, DataValue) referenceDescriptionAndData, bool shouldListenToChangeMessage = true)
            : base(referenceDescriptionAndData, shouldListenToChangeMessage)
        {
            var (referenceDescriptionValue, dataValue) = referenceDescriptionAndData;
            this.Reference = referenceDescriptionValue;
            this.Data = dataValue;
            this.SetProperties();

            this.Disposables.Add(CDPMessageBus.Current.Listen<DstHighlightEvent>()
                .Where(x => x.TargetThingId == this.Reference.NodeId.Identifier)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => this.IsHighlighted = x.ShouldHighlight));
        }

        /// <summary>
        /// Initializes a new <see cref="VariableRowViewModel" />
        /// </summary>
        /// <param name="variable">The <see cref="VariableBaseRowViewModel" /> to clone</param>
        public VariableRowViewModel(VariableBaseRowViewModel variable) : 
            this((variable.Reference, variable.Data), false)
        {
        }

        /// <summary>
        /// Gets the index of the represented variable if it is part of an array
        /// </summary>
        public List<int> Index { get; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether this row is highlighted
        /// </summary>
        public bool IsHighlighted
        {
            get { return this.isHiglighted; }
            set { this.RaiseAndSetIfChanged(ref this.isHiglighted, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this row is averaged over timestep
        /// </summary>
        public bool IsAveraged
        {
            get { return this.isAveraged; }
            set { this.RaiseAndSetIfChanged(ref this.isAveraged, value); }
        }

        /// <summary>
        /// Gets or sets the timeStep step value
        /// </summary>
        public double SelectedTimeStep
        {
            get { return this.selectedTimeStep; }
            set { this.RaiseAndSetIfChanged(ref this.selectedTimeStep, value); }
        }

        /// <summary>
        /// Gets or sets the collection of value collection to display in the chart view
        /// </summary>
        public ReactiveList<object> ChartValues { get; private set; }

        /// <summary>
        /// Updates the <see cref="SelectedValues"/> based on <see cref="SelectedTimeStep" />
        /// and <see cref="SelectedTimeStep" />
        /// </summary>
        public void ApplyTimeStep()
        {
            this.SelectedValues.Clear();
            this.ClearAverages();

            if (this.SelectedTimeStep is 0)
            {
                this.SelectedValues.AddRange(this.Values);
                return;
            }

            var firstValue = this.Values.FirstOrDefault();

            if (firstValue == null)
            {
                return;
            }

            var currentTimestep = firstValue.TimeStep;

            this.SelectedValues.Add(firstValue);

            var averagingList = new List<double>();

            foreach (var timeTaggedValueRowViewModel in this.Values)
            {
                if (this.IsAveraged)
                {
                    if (timeTaggedValueRowViewModel.Value is IConvertible convert)
                    {
                        averagingList.Add(convert.ToDouble(null));
                    }
                }

                var lastValuePlusTimeStep = currentTimestep + this.SelectedTimeStep;

                if (Math.Round(Math.Abs(timeTaggedValueRowViewModel.TimeStep), 3) >= Math.Round(Math.Abs(lastValuePlusTimeStep), 3))
                {
                    if (this.IsAveraged)
                    {
                        var lastSelectedRow = this.SelectedValues.LastOrDefault();

                        if (lastSelectedRow != null)
                        {
                            averagingList.RemoveAt(averagingList.Count - 1);
                            lastSelectedRow.AveragedValue = averagingList.Average();
                        }

                        averagingList.Clear();

                        if (timeTaggedValueRowViewModel.Value is IConvertible convert)
                        {
                            averagingList.Add(convert.ToDouble(null));
                        }
                    }

                    this.SelectedValues.Add(timeTaggedValueRowViewModel);
                    currentTimestep = timeTaggedValueRowViewModel.TimeStep;
                }
            }

            // come back for the last added row
            if (this.IsAveraged)
            {
                var lastSelectedRow = this.SelectedValues.LastOrDefault();

                if (lastSelectedRow != null)
                {
                    lastSelectedRow.AveragedValue = averagingList.Average();
                }
            }
        }

        /// <summary>
        /// Clears the averaged value of every row
        /// </summary>
        public void ClearAverages()
        {
            foreach (var timeTaggedValueRowViewModel in this.Values)
            {
                timeTaggedValueRowViewModel.AveragedValue = null;
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

            if (this.Data != null)
            {
                this.InitialValue = this.Data.Value;
                this.ActualValue = this.Data.Value;
            }
        }

        /// <summary>
        /// Sets the <see cref="Name"/> of the represented variable to reflect it's own indice in the array it belongs to,
        /// Also sets the <see cref="Index"/>
        /// </summary>
        public void SetsArrayItemProperties()
        {
            if (!this.Name.Contains('['))
            {
                return;
            }

            foreach (var x in this.Name.Split('[', ']', ',')
                .Skip(1)
                .Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                if (int.TryParse(x, out var number))
                {
                    this.Index.Add(number);
                }
            }

            this.Name = "[" + string.Join("x", this.Index) + "]";
        }
    }
}
