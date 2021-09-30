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
    using System.Data;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Windows.Input;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Validation;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Services.MappingConfiguration;
    using DEHPEcosimPro.ViewModel.Dialogs.Interfaces;
    using DEHPEcosimPro.ViewModel.Rows;
    using DEHPEcosimPro.Views.Dialogs;

    using DevExpress.Data.Helpers;

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
        private readonly INavigationService navigation;
        
        /// <summary>
        /// </summary>
        public ChooseMappingColumnsViewModel ListOfRowsToMap { get; set; } = new ChooseMappingColumnsViewModel();

        /// <summary>
        /// 
        /// </summary>
        public Option SelectedOption { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ActualFiniteState SelectedState { get; set; }

        /// <summary>
        /// </summary>
        private VariableBaseRowViewModel selectedAvailableVariables;

        /// <summary>
        /// Gets or sets the collection of available variables
        /// </summary>
        public VariableBaseRowViewModel SelectedAvailableVariables
        {
            get => this.selectedAvailableVariables;
            set => this.RaiseAndSetIfChanged(ref this.selectedAvailableVariables, value);
        }

        /// <summary>
        /// Gets or sets the collection of available variables
        /// </summary>
        public ReactiveList<VariableBaseRowViewModel> AvailableVariables { get; set; } = new ReactiveList<VariableBaseRowViewModel>();
        
        /// <summary>
        /// Gets or sets the collection of <see cref="ElementDefinitionRowViewModel" /> that hold parameter value to map
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

        private IStatusBarControlViewModel statusBar;

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
            IDstController dstController, IStatusBarControlViewModel statusBar, IMappingConfigurationService mappingService, INavigationService navigation) :
            base(hubController, dstController, statusBar)
        {
            this.navigation = navigation;
            this.statusBar = statusBar;
            this.UpdateProperties();
            this.InitializesCommandsAndObservableSubscriptions();
        }

        /// <summary>
        /// Initializes this view model <see cref="ICommand"/> and <see cref="IObservable{T}"/>
        /// </summary>
        private void InitializesCommandsAndObservableSubscriptions()
        {
            this.ContinueCommand = ReactiveCommand.Create(this.WhenAnyValue(x => x.CanContinue), RxApp.MainThreadScheduler);

            this.ContinueCommand.Subscribe(_ => this.ExecuteContinueCommand(
                () =>
                {
                    var mappedElement =
                        this.MappedElements.Where(x => x.IsValid).ToList();

                    this.DstController.Map(mappedElement);
                    this.StatusBar.Append($"Mapping in progress of {mappedElement.Count} value(s)...");
                }));
            this.WhenAnyValue(x => x.Elements)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(this.CreateMappedElements));

            this.WhenAnyValue(x => x.SelectedElementDefinition)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(this.SelectedElementDefinitionChanged));

            this.WhenAnyValue(x => x.SelectedParameter)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(this.SelectedParameterChanged));

            this.WhenAnyValue(x => x.SelectedAvailableVariables)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(this.SelectedAvailableVariablesChanged));

            this.WhenAnyValue(x => x.SelectedMappedElement)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(this.SetHubElementFromSelectedMappedElement));

            this.WhenAnyValue(x => x.SelectedThing)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(this.SelectedThingChanged));

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
        public void AreVariableTypesAreCompatible()
        {
            if (!(this.SelectedAvailableVariables is {} variable && this.SelectedMappedElement?.SelectedParameter is {} parameter))
            {
                return;
            }
            
            ValidationResult validationResult;
            validationResult.Message = string.Empty;

            if (parameter.ParameterType is SampledFunctionParameterType sampledFunctionParameterType)
            {
                bool? userSelectecValue = false;
                var areArraysCompatible = this.AreArraysCompatible(variable, parameter);

                if (areArraysCompatible && variable is ArrayVariableRowViewModel array)
                {
                    this.ListOfRowsToMap = new ChooseMappingColumnsViewModel(array, parameter);
                    var table = new ValueSetsToTableViewModel(parameter, this.SelectedOption, this.SelectedState);
                    userSelectecValue = this.navigation.ShowDxDialog<ChooseMappingColumns, ChooseMappingColumnsViewModel>(this.ListOfRowsToMap);

                    if (userSelectecValue == true)
                    {
                        this.MapParameterToVariable(table.ListOfTuple, parameter);
                    }
                }
                else
                {
                    //not compatible, show error to user
                }

                validationResult.ResultKind = userSelectecValue == true && areArraysCompatible
                    ? ValidationResultKind.Valid
                    : ValidationResultKind.Invalid;
                 
            }
            else
            {
                validationResult = parameter.ParameterType.Validate(variable.ActualValue, parameter.Scale);
                this.SelectedMappedElement.SelectedVariable = (VariableRowViewModel) this.SelectedAvailableVariables;
                this.SelectedMappedElement.VerifyValidity();
            }

            if (validationResult.ResultKind != ValidationResultKind.Valid)
            {
                this.StatusBar.Append(
                    $"Unable to map the {parameter.ParameterType.Name} with {variable.Name} \n\r {validationResult.Message}",
                    StatusBarMessageSeverity.Error);

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
                        var a = this.Values.Where(x => !string.IsNullOrWhiteSpace(x.Value) && x.Value != "-");
                        this.SelectedMappedElement.SelectedValue = a != null ? a.FirstOrDefault() : null ;
                        break;
                    }
                case ParameterStateRowViewModel parameterState:
                    {
                        this.SelectedOption = parameterState.ActualOption;
                        this.SelectedState = parameterState.ActualState;

                        this.SelectedMappedElement = this.MappedElements
                            .FirstOrDefault(x => x.SelectedParameter?.Iid == parameterState.Thing.Iid);

                        if (this.SelectedMappedElement == null)
                        {
                            this.MappedElements.Add(this.CreateMappedElement((ParameterOrOverrideBase)parameterState.Thing));

                            this.SelectedMappedElement = this.MappedElements
                                .FirstOrDefault(x => x.SelectedParameter?.Iid == parameterState.Thing.Iid);
                        }

                        if (this.SelectedAvailableVariables != null)
                        {
                            this.AreVariableTypesAreCompatible();
                        }

                        break;
                    }
                case ParameterOptionRowViewModel parameterOption:
                    {
                        this.SelectedOption = parameterOption.ActualOption;
                        this.SelectedState = parameterOption.ActualState;
                        this.SelectedMappedElement = this.MappedElements
                            .FirstOrDefault(x => x.SelectedParameter?.Iid == parameterOption.Thing.Iid);

                        if (this.SelectedMappedElement == null)
                        {
                            this.MappedElements.Add(this.CreateMappedElement((ParameterOrOverrideBase) parameterOption.Thing));

                            this.SelectedMappedElement = this.MappedElements
                                .FirstOrDefault(x => x.SelectedParameter?.Iid == parameterOption.Thing.Iid);
                        }

                        if (this.SelectedAvailableVariables != null)
                        {
                            this.AreVariableTypesAreCompatible();
                        }

                        break;
                    }
                case IRowViewModelBase<ParameterOrOverrideBase> parameterOrOverrideRow:
                    {
                        this.SelectedMappedElement = this.MappedElements
                            .FirstOrDefault(x => x.SelectedParameter?.Iid == parameterOrOverrideRow.Thing.Iid);
                        if (this.SelectedMappedElement == null)
                        {
                            this.MappedElements.Add(this.CreateMappedElement((ParameterOrOverrideBase) parameterOrOverrideRow.Thing));

                            this.SelectedMappedElement = this.MappedElements
                                .FirstOrDefault(x => x.SelectedParameter?.Iid == parameterOrOverrideRow.Thing.Iid);
                        }

                        if (this.Values.Count == 0)
                        {
                            this.ComputesAvailableValues();
                        }
                        this.SelectedMappedElement.SelectedValue = this.Values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Value) && x.Value != "-") ?? this.Values.First();
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
            if (this.DstController.HubMapResult != null)
            {
                foreach (var element in this.DstController.HubMapResult)
                {
                    element.VerifyValidity();
                    this.MappedElements.Add(element);
                }
                this.CheckCanExecute();
            }
            //foreach (var thing in this.ElementDefinitions.SelectMany(x => x.Parameter))
            //{
            //    this.MappedElements.Add(this.CreateMappedElement(thing));
            //}

            //foreach (var parameterOverride in this.ElementDefinitions
            //    .SelectMany(x => x.ReferencingElementUsages()
            //        .SelectMany(p => p.ParameterOverride)))
            //{
            //    this.MappedElements.Add(this.CreateMappedElement(parameterOverride));
            //}

            //this.CheckCanExecute();
        }

        /// <summary>
        /// Creates a new <see cref="MappedElementDefinitionRowViewModel"/> and adds it to <see cref="MappedElements"/>
        /// if it does not exist in the mapped things
        /// </summary>
        /// <param name="thing">The base <see cref="Thing"/></param>
        /// <returns>A new <see cref="MappedElementDefinitionRowViewModel"/></returns>
        private MappedElementDefinitionRowViewModel CreateMappedElement(ParameterOrOverrideBase thing)
        {
            var selectedElement = this.DstController.HubMapResult
                    .FirstOrDefault(x => x.SelectedParameter.Iid == thing.Iid)
                is { } existinMappedElement
                ? existinMappedElement
                : new MappedElementDefinitionRowViewModel() { SelectedParameter = thing };

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

            var availableVariables =
                this.DstController.Variables
                    .Where(x => this.DstController.IsVariableWritable(x.Reference))
                    .ToList();

            if (availableVariables.Count == 0)
            {
                return;
            }

            var arrayVariables = availableVariables.Where(x => x.Reference.DisplayName.Text.Contains("[")).ToList();

            this.AvailableVariables.AddRange(availableVariables.Where(x => arrayVariables
                    .All(y => y.Reference.NodeId.Identifier != x.Reference.NodeId.Identifier))
                .Select(x =>
                {
                    var (reference, _) = x;
                    return new VariableRowViewModel((reference, this.DstController.ReadNode(x.Reference)));
                }));

            var grouby = arrayVariables.GroupBy(x => this.GetNameFromArrayReference(x.Reference));

            foreach (var arrayVariablesGrouped in grouby)
            {
                if (arrayVariablesGrouped.Key == null)
                {
                    continue;
                }

                if (arrayVariablesGrouped.ToList().Count < 2)
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
                    this.AvailableVariables.Add(new ArrayVariableRowViewModel(arrayVariablesGrouped.Key, arrayVariablesGrouped
                    .Select(x => new VariableRowViewModel((x.Reference, this.DstController.ReadNode(x.Reference))))));

                }
            }

            this.MappedElements.ChangeTrackingEnabled = true;

            this.IsBusy = false;
        }

        /// <summary>
        /// </summary>
        public void SelectedAvailableVariablesChanged()
        {
            this.AreVariableTypesAreCompatible();
        }

        /// <summary>
        /// </summary>
        /// <param name="reference"></param>
        /// <returns></returns>
        private string GetNameFromArrayReference(ReferenceDescription reference)
        {
            var splitedName = reference.BrowseName.Name.Split('[', ']');

            if (splitedName.Length >= 1 && splitedName.Length <= 2)
            {
                return splitedName[0];
            }

            if (splitedName.Length > 2)
            {
                return splitedName[0] + splitedName[2];
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="parameter"></param>
        /// <param name="compatibilityOfColumns"></param>
        /// <returns></returns>
        private bool AreArraysCompatible(VariableBaseRowViewModel variable, ParameterOrOverrideBase parameter)
        {
            var rowsAndColumnsOfVariable = variable.ActualValue.ToString().Split('[', 'x', ']');

            var columnsOfParameter = parameter.ParameterType.NumberOfValues;

            var columnByRowsOfParameter = parameter.QueryParameterBaseValueSet
                (this.SelectedOption, this.SelectedState).ActualValue.Count ;

            string rowsOfVariable = "", columnsOfVariable = "";

            if (rowsAndColumnsOfVariable.Length >= 2)
            {
                rowsOfVariable = rowsAndColumnsOfVariable[1];
                columnsOfVariable = rowsAndColumnsOfVariable[2];
            }
            else
            {
                return false;
            }

            var isNumberOfRowsOfVariable = int.TryParse(rowsOfVariable, out var numberOfRowsOfVariable);

            var numberOfRowsOfParameter = columnByRowsOfParameter / columnsOfParameter;

            bool areRowsCompatibles = false ; 

            if (isNumberOfRowsOfVariable && numberOfRowsOfParameter != 0 && numberOfRowsOfVariable != 0)
            {
                areRowsCompatibles = numberOfRowsOfParameter == numberOfRowsOfVariable;
            }

            return areRowsCompatibles;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableListOfTuple"></param>
        /// <param name="parameter"></param>
        private void MapParameterToVariable(List<(string name, List<string> list)> tableListOfTuple, ParameterOrOverrideBase parameter)
        {
            var lisOfMappedElement = new List<MappedElementDefinitionRowViewModel>();
            var variableName = this.ListOfRowsToMap.VariableName;

            var listOfValueByRowsAndColumns = this.DstController.Variables
                .Where(x => this.DstController.IsVariableWritable(x.Reference)
                            && this.GetNameFromArrayReference(x.Reference) == variableName).ToList();

            var listOfVariableRowViewModel = listOfValueByRowsAndColumns.Select(x =>
            {
                var (reference, _) = x;
                return new VariableRowViewModel((reference, this.DstController.ReadNode(x.Reference)));
            }).ToList();
            for (int i = 0; i < listOfVariableRowViewModel.Count(); i++)
            {
                foreach (var parameterChoosen in this.ListOfRowsToMap.ListOfVariableToMap)
                {
                    this.GetRowAndColumnNumberFromName(listOfVariableRowViewModel[i].Name, out int rowNumber, out int colNumber);
                    
                    if (parameterChoosen.SelectedColumnMatched != null && (colNumber == 0 || parameterChoosen.Index.Contains(colNumber.ToString())))
                    {
                        var columnOfParameter = parameterChoosen.SelectedColumnMatched;
                        var colData = tableListOfTuple.FirstOrDefault(x => x.name == columnOfParameter);

                        var variableRowViewModel = listOfVariableRowViewModel.FirstOrDefault(x => x.Name == listOfVariableRowViewModel[i].Name);

                        var element = new MappedElementDefinitionRowViewModel()
                        {
                            SelectedParameter = parameter,
                            SelectedValue = new ValueSetValueRowViewModel(parameter.QueryParameterBaseValueSet(this.SelectedOption, this.SelectedState), colData.list[rowNumber - 1], null),
                            SelectedVariable = variableRowViewModel,
                        };

                        element.VerifyValidity();

                        if (element.IsValid)
                        {
                            lisOfMappedElement.Add(element);
                            this.MappedElements.Add(element);
                        }

                    }
                    var result = lisOfMappedElement.Select(x => x.SelectedVariable.Name).ToList();
                }

            }
        }

        /// <summary>
        /// Get as a number the row and the colomn out of the name
        /// </summary>
        /// <param name="name">name of a value, should include [x] or [x,y]</param>
        /// <param name="rowNumber">the row of the value</param>
        /// <param name="colNumber">the column of the value</param>
        private void GetRowAndColumnNumberFromName(string name, out int rowNumber, out int colNumber)
        {
            rowNumber = 0;
            colNumber = 0;

            var splitedName = name.Split('[', ']');

            var numbersStripped = splitedName.Length != 2 && splitedName.Length >= 1
                ? splitedName[1]
                : null;

            var array = numbersStripped?.Length >= 1
                ? numbersStripped.Split(',')
                : null;
            
            if (array != null )
            {
                var isNumberRow = int.TryParse(array[0], out int numberRow);
                rowNumber = isNumberRow && numberRow > rowNumber ? numberRow : rowNumber;

                if (array.Length > 1)
                {
                    var isNumberCol = int.TryParse(array[1], out int numberCol);
                    colNumber = isNumberCol && numberCol > colNumber ? numberCol : colNumber;
                }
            }
        }
    }
}
