// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubMappingConfigurationDialogViewModel.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.ViewModel.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Windows.Input;

    using Autofac;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Validation;

    using CDP4Dal;

    using DEHPCommon;
    using DEHPCommon.Enumerators;
    using DEHPCommon.Events;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Services.MappingConfiguration;
    using DEHPEcosimPro.ViewModel.Dialogs.Interfaces;
    using DEHPEcosimPro.ViewModel.Rows;
    using DEHPEcosimPro.Views.Dialogs;

    using Opc.Ua;

    using ReactiveUI;

    /// <summary>
    /// The <see cref="HubMappingConfigurationDialogViewModel"/> is the view model to let the user configure the mapping to the Ecosim source
    /// </summary>
    public class HubMappingConfigurationDialogViewModel : MappingConfigurationDialogViewModel, IHubMappingConfigurationDialogViewModel
    {
        /// <summary>
        /// The <see cref="INavigationService" />
        /// </summary>
        private readonly INavigationService navigationService;

        /// <summary>
        /// Get the Selected Option of the parameter
        /// </summary>
        public Option SelectedOption { get; set; }

        /// <summary>
        /// Get the Selected State of the parameter
        /// </summary>
        public ActualFiniteState SelectedState { get; set; }

        /// <summary>
        /// Gets or sets the collection of available variables
        /// </summary>
        private VariableBaseRowViewModel selectedVariable;

        /// <summary>
        /// Gets or sets the collection of available variables
        /// </summary>
        public VariableBaseRowViewModel SelectedVariable
        {
            get => this.selectedVariable;
            set => this.RaiseAndSetIfChanged(ref this.selectedVariable, value);
        }

        /// <summary>
        /// Gets or sets the collection of available variables
        /// </summary>
        public ReactiveList<VariableBaseRowViewModel> AvailableVariables { get; set; } = new();
        
        /// <summary>
        /// Gets or sets the collection of <see cref="ElementDefinitionRowViewModel" /> that hold parameter value to map
        /// </summary>
        public ReactiveList<ElementDefinitionRowViewModel> Elements { get; set; } = new();
        
        /// <summary>
        /// Gets the collection of <see cref="MappedElementDefinitionRowViewModel"/>
        /// </summary>
        public ReactiveList<MappedElementDefinitionRowViewModel> MappedElements { get; } = new();

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
        /// Backing field for <see cref="CanContinue"/>
        /// </summary>
        private bool canContinue;

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel" />
        /// </summary>
        private readonly IStatusBarControlViewModel statusBarService;

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="MappingConfigurationDialogViewModel.ContinueCommand"/> can execute
        /// </summary>
        public bool CanContinue
        {
            get => this.canContinue;
            set => this.RaiseAndSetIfChanged(ref this.canContinue, value);
        }

        /// <summary>
        /// Gets the command that delete the specified row from the <see cref="MappedElements"/>
        /// </summary>
        public ReactiveCommand<object> DeleteMappedRowCommand { get; private set; }

        /// <summary>
        /// Initializes a new <see cref="HubMappingConfigurationDialogViewModel"/>
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="statusBarService">The <see cref="IStatusBarControlViewModel"/></param>
        /// <param name="navigationService">The <see cref="INavigationService" /></param>
        public HubMappingConfigurationDialogViewModel(IHubController hubController,
            IDstController dstController, IStatusBarControlViewModel statusBarService, INavigationService navigationService) 
            : base(hubController, dstController, statusBarService)
        {
            this.navigationService = navigationService;
            this.statusBarService = statusBarService;
            this.UpdateProperties();
            this.InitializesCommandsAndObservableSubscriptions();
        }

        /// <summary>
        /// Initializes this view model <see cref="ICommand"/> and <see cref="IObservable{T}"/>
        /// </summary>
        private void InitializesCommandsAndObservableSubscriptions()
        {
            this.ContinueCommand = ReactiveCommand.Create(this.WhenAnyValue(x => x.CanContinue), RxApp.MainThreadScheduler);

            this.Disposables.Add(this.ContinueCommand.Subscribe(_ => this.ExecuteContinueCommand(() =>
                {
                    var mappedElement =
                        this.MappedElements.Where(x => x.IsValid).ToList();

                    this.DstController.Map(mappedElement);
                    this.statusBarService.Append($"Mapping in progress of {mappedElement.Count} value(s)...");
                })));

            this.Disposables.Add(this.WhenAnyValue(x => x.Elements)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(this.LoadExistingMappedElement)));

            this.Disposables.Add(this.WhenAnyValue(x => x.SelectedParameter)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(this.SelectedParameterChanged)));

            this.Disposables.Add(this.WhenAnyValue(x => x.SelectedVariable)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(this.SelectedAvailableVariablesChanged)));

            this.Disposables.Add(this.WhenAnyValue(x => x.SelectedThing)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(this.SelectedThingChanged), e =>
                {
                    this.StatusBar.Append($"An error of type {e.GetType().Name}, check the log for more detail.");
                    this.Logger.Error(e);
                }));

            this.DeleteMappedRowCommand = ReactiveCommand.Create(this.WhenAny(x => x.SelectedMappedElement,
                x =>
                    x.Value != null && this.DstController.HubMapResult.All(h => 
                        h.SelectedParameter.Iid != x.Value.SelectedParameter.Iid)));

            this.Disposables.Add(this.DeleteMappedRowCommand.OfType<Guid>()
                .Subscribe(this.DeleteMappedRowCommandExecute));
        }

        /// <summary>
        /// Executes the <see cref="DeleteMappedRowCommand"/>
        /// </summary>
        /// <param name="iid">The id of the Thing to delete from <see cref="MappedElements"/>/></param>
        private void DeleteMappedRowCommandExecute(Guid iid)
        {
            var mappedElement = this.MappedElements.FirstOrDefault(x => x.SelectedParameter.Iid == iid);

            if (mappedElement is null)
            {
                this.Logger.Info($"No MappedElement has been found with the Iid: {iid}");
                return;
            }

            CDPMessageBus.Current.SendMessage(new SelectEvent(mappedElement.SelectedParameter.GetContainerOfType<ElementDefinition>(), true));
            this.MappedElements.Remove(mappedElement);
            var mappingConfigurationService = AppContainer.Container.Resolve<IMappingConfigurationService>();

            mappingConfigurationService.ExternalIdentifierMap.Correspondence.Remove(mappingConfigurationService
                .ExternalIdentifierMap.Correspondence.FirstOrDefault(x => x.InternalThing == iid));
            
            this.CheckCanExecute();
        }
        
        /// <summary>
        /// Occurs when the selected <see cref="ParameterOrOverrideBase"/> changes
        /// </summary>
        private void SelectedParameterChanged()
        {
            this.SetSelectedMappedElement(this.SelectedParameter);
        }

        /// <summary>
        /// Dispose this <see cref="HubMappingConfigurationDialogViewModel" />
        /// </summary>
        /// <param name="disposing">A value indicating if it should dispose or not</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                foreach (var variable in this.AvailableVariables)
                {
                    variable.Dispose();
                }

                this.AvailableVariables.Clear();
            }
        }

        /// <summary>
        /// Verifies that the selected variable has a compatible type with the parameter <see cref="ParameterType"/> selected
        /// </summary>
        public void AreVariableTypesCompatible()
        {
            if (!(this.SelectedVariable is {} variable && this.SelectedMappedElement?.SelectedParameter is {} parameter))
            {
                return;
            }

            var validationResult = new ValidationResult
            {
                Message = string.Empty
            };

            if (variable is ArrayVariableRowViewModel arrayVariable && parameter.ParameterType is SampledFunctionParameterType sampledFunctionParameter)
            {
                validationResult.ResultKind = this.AreVariableTypesCompatible(arrayVariable, parameter);
            }
            else if(parameter.ParameterType is not SampledFunctionParameterType)
            {
                validationResult = parameter.ParameterType.Validate(variable.ActualValue, parameter.Scale);
                this.SelectedMappedElement.SelectedParameter = parameter;
                this.SelectedMappedElement.SelectedVariable = this.SelectedVariable;
                this.SelectedMappedElement.VerifyValidity();
            }

            if (validationResult.ResultKind == ValidationResultKind.Invalid)
            {
                this.statusBarService.Append(
                    $"Unable to map the {parameter.ParameterType.Name} with {variable.Name} \n\r {validationResult.Message}",
                    StatusBarMessageSeverity.Error);

                this.SelectedMappedElement.SelectedVariable = null;
            }

            this.SelectedParameter = null;
            this.SelectedVariable = null;
            this.CheckCanExecute();
        }

        /// <summary>
        /// Determine if arrays selected as variable and parameters are compatibles and show a dialog to let the user choose which parameter columns should be mapped to variable column
        /// </summary>
        /// <param name="variable">selected variable that is <see cref="ArrayVariableRowViewModel"/></param>
        /// <param name="parameter">selected parameter that is <see cref="SampledFunctionParameterType"/></param>
        /// <returns>A <see cref="ValidationResultKind"/></returns>
        private ValidationResultKind AreVariableTypesCompatible(ArrayVariableRowViewModel variable, ParameterOrOverrideBase parameter)
        {
            if (this.AreArraysCompatible(variable, parameter))
            {
                var viewModel = new ArrayParameterMappingConfigurationDialogViewModel(variable, parameter);

                if (this.navigationService
                        .ShowDxDialog<ArrayParameterMappingConfigurationDialog, ArrayParameterMappingConfigurationDialogViewModel>(viewModel)
                    != true)
                {
                    return ValidationResultKind.InConclusive;
                }

                this.MapParameterToVariable(viewModel, parameter);
                return ValidationResultKind.Valid;
            }
            
            if(this.AreTwoDimensionsArrayCompatible(variable, parameter))
            {
                var viewModel = new TwoDimensionsArrayMappingConfigurationDialogViewModel(parameter);

                if (this.navigationService
                        .ShowDxDialog<TwoDimensionsArrayMappingConfigurationDialog, TwoDimensionsArrayMappingConfigurationDialogViewModel>(viewModel)
                    != true)
                {
                    return ValidationResultKind.InConclusive;
                }

                this.MapParameterToVariable(variable, viewModel.SelectedItem, parameter);
                return ValidationResultKind.Valid;
            }

            return ValidationResultKind.Invalid;
        }

        /// <summary>
        /// Invoked when the <see cref="SelectedThing"/> has changed
        /// </summary>
        private void SelectedThingChanged()
        {
            if (this.SelectedThing is ParameterValueRowViewModel parameterValueRow)
            {
                this.SelectedOption = parameterValueRow.ActualOption;
                this.SelectedState = parameterValueRow.ActualState;
                
                this.SetsSelectedMappedElement((ParameterOrOverrideBase)parameterValueRow.Thing);
            }
            else if (this.SelectedThing is ParameterOrOverrideBaseRowViewModel parameterOrOverride)
            {
                this.SetsSelectedMappedElement(parameterOrOverride.Thing);
            }
        }

        /// <summary>
        /// Sets the selected <see cref="MappedElementDefinitionRowViewModel"/> based on the provided 
        /// </summary>
        /// <param name="parameter">The <see cref="ParameterOrOverrideBase"/></param>
        private void SetsSelectedMappedElement(ParameterOrOverrideBase parameter)
        {
            this.SelectedMappedElement = this.MappedElements
                                                             .FirstOrDefault(x => x.SelectedParameter?.Iid == parameter.Iid)
                                                         ?? this.CreateMappedElement(parameter);

            this.SelectedMappedElement.SelectedParameter = parameter;

            if (this.SelectedVariable != null)
            {
                this.AreVariableTypesCompatible();
            }
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
        /// Creates all <see cref="MappedElementDefinitionRowViewModel"/> and adds it to <see cref="MappedElements"/>. Use to add the already saved mapping.
        /// </summary>
        public void LoadExistingMappedElement()
        {
            if (!this.DstController.HubMapResult.Any())
            {
                return;
            }

            this.MappedElements.AddRange(this.DstController.HubMapResult.Distinct());
            this.CheckCanExecute();
        }

        /// <summary>
        /// Creates a new <see cref="MappedElementDefinitionRowViewModel"/> and adds it to <see cref="MappedElements"/>
        /// if it does not exist in the mapped things
        /// </summary>
        /// <param name="parameterOrOverride">The base <see cref="Thing"/></param>
        /// <returns>A new <see cref="MappedElementDefinitionRowViewModel"/></returns>
        private MappedElementDefinitionRowViewModel CreateMappedElement(ParameterOrOverrideBase parameterOrOverride)
        {
            MappedElementDefinitionRowViewModel mappedElement;

            if (this.MappedElements.LastOrDefault(x => x.SelectedVariable is null) is { } lastMappedElement)
            {
                this.MappedElements.Remove(lastMappedElement);
            }

            if (this.DstController.HubMapResult
                    .FirstOrDefault(x => x.SelectedParameter.Iid == parameterOrOverride.Iid)
                is { } existinMappedElement)
            {
                mappedElement = existinMappedElement;
            }
            else
            {
                var selectedValue = parameterOrOverride.ParameterType is SampledFunctionParameterType
                    ? null
                    : parameterOrOverride.ValueSets.SelectMany(x => this.ComputesValueRow(x, x.ActualValue, null)).FirstOrDefault();
               
                mappedElement = new MappedElementDefinitionRowViewModel()
                {
                    SelectedParameter = parameterOrOverride,
                    SelectedValue = selectedValue
                };
            }

            this.MappedElements.Add(mappedElement);

            mappedElement.WhenAnyValue(x => x.IsValid).Subscribe(_ => this.CheckCanExecute());
            return mappedElement;
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

            var availableVariables =
                this.DstController.Variables
                    .Where(x => this.DstController.IsVariableWritable(x.Reference))
                    .ToList();

            if (availableVariables.Count == 0)
            {
                return;
            }

            var arrayVariables = availableVariables.Where(x => x.Reference.DisplayName.Text.Contains("["));

            this.AvailableVariables.AddRange(availableVariables.Where(x => arrayVariables
                    .All(y => y.Reference.NodeId.Identifier != x.Reference.NodeId.Identifier))
                .Select(x =>
                {
                    var (reference, _) = x;
                    return new VariableRowViewModel((reference, this.DstController.ReadNode(x.Reference)));
                }));

            var groupingOfReferencesFromVariables = arrayVariables.GroupBy(x => this.GetArrayName(x.Reference));

            foreach (var arrayVariablesGrouped in groupingOfReferencesFromVariables)
            {
                if (arrayVariablesGrouped.Key == null)
                {
                    continue;
                }

                if (arrayVariablesGrouped.Count() < 2)
                {
                    this.AvailableVariables.AddRange(arrayVariablesGrouped
                        .Select(x =>
                        {
                            var (reference, _) = x;
                            return new VariableRowViewModel((reference, this.DstController.ReadNode(x.Reference)));
                        }));
                } 
                else
                { 
                    this.AvailableVariables.Add(
                        new ArrayVariableRowViewModel(arrayVariablesGrouped.Key, 
                            arrayVariablesGrouped.Select(x => 
                                new VariableRowViewModel((x.Reference, this.DstController.ReadNode(x.Reference))))));
                }
            }
            
            this.IsBusy = false;
        }

        /// <summary>
        /// Gets the name of the array variable by ignoring the included indice out of the specified <paramref name="referenceDescription"/>
        /// </summary>
        /// <param name="referenceDescription">The <see cref="ReferenceDescription"/> that holds the name to parse</param>
        /// <returns>The bare array variable name</returns>
        private string GetArrayName(ReferenceDescription referenceDescription)
        {
            var newName = referenceDescription.DisplayName.Text.Split('[')[0];

            if (referenceDescription.DisplayName.Text.Contains('\''))
            {
                newName = $"{newName}'";
            }

            return newName;
        }

        /// <summary>
        /// Invoked when the <see cref="SelectedVariable"/> has changed
        /// </summary>
        public void SelectedAvailableVariablesChanged()
        {
            this.AreVariableTypesCompatible();
        }
        
        /// <summary>
        /// Compares the number of rows of the <paramref name="variable"/> and the <see cref="parameter"/> </param>
        /// </summary>
        /// <param name="variable">The <see cref="ArrayVariableRowViewModel"/></param>
        /// <param name="parameter">The <see cref="SampledFunctionParameterType"/> <see cref="Parameter"/></param>
        /// <returns>A value indicating whether the two provided references are compatible</returns>
        public bool AreArraysCompatible(ArrayVariableRowViewModel variable, ParameterOrOverrideBase parameter)
        {
            if (parameter.ParameterType is not SampledFunctionParameterType sampledFunctionParameterType)
            {
                return false;
            }

            var result = parameter.QueryParameterBaseValueSet(this.SelectedOption, this.SelectedState)
                             .ActualValue.Count / sampledFunctionParameterType.NumberOfValues == variable.NumberOfValues;

            return result;
        }

        /// <summary>
        /// Verifies if the <see cref="ArrayVariableRowViewModel"/> is a 2 dimensions array and if it is compatible
        /// with the provided <see cref="ParameterOrOverrideBase"/>
        /// </summary>
        /// <param name="variable">The <see cref="ArrayVariableRowViewModel"/></param>
        /// <param name="parameter">The <see cref="ParameterOrOverrideBase"/></param>
        /// <returns>A value indicating if the <see cref="ArrayVariableRowViewModel"/> and the <see cref="ParameterOrOverrideBase"/>
        /// are compatible</returns>
        private bool AreTwoDimensionsArrayCompatible(ArrayVariableRowViewModel variable, ParameterOrOverrideBase parameter)
        {
            if (parameter.ParameterType is not SampledFunctionParameterType sampledFunctionParameterType)
            {
                return false;
            }

            if (sampledFunctionParameterType.NumberOfValues != 2)
            {
                return false;
            }

            var rowCount = parameter.QueryParameterBaseValueSet(this.SelectedOption, this.SelectedState)
                .ActualValue.Count / sampledFunctionParameterType.NumberOfValues;

            return (variable.Dimensions.Count == 2 && variable.Dimensions.Contains(rowCount) 
                                                   && variable.Dimensions.Contains(sampledFunctionParameterType.NumberOfValues));
        }

        /// <summary>
        /// Maps the <see cref="ArrayVariableRowViewModel"/> values based on the chosen <see cref="IParameterTypeAssignment"/>
        /// </summary>
        /// <param name="variable">The <see cref="ArrayVariableRowViewModel"/></param>
        /// <param name="parameterTypeAssignment">The <see cref="IParameterTypeAssignment"/></param>
        /// <param name="parameter">The <see cref="ParameterOrOverrideBase"/></param>
        private void MapParameterToVariable(ArrayVariableRowViewModel variable, IParameterTypeAssignment parameterTypeAssignment, ParameterOrOverrideBase parameter)
        {
            this.MappedElements.Remove(this.SelectedMappedElement);

            if (parameter.ParameterType is not SampledFunctionParameterType parameterType)
            {
                this.statusBarService.Append($"The selected parameter is not supported for mapping to a array opc variable");
                return;
            }

            var isSelectedParameterTypeIsIndependent = parameterTypeAssignment is IndependentParameterTypeAssignment;

            var actualValues = parameter.QueryParameterBaseValueSet(this.SelectedOption, this.SelectedState).ActualValue;

            var rowCount = actualValues.Count / parameterType.NumberOfValues;

            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                var independentValue = actualValues[2 * rowIndex];
                var dependentValue = actualValues[(2 * rowIndex)+1];

                var firstElement = new MappedElementDefinitionRowViewModel()
                {
                    SelectedParameter = parameter,

                    SelectedValue = new ValueSetValueRowViewModel(
                        parameter.QueryParameterBaseValueSet(this.SelectedOption, this.SelectedState), 
                        isSelectedParameterTypeIsIndependent ? independentValue : dependentValue, parameter.Scale),

                    SelectedVariable = variable.Variables[rowIndex *2]
                };

                firstElement.VerifyValidity();

                var existingRow = this.MappedElements.FirstOrDefault(x =>
                    x.SelectedVariable.Reference.NodeId.Identifier.Equals(firstElement.SelectedVariable.Reference.NodeId.Identifier));

                this.MappedElements.Remove(existingRow);
                this.MappedElements.Add(firstElement);

                var secondElement = new MappedElementDefinitionRowViewModel()
                {
                    SelectedParameter = parameter,

                    SelectedValue = new ValueSetValueRowViewModel(
                        parameter.QueryParameterBaseValueSet(this.SelectedOption, this.SelectedState),
                        isSelectedParameterTypeIsIndependent ? dependentValue : independentValue, parameter.Scale),

                    SelectedVariable = variable.Variables[(rowIndex * 2)+1]
                };

                secondElement.VerifyValidity();

                var existingSecondRow = this.MappedElements.FirstOrDefault(x =>
                    x.SelectedVariable.Reference.NodeId.Identifier.Equals(secondElement.SelectedVariable.Reference.NodeId.Identifier));

                this.MappedElements.Remove(existingSecondRow);
                this.MappedElements.Add(secondElement);
            }
        }

        /// <summary>
        /// Maps the correponding values to the chosen columns
        /// </summary>
        /// <param name="arrayParameterMappingConfigurationDialogViewModel"></param>
        /// <param name="parameter">parameter to map on variable</param>
        public void MapParameterToVariable(ArrayParameterMappingConfigurationDialogViewModel arrayParameterMappingConfigurationDialogViewModel, ParameterOrOverrideBase parameter)
        {
            this.MappedElements.Remove(this.SelectedMappedElement);

            foreach (var arrayParameterMappingConfigurationRowViewModel in arrayParameterMappingConfigurationDialogViewModel.MappingRows)
            {
                if (arrayParameterMappingConfigurationRowViewModel.SelectedParameterType == null)
                {
                    continue;
                }

                var variables = arrayParameterMappingConfigurationDialogViewModel.MappingRows
                    .Where(x => x.SelectedParameterType != null)
                    .Select(x => (x.Index, x.SelectedParameterType, x.Variables));

                if (parameter.ParameterType is not SampledFunctionParameterType parameterType)
                {
                    this.statusBarService.Append($"The selected parameter is not supported for mapping to a array opc variable");
                    return;
                }
                
                var allParameterType = new List<ParameterType>(parameterType.IndependentParameterType.Select(x => x.ParameterType));
                allParameterType.AddRange(parameterType.DependentParameterType.Select(x => x.ParameterType));

                foreach (var (_, selectedParameterTypeShortName, variableRowViewModels) in variables)
                {
                    if (allParameterType.FirstOrDefault(x => x.ShortName == selectedParameterTypeShortName) is not { } selectedParameterType)
                    {
                        this.Logger.Error($"The parameter type: {selectedParameterTypeShortName} was not found in the parameter independent or dependent parameter types.");
                        continue;
                    }

                    var values = this.GetSpecificParameterTypeValues(parameter, allParameterType, selectedParameterType);

                    foreach (var variable in variableRowViewModels)
                    {
                        var value = values[variable.Index.Aggregate((dimension0, dimension1) => dimension0 * dimension1) - 1];

                        var element = new MappedElementDefinitionRowViewModel()
                        {
                            SelectedParameter = parameter,

                            SelectedValue = new ValueSetValueRowViewModel(
                                parameter.QueryParameterBaseValueSet(this.SelectedOption, this.SelectedState), value, parameter.Scale),

                            SelectedVariable = variable,
                        };

                        element.VerifyValidity();

                        var existingRow = this.MappedElements.FirstOrDefault(x =>
                            x.SelectedVariable.Reference.NodeId.Identifier.Equals(variable.Reference.NodeId.Identifier));

                        this.MappedElements.Remove(existingRow);
                        this.MappedElements.Add(element);
                    }
                }
            }
        }

        /// <summary>
        /// Gets an <see cref="IEnumerable{T}"/> of values that belongs to <paramref name="selectedParameterType"/>
        /// from the <see cref="IValueSet"/> of the <paramref name="parameter"/>
        /// </summary>
        /// <param name="parameter">The <see cref="ParameterBase"/> to get the values from</param>
        /// <param name="allParameterType">The collection of <see cref="ParameterType"/> from the <paramref name="parameter"/></param>
        /// <param name="selectedParameterType">The <see cref="ParameterType"/></param>
        /// <returns>A <see cref="List{T}"/> of <see cref="string"/></returns>
        private List<string> GetSpecificParameterTypeValues(ParameterBase parameter, IList<ParameterType> allParameterType, ParameterType selectedParameterType)
        {
            var actualValue = parameter.QueryParameterBaseValueSet(this.SelectedOption, this.SelectedState).ActualValue;

            var values = new List<string>();

            for (var i = allParameterType.IndexOf(selectedParameterType); i < actualValue.Count; i += parameter.ParameterType.NumberOfValues)
            {
                values.Add(actualValue[i]);
            }

            return values;
        }
    }
}
