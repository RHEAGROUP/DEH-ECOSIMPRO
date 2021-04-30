// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstMappingConfigurationDialogViewModel.cs" company="RHEA System S.A.">
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
    using System.Linq;
    using System.Reactive.Linq;
    using System.Windows.Input;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Extensions;
    using DEHPEcosimPro.ViewModel.Dialogs.Interfaces;
    using DEHPEcosimPro.ViewModel.Rows;
    using DEHPEcosimPro.Views.Dialogs;

    using Opc.Ua;

    using ReactiveUI;

    using INavigationService = DEHPCommon.Services.NavigationService.INavigationService;

    /// <summary>
    /// The <see cref="DstMappingConfigurationDialogViewModel"/> is the view model to let the user configure the mapping to the hub source
    /// </summary>
    public class DstMappingConfigurationDialogViewModel : MappingConfigurationDialogViewModel, IDstMappingConfigurationDialogViewModel
    {
        /// <summary>
        /// The <see cref="INavigationService"/>
        /// </summary>
        private readonly INavigationService navigation;

        /// <summary>
        /// Backing field for <see cref="SelectedThing"/>
        /// </summary>
        private VariableRowViewModel selectedThing;

        /// <summary>
        /// Gets or sets the selected row that represents a <see cref="ReferenceDescription"/>
        /// </summary>
        public VariableRowViewModel SelectedThing
        {
            get => this.selectedThing;
            set
            {
                if (this.IsParameterTypeAllowed())
                {
                    this.RaiseAndSetIfChanged(ref this.selectedThing, value);
                }
            }
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
        /// Gets the collection of the available <see cref="Option"/> from the connected Hub Model
        /// </summary>
        public ReactiveList<Option> AvailableOptions { get; } = new ReactiveList<Option>();

        /// <summary>
        /// Gets the collection of the available <see cref="ElementDefinition"/>s from the connected Hub Model
        /// </summary>
        public ReactiveList<ElementDefinition> AvailableElementDefinitions { get; } = new ReactiveList<ElementDefinition>();

        /// <summary>
        /// Gets the collection of the available <see cref="ElementUsage"/>s from the connected Hub Model
        /// </summary>
        public ReactiveList<ElementUsage> AvailableElementUsages { get; } = new ReactiveList<ElementUsage>();

        /// <summary>
        /// Gets the collection of the available <see cref="ParameterType"/>s from the connected Hub Model
        /// </summary>
        public ReactiveList<ParameterType> AvailableParameterTypes { get; } = new ReactiveList<ParameterType>();

        /// <summary>
        /// Gets the collection of the available <see cref="Parameter"/>s from the connected Hub Model
        /// </summary>
        public ReactiveList<ParameterOrOverrideBase> AvailableParameters { get; } = new ReactiveList<ParameterOrOverrideBase>();
        
        /// <summary>
        /// Gets the collection of the available <see cref="ActualFiniteState"/>s depending on the selected <see cref="Parameter"/>
        /// </summary>
        public ReactiveList<ActualFiniteState> AvailableActualFiniteStates { get; } = new ReactiveList<ActualFiniteState>();

        /// <summary>
        /// Gets the collection of <see cref="VariableRowViewModel"/>
        /// </summary>
        public ReactiveList<VariableRowViewModel> Variables { get; } = new ReactiveList<VariableRowViewModel>();
        
        /// <summary>
        /// Gets or sets the command that applies the configured time step at the current <see cref="SelectedThing"/>
        /// </summary>
        public ReactiveCommand<object> ApplyTimeStepOnSelectionCommand { get; set; }

        /// <summary>
        /// Initializes a new <see cref="DstMappingConfigurationDialogViewModel"/>
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel"/></param>
        /// <param name="navigation">The <see cref="INavigationService"/></param>
        public DstMappingConfigurationDialogViewModel(IHubController hubController, IDstController dstController, 
            IStatusBarControlViewModel statusBar, INavigationService navigation) :
                base(hubController, dstController, statusBar)
        {
            this.navigation = navigation;
        }

        /// <summary>
        /// Initializes this view model properties
        /// </summary>
        public void Initialize()
        {
            this.UpdateProperties();

            this.WhenAny(x => x.Variables.CountChanged,
                    x => x.Value.Where(c => c > 0))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(() =>
                    {
                        this.InitializesCommandsAndObservableSubscriptions();
                        this.AssignMapping();
                        this.UpdatePropertiesBasedOnMappingConfiguration();
                        this.CheckCanExecute();
                    }));
        }

        /// <summary>
        /// Assings a mapping configuration if any to each of the selected variables
        /// </summary>
        private void AssignMapping()
        {
            foreach (var variable in this.Variables)
            {
                variable.MappingConfigurations.AddRange(
                    this.DstController.ExternalIdentifierMap.Correspondence.Where(
                        x => x.ExternalId == variable.ElementName ||
                             x.ExternalId == variable.Name));
            }
        }

        /// <summary>
        /// Initializes this view model <see cref="ICommand"/> and <see cref="Observable"/>
        /// </summary>
        public void InitializesCommandsAndObservableSubscriptions()
        {
            foreach (var variableRowViewModel in this.Variables)
            {
                variableRowViewModel.SelectedValues.CountChanged
                    .Subscribe(n =>
                        this.UpdateHubFields(() =>
                        {
                            this.UpdateAvailableParameters();
                            this.UpdateAvailableParameterType(n < 2);
                            this.CheckCanExecute();
                        }));
            }

            this.ContinueCommand = ReactiveCommand.Create(
                this.WhenAnyValue(x => x.CanContinue),
                RxApp.MainThreadScheduler);

            this.ContinueCommand.Subscribe(_ => this.ExecuteContinueCommand(
                () =>
                {
                    var variableRowViewModels = this.Variables.Where(x => !x.SelectedValues.IsEmpty).ToList();
                    this.DstController.Map(variableRowViewModels);
                    this.StatusBar.Append($"Mapping in progress of {variableRowViewModels.Count} value(s)...");
                }));

            this.WhenAnyValue(x => x.SelectedThing.SelectedElementDefinition)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(() =>
                {
                    this.UpdateAvailableParameters();
                    this.UpdateAvailableParameterType();
                    this.UpdateAvailableElementUsages();
                    this.CheckCanExecute();
                }));

            this.WhenAnyValue(x => x.SelectedThing.SelectedParameterType)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(() =>
                {
                    this.UpdateSelectedParameter();
                    this.UpdateSelectedScale();
                    this.CheckCanExecute();
                    }));
            
            this.WhenAnyValue(x => x.SelectedThing.SelectedParameter)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(() =>
                {
                    this.UpdateSelectedParameterType();
                    this.UpdateAvailableActualFiniteStates();
                    this.CheckCanExecute();
                }));

            this.ApplyTimeStepOnSelectionCommand = ReactiveCommand.Create();

            this.ApplyTimeStepOnSelectionCommand.Subscribe(_ =>
                this.UpdateHubFields(() => this.SelectedThing?.ApplyTimeStep()));
        }

        /// <summary>
        /// Checks that any of the <see cref="Variables"/> has at least one value selected
        /// </summary>
        private void CheckCanExecute()
        {
            this.CanContinue = this.Variables.Any(x => x.IsValid());
        }
        
        /// <summary>
        /// Update this view model properties
        /// </summary>
        private void UpdateProperties()
        {
            this.IsBusy = true;
            this.UpdateAvailableOptions();
            this.AvailableElementDefinitions.Clear();

            this.AvailableElementDefinitions.AddRange(
                this.HubController.OpenIteration.Element
                    .Select(e => e.Clone(true))
                    .OrderBy(x => x.Name));
            
            this.UpdateAvailableParameterType();
            this.UpdateAvailableParameters();
            this.UpdateAvailableElementUsages();
            this.UpdateAvailableActualFiniteStates();

            this.IsBusy = false;
        }

        /// <summary>
        /// Verify that the selected <see cref="ParameterType"/> is compatible with the selected variable value type
        /// </summary>
        /// <returns>A value indicating whether the current <see cref="SelectedThing"/> <see cref="ParameterType"/>is compatible with </returns>
        public bool IsParameterTypeAllowed()
        {
            if (this.SelectedThing?.SelectedParameterType == null
                || (this.SelectedThing?.SelectedParameterType is {}
                && this.SelectedThing.IsParameterTypeValid()))
            {
                return true;
            }

            this.StatusBar.Append($"The selected ParameterType isn't compatible with the selected variable", StatusBarMessageSeverity.Error);
            this.navigation.ShowDxDialog<MappingValidationErrorDialog>();
            return false;
        }

        /// <summary>
        /// Sets the <see cref="SelectedThing"/> <see cref="ParameterType"/> according to the selected <see cref="Parameter"/>
        /// </summary>
        public void UpdateSelectedParameterType()
        {
            if (this.SelectedThing?.SelectedParameter is null)
            {
                return;
            }

            this.SelectedThing.SelectedParameterType = this.SelectedThing.SelectedParameter.ParameterType;
        }

        /// <summary>
        /// Sets the <see cref="SelectedThing"/> <see cref="MeasurementScale"/> according to the selected <see cref="Parameter"/> and the selected <see cref="ParameterType"/>
        /// </summary>
        public void UpdateSelectedScale()
        {
            if (this.SelectedThing?.SelectedParameterType is null)
            {
                return;
            }

            if (this.SelectedThing.SelectedParameter is {} parameter)
            {
                this.SelectedThing.SelectedScale = parameter.Scale;
                return;
            }
                
            this.SelectedThing.SelectedScale = 
                this.SelectedThing.SelectedParameterType is QuantityKind quantityKind 
                ? quantityKind.DefaultScale 
                : null;
        }

        /// <summary>
        /// Sets the <see cref="SelectedThing"/> <see cref="Parameter"/> according to the selected <see cref="ParameterType"/>
        /// </summary>
        public void UpdateSelectedParameter()
        {
            if (this.SelectedThing?.SelectedParameterType is null)
            {
                return;
            }

            if (this.SelectedThing.SelectedParameter is { } parameter
                && this.SelectedThing.SelectedParameterType is { } parameterType 
                    && parameter.ParameterType.Iid != parameterType.Iid)
            {
                this.SelectedThing.SelectedParameter = null;
                this.UpdateAvailableParameterType();
            }
            else if(this.AvailableParameters.FirstOrDefault(x => 
                x.ParameterType.Iid == this.SelectedThing.SelectedParameterType?.Iid) is Parameter parameterOrOverride )
            {
                this.SelectedThing.SelectedParameter = parameterOrOverride;
            }

            if (this.SelectedThing.SelectedParameter?.IsOptionDependent is true)
            {
                this.SelectedThing.SelectedOption = this.AvailableOptions.FirstOrDefault();
            }
        }

        /// <summary>
        /// Updates the <see cref="AvailableParameterTypes"/>
        /// </summary>
        /// <param name="allowScalarParameterType">A value indicating whether the <see cref="ScalarParameterType"/>s should be included in the <see cref="AvailableParameterTypes"/></param>
        public void UpdateAvailableParameterType(bool? allowScalarParameterType = null)
        {
            this.AvailableParameterTypes.Clear();

            allowScalarParameterType ??= !(this.SelectedThing?.SelectedValues.Count > 1);

            var parameterTypes = 
                this.HubController.OpenIteration
                    .GetContainerOfType<EngineeringModel>().RequiredRdls
                    .SelectMany(x => x.ParameterType);

            if (allowScalarParameterType != true)
            {
                parameterTypes = parameterTypes.OfType<SampledFunctionParameterType>();
            }

            var filteredParameterTypes = parameterTypes
                .Where(this.FilterParameterType)
                .OrderBy(x => x.Name);

                this.AvailableParameterTypes.AddRange(filteredParameterTypes);
        }

        /// <summary>
        /// Verify if the <paramref name="parameterType"/> is <see cref="ScalarParameterType"/>
        /// or is <see cref="SampledFunctionParameterType"/> and at least compatible with this dst adapter
        /// </summary>
        /// <param name="parameterType">The <see cref="ParameterType"/></param>
        /// <returns>An value indicating whether the <paramref name="parameterType"/> is valid</returns>
        private bool FilterParameterType(ParameterType parameterType)
        {
            return parameterType switch
            {
                SampledFunctionParameterType _ when this.SelectedThing is null => true,
                SampledFunctionParameterType sampledFunctionParameterType => 
                    sampledFunctionParameterType.Validate(this.SelectedThing?.ActualValue, this.SelectedThing?.SelectedScale),
                ScalarParameterType _ => true,
                _ => false
            };
        }

        /// <summary>
        /// Updates the <see cref="AvailableActualFiniteStates"/>
        /// </summary>
        private void UpdateAvailableActualFiniteStates()
        {
            this.AvailableActualFiniteStates.Clear();

            if (this.SelectedThing?.SelectedParameter is { } parameter && parameter.StateDependence is { } stateDependence)
            {
                this.AvailableActualFiniteStates.AddRange(stateDependence.ActualState);
                this.SelectedThing.SelectedActualFiniteState = this.AvailableActualFiniteStates.FirstOrDefault();
            }
        }

        /// <summary>
        /// Updates the <see cref="AvailableOptions"/> collection
        /// </summary>
        private void UpdateAvailableOptions()
        {
            this.AvailableOptions.AddRange(this.HubController.OpenIteration.Option.Where(x => this.AvailableOptions.All(o => o.Iid != x.Iid)));
        }

        /// <summary>
        /// Updates the available <see cref="Parameter"/>s for the <see cref="VariableRowViewModel.SelectedElementDefinition"/>
        /// </summary>
        private void UpdateAvailableParameters()
        {
            this.AvailableParameters.Clear();

            if (!(this.SelectedThing?.SelectedElementDefinition is { } element))
            {
                return;
            }

            var parameters = element.Parameter.ToList();
                
            if(element.Iid != Guid.Empty)
            {
                parameters = parameters.Where(x => this.HubController.Session.PermissionService.CanWrite(x)).ToList();
            }

            if (this.SelectedThing.SelectedValues.Count > 1)
            {
                this.AvailableParameters.AddRange(parameters.Where(
                    p=> this.IsSampledFunctionParameterTypeAndIsCompatible(p.ParameterType, this.SelectedThing.ActualValue, this.SelectedThing.SelectedScale)));
            }
            else
            {
                this.AvailableParameters.AddRange(parameters.Where(x => this.FilterParameterType(x.ParameterType)));
            }
        }

        /// <summary>
        /// Verify that the <see cref="ParameterType"/> is <see cref="SampledFunctionParameterType"/>
        /// and that it is compatible
        /// </summary>
        /// <param name="parameterType">The <see cref="ParameterType"/> to verify</param>
        /// <param name="value">The <see cref="object"/> value</param>
        /// <param name="scale">The <see cref="MeasurementScale"/></param>
        /// <returns>A value indicating whether the <paramref name="parameterType"/> complies</returns>
        private bool IsSampledFunctionParameterTypeAndIsCompatible(ParameterType parameterType, object value, MeasurementScale scale = null)
        {
            return parameterType is SampledFunctionParameterType sampledFunctionParameterType
                   && sampledFunctionParameterType.Validate(value, scale);
        }

        /// <summary>
        /// Updates the <see cref="AvailableElementUsages"/>
        /// </summary>
        private void UpdateAvailableElementUsages()
        {
            this.AvailableElementUsages.Clear();

            if (this.SelectedThing?.SelectedElementDefinition is { } elementDefinition && elementDefinition.Iid != Guid.Empty)
            {
                var elementUsages = this.AvailableElementDefinitions.SelectMany(d => d.ContainedElement)
                    .Where(u => u.ElementDefinition.Iid == elementDefinition.Iid);

                if (this.SelectedThing.SelectedOption is { } option)
                {
                    elementUsages = elementUsages.Where(x => !x.ExcludeOption.Contains(option));
                }

                this.AvailableElementUsages.AddRange(elementUsages.Select(x => x.Clone(true)));
            }
        }
        
        /// <summary>
        /// Updates the mapping based on the available 10-25 elements
        /// </summary>
        public void UpdatePropertiesBasedOnMappingConfiguration()
        {
            this.IsBusy = true;

            foreach (var variable in this.Variables)
            {
                foreach (var idCorrespondence in variable.MappingConfigurations)
                {
                    if (!this.HubController.GetThingById(idCorrespondence.InternalThing, this.HubController.OpenIteration, out Thing thing))
                    {
                        continue;
                    }

                    Action action = thing switch
                    {
                        ElementDefinition elementDefinition => () => variable.SelectedElementDefinition = 
                            this.AvailableElementDefinitions.FirstOrDefault(x => x.Iid == thing.Iid),

                        ElementUsage elementUsage => () =>
                        {
                            if (this.AvailableElementDefinitions.SelectMany(e => e.ContainedElement)
                                .FirstOrDefault(x => x.Iid == thing.Iid) is {} usage)
                            {
                                variable.SelectedElementUsages.Add(usage);
                            }
                        },

                        Parameter parameter => () => variable.SelectedParameter = 
                            this.AvailableElementDefinitions.SelectMany(e => e.Parameter)
                                .FirstOrDefault(p => p.Iid == thing.Iid),

                        Option option => () => variable.SelectedOption = option,

                        ActualFiniteState state => () => variable.SelectedActualFiniteState = state,

                        _ => null
                    };
                        
                    action?.Invoke();

                    if (action is null &&
                        this.HubController.GetThingById(idCorrespondence.InternalThing, this.HubController.OpenIteration, 
                            out SampledFunctionParameterType parameterType))
                    {
                        variable.SelectedParameterType = parameterType;
                    }
                }
            }

            this.IsBusy = false;
        }
    }
}
