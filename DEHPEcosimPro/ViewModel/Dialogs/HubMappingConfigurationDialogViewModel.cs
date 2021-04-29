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
    using System.Windows.Input;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Validation;

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Services.TypeResolver.Interfaces;
    using DEHPEcosimPro.ViewModel.Dialogs.Interfaces;
    using DEHPEcosimPro.ViewModel.Rows;

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
        /// Gets the collection of <see cref="ElementDefinition"/> that hold parameter value to map
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
        /// Backing field for <see cref="SelectedParameter"/>
        /// </summary>
        private ParameterOrOverrideBase selectedParameter;

        /// <summary>
        /// Gets or sets the source <see cref="Parameter"/>
        /// </summary>
        public ParameterOrOverrideBase SelectedParameter
        {
            get => this.selectedParameter;
            set => this.RaiseAndSetIfChanged(ref this.selectedParameter, value);
        }

        /// <summary>
        /// Backing field for <see cref="SelectedElementUsage"/>
        /// </summary>
        private ElementUsage selectedElementUsage;

        /// <summary>
        /// Gets or sets the source <see cref="ElementDefinition"/>
        /// </summary>
        public ElementUsage SelectedElementUsage
        {
            get => this.selectedElementUsage;
            set => this.RaiseAndSetIfChanged(ref this.selectedElementUsage, value);
        }

        /// <summary>
        /// Backing field for <see cref="SelectedElementDefinition"/>
        /// </summary>
        private ElementDefinition selectedElementDefinition;

        /// <summary>
        /// Gets or sets the source <see cref="ElementDefinition"/>
        /// </summary>
        public ElementDefinition SelectedElementDefinition
        {
            get => this.selectedElementDefinition;
            set => this.RaiseAndSetIfChanged(ref this.selectedElementDefinition, value);
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
                .Subscribe(_ => this.UpdateHubFields(this.SelectedThingChanged));

            this.WhenAnyValue(x => x.Elements)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(this.CreateMappedElements));

            this.WhenAnyValue(x => x.SelectedElementDefinition)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(this.SelectedElementDefinitionChanged));
            
            this.WhenAnyValue(x => x.SelectedParameter)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(this.SelectedParameterChanged));

            this.WhenAnyValue(x => x.SelectedMappedElement.SelectedVariable)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(this.VerifyVariableTypesAreCompatible));

            this.WhenAnyValue(x => x.SelectedMappedElement)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(this.SetHubElementFromSelectedMappedElement));
        }

        /// <summary>
        /// Sets the <see cref="SelectedElementDefinition"/>, <see cref="SelectedElementUsage"/>
        /// </summary>
        private void SetHubElementFromSelectedMappedElement()
        {
            if (this.SelectedMappedElement is null)
            {
                return;
            }

            this.SelectedElementDefinition = this.SelectedMappedElement.SelectedParameter?.GetContainerOfType<ElementDefinition>();
            this.SelectedElementUsage = this.SelectedMappedElement.SelectedParameter?.GetContainerOfType<ElementUsage>();

            this.ComputesAvailableValues();
        }

        /// <summary>
        /// Occurs when the selected <see cref="ParameterOrOverrideBase"/> changes
        /// </summary>
        private void SelectedParameterChanged()
        {
            this.SetSelectedMappedElement(this.SelectedParameter);
            this.ComputesAvailableValues();
        }
        
        /// <summary>
        /// Occurs when the selected <see cref="ElementDefinition"/> changes
        /// </summary>
        private void SelectedElementDefinitionChanged()
        {
            if (this.SelectedElementDefinition is null)
            {
                return;
            }

            if (this.SelectedElementUsage?.ElementDefinition.Iid != this.SelectedElementDefinition?.Iid)
            {
                this.ElementUsages.Clear();
                this.ElementUsages.AddRange(this.SelectedElementDefinition.ReferencingElementUsages());
            }

            this.Parameters.Clear();
            this.Parameters.AddRange(this.SelectedElementDefinition.Parameter);

            if (this.SelectedElementUsage?.ParameterOverride is {} parameterOverrides)
            {
                this.Parameters.AddRange(parameterOverrides);
            }
        }

        /// <summary>
        /// Verifies that the selected variable has a compatible type with the parameter <see cref="ParameterType"/> selected
        /// </summary>
        private void VerifyVariableTypesAreCompatible()
        {
            if (!(this.SelectedMappedElement?.SelectedVariable is {} variable && this.SelectedMappedElement.SelectedParameter is {} parameter))
            {
                return;
            }

            var validationResult = parameter.ParameterType.Validate(variable.ActualValue, parameter.Scale);

            if (validationResult.ResultKind == ValidationResultKind.Valid)
            {
                this.StatusBar.Append($"Unable to map the {parameter.ParameterType.Name} with {variable.Name} \n\r {validationResult.Message}");
                this.SelectedMappedElement.SelectedVariable = null;
            }
            
            this.CheckCanExecute();
        }

        /// <summary>
        /// Invoked when the <see cref="SelectedThing"/> has changed
        /// </summary>
        private void SelectedThingChanged()
        {
            switch (this.SelectedThing)
            {
                case IRowViewModelBase<ElementDefinition> elementDefinition:
                {
                    this.SelectedElementDefinition = elementDefinition.Thing;
                    break;
                }
                case IRowViewModelBase<ElementUsage> elementUsage:
                {
                    this.SelectedElementUsage = elementUsage.Thing;
                    break;
                }
                case IRowViewModelBase<ParameterOrOverrideBase> parameterOrOverrideRow:
                {
                    this.SelectedMappedElement = this.MappedElements
                        .FirstOrDefault(x => x.SelectedParameter?.Iid == parameterOrOverrideRow.Thing.Iid);
                    break;
                }
                default:
                    return;
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
                new ValueSetValueRowViewModel(valueSet, value, scale));
        }

        /// <summary>
        /// Sets the <see cref="SelectedMappedElement"/>
        /// </summary>
        /// <param name="parameter">The corresponding <see cref="ParameterOrOverrideBase"/></param>
        private void SetSelectedMappedElement(Thing parameter)
        {
            if (parameter is null )
            {
                return;
            }

            this.SelectedMappedElement = this.MappedElements.FirstOrDefault(
                x => x.SelectedParameter.Iid == parameter.Iid);
        }

        /// <summary>
        /// Creates all <see cref="MappedElementDefinitionRowViewModel"/> and adds it to <see cref="MappedElements"/>
        /// </summary>
        public void CreateMappedElements()
        {
            foreach (var thing in this.ElementDefinitions.SelectMany(x => x.Parameter))
            {
                this.MappedElements.Add(this.CreateMappedElement(thing));
            }
            
            foreach (var parameterOverride in this.ElementDefinitions
                .SelectMany(x => x.ReferencingElementUsages()
                    .SelectMany(p => p.ParameterOverride)))
            {
                this.MappedElements.Add(this.CreateMappedElement(parameterOverride));
            }

            this.AssignMapping();
        }

        /// <summary>
        /// Creates a new <see cref="MappedElementDefinitionRowViewModel"/> and adds it to <see cref="MappedElements"/>
        /// </summary>
        /// <param name="thing">The base <see cref="Thing"/></param>
        /// <returns>A new <see cref="MappedElementDefinitionRowViewModel"/></returns>
        private MappedElementDefinitionRowViewModel CreateMappedElement(ParameterOrOverrideBase thing)
        {
            var selectedElement = new MappedElementDefinitionRowViewModel()
            {
                SelectedParameter = thing
            };

            selectedElement.WhenAnyValue(x => x.IsValid).Subscribe(_ => this.CheckCanExecute());
            return selectedElement;
        }

        /// <summary>
        /// Checks that any of the <see cref="MappedElements"/> is <see cref="MappedElementDefinitionRowViewModel.IsValid"/>
        /// </summary>
        public void CheckCanExecute()
        {
            this.CanContinue = this.MappedElements.Any(x => x.IsValid);
        }

        /// <summary>
        /// Update this view model properties
        /// </summary>
        private void UpdateProperties()
        {
            this.IsBusy = true;

            this.AvailableVariables = new ReactiveList<VariableRowViewModel>(
                this.DstController.Variables
                    .Where(x => this.DstController.IsVariableWritable(x.Reference))
                    .Select(r =>
                    {
                        r.Node = this.DstController.ReadNode(r.Reference);
                        return new VariableRowViewModel(r, false)
                        {
                            HasWriteAccess = true
                        };
                    }));

            this.MappedElements.ChangeTrackingEnabled = true;
            
            this.IsBusy = false;
        }

        /// <summary>
        /// Assings a mapping configuration if any to each of the selected variables
        /// </summary>
        private void AssignMapping()
        {
            foreach (var elementDefinitionRowViewModel in this.MappedElements)
            {
                elementDefinitionRowViewModel.MappingConfiguration =
                    this.DstController.ExternalIdentifierMap.Correspondence.FirstOrDefault(
                        x => elementDefinitionRowViewModel.SelectedParameter
                            .ValueSets.Cast<ParameterValueSetBase>()
                            .Any(v => v.Iid == x.InternalThing));
            }

            this.UpdatePropertiesBasedOnMappingConfiguration();
        }

        /// <summary>
        /// Updates the mapping based on the available 10-25 elements
        /// </summary>
        public void UpdatePropertiesBasedOnMappingConfiguration()
        {
            this.IsBusy = true;

            foreach (var rowViewModel in this.MappedElements.Where(x => x.MappingConfiguration != null))
            {
                if (this.HubController.GetThingById(
                        rowViewModel.MappingConfiguration.InternalThing, 
                        this.HubController.OpenIteration, out ParameterValueSetBase thing)
                && (this.ElementDefinitions.Any(
                        x => thing.GetContainerOfType<ElementDefinition>()?.Iid == x.Iid)
                || this.ElementDefinitions.Any(x => 
                    x.ReferencingElementUsages().Any( u => thing.GetContainerOfType<ElementUsage>()?.Iid == x.Iid))))
                { 
                    rowViewModel.SelectedParameter = thing.GetContainerOfType<ParameterOrOverrideBase>();
                    this.ComputesAvailableValues();

                    rowViewModel.SelectedVariable = this.AvailableVariables.FirstOrDefault(
                        x => x.Name == rowViewModel.MappingConfiguration.ExternalId);
                }
            }

            this.IsBusy = false;
        }
    }
}
