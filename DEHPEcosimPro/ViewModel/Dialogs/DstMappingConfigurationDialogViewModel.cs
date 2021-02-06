// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstDstMappingConfigurationDialogViewModel.cs" company="RHEA System S.A.">
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
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel.Dialogs.Interfaces;
    using DEHPEcosimPro.ViewModel.Rows;

    using DevExpress.Mvvm.Native;

    using Opc.Ua;

    using ReactiveUI;

    /// <summary>
    /// The <see cref="DstMappingConfigurationDialogViewModel"/> is the view model to let the user configure the mapping to the hub source
    /// </summary>
    public class DstMappingConfigurationDialogViewModel : MappingConfigurationDialogViewModel, IDstMappingConfigurationDialogViewModel
    {
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
        public ReactiveList<Parameter> AvailableParameters { get; } = new ReactiveList<Parameter>();
        
        /// <summary>
        /// Gets the collection of the available <see cref="ActualFiniteState"/>s depending on the selected <see cref="Parameter"/>
        /// </summary>
        public ReactiveList<ActualFiniteState> AvailableActualFiniteStates { get; } = new ReactiveList<ActualFiniteState>();

        /// <summary>
        /// Gets the collection of <see cref="VariableRowViewModel"/>
        /// </summary>
        public ReactiveList<VariableRowViewModel> Variables { get; } = new ReactiveList<VariableRowViewModel>();
        
        /// <summary>
        /// Gets or sets all the selected values from <see cref="VariableRowViewModel.SelectedValues"/>
        /// </summary>
        public ReactiveList<object> SelectedValuesVariable { get; set; }

        /// <summary>
        /// Initializes a new <see cref="DstMappingConfigurationDialogViewModel"/>
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel"/></param>
        public DstMappingConfigurationDialogViewModel(IHubController hubController, IDstController dstController, 
            IStatusBarControlViewModel statusBar) :
                base(hubController, dstController, statusBar)
        {
            this.UpdateProperties();
        }

        /// <summary>
        /// Initializes this view model <see cref="ICommand"/> and <see cref="Observable"/>
        /// </summary>
        public void InitializesCommandsAndObservableSubscriptions()
        {
            foreach (var variableRowViewModel in this.Variables)
            {
                variableRowViewModel.SelectedValues.CountChanged.Subscribe(_ => this.CheckCanExecute());
            }

            this.ContinueCommand = ReactiveCommand.Create(this.WhenAnyValue(x => x.CanContinue));

            this.ContinueCommand.Subscribe(_ => this.ExecuteContinueCommand(
                () =>
                {
                    var variableRowViewModels = this.Variables.Where(x => !x.SelectedValues.IsEmpty).ToList();
                    this.DstController.Map(variableRowViewModels);

                    Application.Current.Dispatcher.Invoke(() => this.StatusBar.Append($"Mapping in progress of {variableRowViewModels.Count} value(s)..."));
                }));

            this.WhenAnyValue(x => x.SelectedThing.SelectedOption)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateProperties());

            this.WhenAnyValue(x => x.SelectedThing.SelectedElementDefinition)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(() =>
                {
                    this.UpdateAvailableParameters();
                    this.UpdateAvailableElementUsages();
                }));

            this.WhenAnyValue(x => x.SelectedThing.SelectedParameter)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateHubFields(this.UpdateAvailableActualFiniteStates));
        }

        /// <summary>
        /// Checks that any of the <see cref="Variables"/> has at least one value selected
        /// </summary>
        private void CheckCanExecute()
        {
            this.CanContinue = this.Variables.Any(x => x.SelectedValues.Any());
        }
        
        /// <summary>
        /// Update this view model properties
        /// </summary>
        private void UpdateProperties()
        {
            this.IsBusy = true;
            this.UpdateAvailableOptions();
            this.AvailableElementDefinitions.Clear();
            this.AvailableParameterTypes.Clear();
            this.AvailableElementDefinitions.AddRange(this.HubController.OpenIteration.Element.Where(this.AreTheseOwnedByTheDomain<ElementDefinition>()));

            this.AvailableParameterTypes.AddRange(this.HubController.GetSiteDirectory().SiteReferenceDataLibrary
                .SelectMany(x => x.ParameterType).Where(
                    x => x is CompoundParameterType parameterType 
                         && parameterType.Component.Count == 2 
                         && parameterType.Component.Count(
                             x => x.ParameterType is DateTimeParameterType) == 1)
                .OrderBy(x => x.Name));

            this.UpdateAvailableParameters();
            this.UpdateAvailableElementUsages();
            this.UpdateAvailableActualFiniteStates();

            this.IsBusy = false;
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
            
            this.Variables.ForEach(x =>
            {
                if (x.SelectedOption is null)
                {
                    x.SelectedOption = this.AvailableOptions.Last();
                }
            });
        }

        /// <summary>
        /// Updates the available <see cref="Parameter"/>s for the <see cref="VariableRowViewModel.SelectedElementDefinition"/>
        /// </summary>
        private void UpdateAvailableParameters()
        {
            this.AvailableParameters.Clear();

            if (this.selectedThing?.SelectedElementDefinition != null)
            {
                this.AvailableParameters.AddRange(this.SelectedThing.SelectedElementDefinition.Parameter.Where(this.AreTheseOwnedByTheDomain<Parameter>()));
            }
        }

        /// <summary>
        /// Updates the <see cref="AvailableElementUsages"/>
        /// </summary>
        private void UpdateAvailableElementUsages()
        {
            this.AvailableElementUsages.Clear();

            if (this.selectedThing?.SelectedElementDefinition != null)
            {
                this.AvailableElementUsages.AddRange(
                    this.selectedThing.SelectedElementDefinition.ContainedElement.Where(
                        this.AreTheseOwnedByTheDomain<ElementUsage>()).Distinct().Select(x => x.Clone(true)));
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
                    if (this.HubController.GetThingById(idCorrespondence.InternalThing, this.HubController.OpenIteration, out Thing thing))
                    {
                        Action action = thing switch
                        {
                            ElementDefinition elementDefinition => (() => variable.SelectedElementDefinition = elementDefinition.Clone(true)),
                            ElementUsage elementUsage => (() => variable.SelectedElementUsages.Add(elementUsage.Clone(true))),
                            Parameter parameter => (() => variable.SelectedParameter = parameter.Clone(false)),
                            Option option => (() => variable.SelectedOption = option),
                            ActualFiniteState state => (() => variable.SelectedActualFiniteState = state),
                            _ => null
                        };
                        
                        action?.Invoke();
                        
                        if (action is null && this.HubController.GetThingById(idCorrespondence.InternalThing, out SampledFunctionParameterType parameterType))
                        {
                            variable.SelectedParameterType = parameterType;
                        }
                    }
                }
            }

            this.IsBusy = false;
        }

        /// <summary>
        /// Verify that the <see cref="IOwnedThing"/> is owned by the current domain of expertise
        /// </summary>
        /// <typeparam name="T">The <see cref="IOwnedThing"/> type</typeparam>
        /// <returns>A <see cref="Func{T,T}"/> input parameter is <see cref="IOwnedThing"/> and outputs an assert whether the verification return true </returns>
        private Func<T, bool> AreTheseOwnedByTheDomain<T>() where T : IOwnedThing 
            => x => x.Owner.Iid == this.HubController.CurrentDomainOfExpertise.Iid;
    }
}
