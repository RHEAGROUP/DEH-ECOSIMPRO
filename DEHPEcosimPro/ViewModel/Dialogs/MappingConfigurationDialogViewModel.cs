// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappingConfigurationDialogViewModel.cs" company="RHEA System S.A.">
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
    /// The <see cref="MappingConfigurationDialogViewModel"/> is the view model to let the user configure the mapping
    /// </summary>
    public class MappingConfigurationDialogViewModel : ReactiveObject, IMappingConfigurationDialogViewModel, ICloseWindowViewModel
    {
        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// Gets or sets the <see cref="ICloseWindowBehavior"/> instance
        /// </summary>
        public ICloseWindowBehavior CloseWindowBehavior { get; set; }

        /// <summary>
        /// Backing field for <see cref="IsBusy"/>
        /// </summary>
        private bool isBusy;

        /// <summary>
        /// Gets or sets the assert indicating whether the view is busy
        /// </summary>
        public bool IsBusy
        {
            get => this.isBusy;
            set => this.RaiseAndSetIfChanged(ref this.isBusy, value);
        }

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
        /// Gets the <see cref="ICommand"/> to continue
        /// </summary>
        public ReactiveCommand<object> ContinueCommand { get; set; }
        
        /// <summary>
        /// Initializes a new <see cref="MappingConfigurationDialogViewModel"/>
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        public MappingConfigurationDialogViewModel(IHubController hubController, IDstController dstController)
        {
            this.hubController = hubController;
            this.dstController = dstController;
            this.UpdateProperties();
            this.InitializesCommandsAndObservableSubscriptions();
        }

        /// <summary>
        /// Initializes this view model <see cref="ICommand"/> and <see cref="Observable"/>
        /// </summary>
        private void InitializesCommandsAndObservableSubscriptions()
        {
            var canContinue = this.WhenAnyValue(x => x.SelectedThing.SelectedValues.CountChanged)
                .SelectMany(x => x.Select(c => c > 0)).ObserveOn(RxApp.MainThreadScheduler);

            this.Variables.ForEach(x => canContinue.Merge(
                x.WhenAny(v => v.SelectedValues, v => v.Value.Any())
                    .ObserveOn(RxApp.MainThreadScheduler)));

            this.ContinueCommand = ReactiveCommand.Create(canContinue);
            this.ContinueCommand.Subscribe(_ => this.ExecuteContinueCommand());

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
        /// Executes the specified action to update the view Hub fields surrounded by a <see cref="IsBusy"/> state change
        /// </summary>
        /// <param name="updateAction">The <see cref="Action"/> to execute</param>
        private void UpdateHubFields(Action updateAction)
        {
            this.IsBusy = true;
            updateAction.Invoke();
            this.IsBusy = false;
        }

        /// <summary>
        /// Executes the <see cref="ContinueCommand"/>
        /// </summary>
        private void ExecuteContinueCommand()
        {
            this.IsBusy = true;

            try
            {
                if (this.dstController.Map(this.Variables.Where(x => !x.SelectedValues.IsEmpty).ToList()))
                {
                    this.CloseWindowBehavior?.Close();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"{e.Message}");
            }
            finally
            {
                this.IsBusy = false;
            }
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
            this.AvailableElementDefinitions.AddRange(this.hubController.OpenIteration.Element.Where(this.AreTheseOwnedByTheDomain<ElementDefinition>()));

            this.AvailableParameterTypes.AddRange(this.hubController.GetSiteDirectory().SiteReferenceDataLibrary
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
            this.AvailableOptions.AddRange(this.hubController.OpenIteration.Option.Where(x => this.AvailableOptions.All(o => o.Iid != x.Iid)));
            
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
                        this.AreTheseOwnedByTheDomain<ElementUsage>()).Distinct());
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
                    if (this.hubController.GetThingById(idCorrespondence.InternalThing, this.hubController.OpenIteration, out Thing thing))
                    {
                        Action action = thing switch
                        {
                            ElementDefinition elementDefinition => (() => variable.SelectedElementDefinition = elementDefinition),
                            ElementUsage elementUsage => (() => variable.SelectedElementUsages.Add(elementUsage)),
                            Parameter parameter => (() => variable.SelectedParameter = parameter),
                            Option option => (() => variable.SelectedOption = option),
                            ActualFiniteState state => (() => variable.SelectedActualFiniteState = state),
                            _ => null
                        };
                        
                        action?.Invoke();
                        
                        if (action is null && this.hubController.GetThingById(idCorrespondence.InternalThing, out CompoundParameterType parameterType))
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
            => x => x.Owner.Iid == this.hubController.CurrentDomainOfExpertise.Iid;
    }
}
