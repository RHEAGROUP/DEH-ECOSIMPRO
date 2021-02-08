// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubMappingConfigurationDialogViewModel.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.ViewModel.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Windows;
    using System.Windows.Input;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel.Dialogs.Interfaces;
    using DEHPEcosimPro.ViewModel.Rows;

    using DevExpress.Mvvm.Native;

    using ReactiveUI;

    /// <summary>
    /// The <see cref="HubMappingConfigurationDialogViewModel"/> is the view model to let the user configure the mapping to the Ecosim source
    /// </summary>
    public class HubMappingConfigurationDialogViewModel : MappingConfigurationDialogViewModel, IHubMappingConfigurationDialogViewModel
    {
        /// <summary>
        /// Gets or sets the collection of available variables
        /// </summary>
        public ReactiveList<VariableRowViewModel> AvailableVariables { get; set; }

        /// <summary>
        /// Gets or sets the collection of <see cref="ElementDefinitionRowViewModel"/> that hold parameter value to map
        /// </summary>
        public ReactiveList<ElementDefinitionRowViewModel> Elements { get; set; } = new ReactiveList<ElementDefinitionRowViewModel>();

        /// <summary>
        /// Gets or sets the collection of <see cref="ElementDefinition"/> that hold parameter value to map
        /// </summary>
        public ReactiveList<ElementDefinition> ElementDefinitions => 
            new ReactiveList<ElementDefinition>(this.Elements.Select(x => x.Thing));

        /// <summary>
        /// Gets or sets the collection of <see cref="ElementUsages"/> that hold parameter value to map
        /// </summary>
        public ReactiveList<ElementUsage> ElementUsages { get; set; } = new ReactiveList<ElementUsage>();

        /// <summary>
        /// Gets or sets the collection of <see cref="Parameter"/> that hold parameter value to map
        /// </summary>
        public ReactiveList<ParameterOrOverrideBase> Parameters { get; set; } = new ReactiveList<ParameterOrOverrideBase>();
        
        /// <summary>
        /// Gets or sets the collection of string value
        /// </summary>
        public ReactiveList<ValueSetValueRowViewModel> Values { get; set; } = new ReactiveList<ValueSetValueRowViewModel>();

        /// <summary>
        /// Gets the collection of <see cref="MappedElementDefinitionRowViewModel"/>
        /// </summary>
        public ReactiveList<MappedElementDefinitionRowViewModel> MappedElements { get; } = new ReactiveList<MappedElementDefinitionRowViewModel>();

        /// <summary>
        /// Backing field for <see cref="SelectedMappedElement"/>
        /// </summary>
        private MappedElementDefinitionRowViewModel selectedMappedElement;

        /// <summary>
        /// Gets or sets the selected <see cref="MappedElementDefinitionRowViewModel"/>
        /// </summary>
        public MappedElementDefinitionRowViewModel SelectedMappedElement
        {
            get => this.selectedMappedElement;
            set => this.RaiseAndSetIfChanged(ref this.selectedMappedElement, value);
        }

        /// <summary>
        /// Backing field for <see cref="SelectedThing"/>
        /// </summary>
        private object selectedThing;

        /// <summary>
        /// Gets or sets the selected <see cref="IRowViewModelBase{T}"/>
        /// </summary>
        public object SelectedThing
        {
            get => this.selectedThing;
            set => this.RaiseAndSetIfChanged(ref this.selectedThing, value);
        }

        /// <summary>
        /// Backing field for <see cref="CanContinue"/>
        /// </summary>
        private bool canContinue;

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="MappingConfigurationDialogViewModel.ContinueCommand"/> can execute
        /// </summary>
        public bool CanContinue
        {
            get => this.canContinue;
            set => this.RaiseAndSetIfChanged(ref this.canContinue, value);
        }

        /// <summary>
        /// Initializes a new <see cref="HubMappingConfigurationDialogViewModel"/>
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel"/></param>
        public HubMappingConfigurationDialogViewModel(IHubController hubController, 
            IDstController dstController, IStatusBarControlViewModel statusBar) :
                base(hubController, dstController, statusBar)
        {
            this.UpdateProperties();
            this.InitializesCommandsAndObservableSubscriptions();
        }

        /// <summary>
        /// Initializes this view model <see cref="ICommand"/> and <see cref="IObservable{T}"/>
        /// </summary>
        private void InitializesCommandsAndObservableSubscriptions()
        {
            this.ContinueCommand = ReactiveCommand.Create(
                this.WhenAnyValue(x => x.CanContinue),
                RxApp.MainThreadScheduler);

            this.ContinueCommand.Subscribe(_ => this.ExecuteContinueCommand(
                () =>
                {
                    var mappedElement = 
                        this.MappedElements.Where(x => x.IsValid).ToList();

                    this.DstController.Map(mappedElement);
                    this.StatusBar.Append($"Mapping in progress of {mappedElement.Count} value(s)...");
                }));

            this.WhenAnyValue(x => x.SelectedThing)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>  this.UpdateHubFields(this.SelectedThingChanged));

            this.WhenAnyValue(x => x.SelectedMappedElement)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(this.SelectedMappedThingChanged));

            this.WhenAnyValue(x => x.SelectedMappedElement.SelectedParameter)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(this.ComputesAvailableValues));

            this.WhenAnyValue(x => x.SelectedMappedElement.SelectedVariable)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(this.VerifyVariableIsWritable));
        }

        /// <summary>
        /// Verifies that the selected variable has write access
        /// </summary>
        private void VerifyVariableIsWritable()
        {
            if (!(this.SelectedMappedElement?.SelectedVariable is {} variable && !variable.HasWriteAccess.HasValue))
            {
                return;
            }

            variable.HasWriteAccess = this.DstController.IsVariableWritable(variable.Reference);
        }

        /// <summary>
        /// Invoked when the <see cref="SelectedMappedElement"/> has changed
        /// </summary>
        private void SelectedMappedThingChanged()
        {
            if (this.SelectedMappedElement is null)
            {
                return;
            }

            if (this.SelectedMappedElement.SelectedElementDefinition is { } elementDefinition)
            {
                this.ElementUsages.Clear();
                this.ElementUsages.AddRange(elementDefinition.ContainedElement);
                this.Parameters.Clear();
                this.Parameters.AddRange(elementDefinition.Parameter);
            }

            if (this.SelectedMappedElement.SelectedElementUsage is {} elementUsage)
            {
                this.Parameters.Clear();
                this.Parameters.AddRange(elementUsage.ParameterOverride);
                this.Parameters.AddRange(elementUsage.ElementDefinition.Parameter);
            }

            this.ComputesAvailableValues();
        }

        /// <summary>
        /// Invoked when the <see cref="SelectedThing"/> has changed
        /// </summary>
        private void SelectedThingChanged()
        {
            switch (this.SelectedThing)
            {
                default:
                    return;
                case IRowViewModelBase<ElementDefinition> elementDefinitionRow:
                    this.SetSelectedMappedElement(elementDefinitionRow.Thing);
                    break;
                case IRowViewModelBase<ElementUsage> elementUsageRow:
                    {
                        this.SetSelectedMappedElement(elementUsageRow.Thing.GetContainerOfType<ElementDefinition>());
                        this.SelectedMappedElement.SelectedElementUsage = elementUsageRow.Thing;
                        break;
                    }
                case IRowViewModelBase<ParameterOrOverrideBase> parameterOrOverrideRow:
                    {
                        this.SetSelectedMappedElement(parameterOrOverrideRow.Thing.GetContainerOfType<ElementDefinition>());
                        this.SelectedMappedElement.SelectedParameter = parameterOrOverrideRow.Thing;
                        this.SelectedMappedElement.SelectedElementUsage = parameterOrOverrideRow.Thing.GetContainerOfType<ElementUsage>();
                        this.ComputesAvailableValues();
                        break;
                    }
            }
        }

        /// <summary>
        /// Sets the available values to select
        /// </summary>
        private void ComputesAvailableValues()
        {
            this.Values.Clear();

            if (this.SelectedMappedElement?.SelectedParameter is null)
            {
                return;
            }

            var scale = this.SelectedMappedElement.SelectedParameter.Scale;

            this.Values.AddRange(this.SelectedMappedElement.SelectedParameter
                .ValueSets.SelectMany(x => this.ComputesValueRow(x, x.Computed, scale)));

            this.Values.AddRange(this.SelectedMappedElement.SelectedParameter
                .ValueSets.SelectMany(x => this.ComputesValueRow(x, x.Reference, scale)));

            this.Values.AddRange(this.SelectedMappedElement.SelectedParameter
                .ValueSets.SelectMany(x => this.ComputesValueRow(x, x.Manual, scale)));
        }

        /// <summary>
        /// Computes the value row of one value array i.e. <see cref="IValueSet.Computed"/>
        /// </summary>
        /// <param name="valueSet">The <see cref="IValueSet"/> container</param>
        /// <param name="values">The collection of values</param>
        /// <param name="scale"></param>
        /// <returns></returns>
        private IEnumerable<ValueSetValueRowViewModel> ComputesValueRow(IValueSet valueSet, IEnumerable<string> values, MeasurementScale scale)
        {
            return values.Select(value =>
                                        new ValueSetValueRowViewModel(value, valueSet.ActualOption, valueSet.ActualState, scale));
        }

        /// <summary>
        /// Sets the <see cref="SelectedMappedElement"/>
        /// </summary>
        /// <param name="element">The corresponding <see cref="ElementDefinition"/></param>
        private void SetSelectedMappedElement(ElementDefinition element)
        {
            this.SelectedMappedElement = this.MappedElements.FirstOrDefault(
                                             x => x.SelectedElementDefinition.Iid == element.Iid)
                                         ?? this.CreateNewMappedElement(element);
        }

        /// <summary>
        /// Creates a new <see cref="MappedElementDefinitionRowViewModel"/> and adds it to <see cref="MappedElements"/>
        /// </summary>
        /// <param name="thing">The base <see cref="Thing"/></param>
        /// <returns>A new <see cref="MappedElementDefinitionRowViewModel"/></returns>
        private MappedElementDefinitionRowViewModel CreateNewMappedElement(ElementDefinition thing)
        {
            var selectedElement = new MappedElementDefinitionRowViewModel()
            {
                SelectedElementDefinition = thing
            };

            selectedElement.WhenAnyValue(x => x.IsValid).Subscribe(_ => this.CheckCanExecute());
            this.MappedElements.Add(selectedElement);
            return selectedElement;
        }

        /// <summary>
        /// Checks that any of the <see cref="MappedElement"/> is <see cref="MappedElementDefinitionRowViewModel.IsValid"/>
        /// </summary>
        private void CheckCanExecute()
        {
            this.CanContinue = this.MappedElements.Any(x => x.IsValid);
        }

        /// <summary>
        /// Update this view model properties
        /// </summary>
        private void UpdateProperties()
        {
            this.AvailableVariables = new ReactiveList<VariableRowViewModel>(
                this.DstController.Variables.Select(r =>
                {
                    r.Node = this.DstController.ReadNode(r.Reference);
                    return new VariableRowViewModel(r, false);
                }));

            this.MappedElements.ChangeTrackingEnabled = true;
        }
    }
}
