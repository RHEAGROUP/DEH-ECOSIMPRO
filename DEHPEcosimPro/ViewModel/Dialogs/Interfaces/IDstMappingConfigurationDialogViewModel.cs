// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IDstMappingConfigurationDialogViewModel.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.ViewModel.Dialogs.Interfaces
{
    using System.Reactive.Linq;
    using System.Windows.Input;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using DEHPEcosimPro.Enumerator;
    using DEHPEcosimPro.ViewModel.Rows;

    using ReactiveUI;

    /// <summary>
    /// Interface definition for <see cref="DstMappingConfigurationDialogViewModel"/>
    /// </summary>
    public interface IDstMappingConfigurationDialogViewModel: IMappingConfigurationDialogViewModel
    {
        /// <summary>
        /// Gets or sets the selected row that represents a <see cref="ReferenceDescription"/>
        /// </summary>
        VariableRowViewModel SelectedThing { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="MappingConfigurationDialogViewModel.ContinueCommand"/> can execute
        /// </summary>
        bool CanContinue { get; set; }

        /// <summary>
        /// Gets the collection of the available <see cref="Option"/> from the connected Hub Model
        /// </summary>
        ReactiveList<Option> AvailableOptions { get; }

        /// <summary>
        /// Gets the collection of the available <see cref="ElementDefinition"/>s from the connected Hub Model
        /// </summary>
        ReactiveList<ElementDefinition> AvailableElementDefinitions { get; }

        /// <summary>
        /// Gets the collection of the available <see cref="ElementUsage"/>s from the connected Hub Model
        /// </summary>
        ReactiveList<ElementUsage> AvailableElementUsages { get; }

        /// <summary>
        /// Gets the collection of the available <see cref="ParameterType"/>s from the connected Hub Model
        /// </summary>
        ReactiveList<ParameterType> AvailableParameterTypes { get; }

        /// <summary>
        /// Gets the collection of the available <see cref="Parameter"/>s from the connected Hub Model
        /// </summary>
        ReactiveList<ParameterOrOverrideBase> AvailableParameters { get; }

        /// <summary>
        /// Gets the collection of the available <see cref="ActualFiniteState"/>s depending on the selected <see cref="Parameter"/>
        /// </summary>
        ReactiveList<ActualFiniteState> AvailableActualFiniteStates { get; }

        /// <summary>
        /// Gets the collection of <see cref="VariableRowViewModel"/>
        /// </summary>
        ReactiveList<VariableRowViewModel> Variables { get; }

        /// <summary>
        /// Gets or sets the command that applies the configured time step at the current <see cref="SelectedThing"/>
        /// </summary>
        ReactiveCommand<object> ApplyTimeStepOnSelectionCommand { get; set; }

        /// <summary>
        /// Initializes this view model properties
        /// </summary>
        void Initialize();

        /// <summary>
        /// Initializes this view model <see cref="ICommand"/> and <see cref="Observable"/>
        /// </summary>
        void InitializesCommandsAndObservableSubscriptions();

        /// <summary>
        /// Verify that the selected <see cref="ParameterType"/> is compatible with the selected variable value type
        /// </summary>
        void NotifyIfParameterTypeIsNotAllowed();

        /// <summary>
        /// Sets the <see cref="DstMappingConfigurationDialogViewModel.SelectedThing"/> <see cref="ParameterType"/> according to the selected <see cref="Parameter"/>
        /// </summary>
        void UpdateSelectedParameterType();

        /// <summary>
        /// Sets the <see cref="DstMappingConfigurationDialogViewModel.SelectedThing"/> <see cref="MeasurementScale"/> according to the selected <see cref="Parameter"/> and the selected <see cref="ParameterType"/>
        /// </summary>
        void UpdateSelectedScale();

        /// <summary>
        /// Sets the <see cref="DstMappingConfigurationDialogViewModel.SelectedThing"/> <see cref="Parameter"/> according to the selected <see cref="ParameterType"/>
        /// </summary>
        void UpdateSelectedParameter();

        /// <summary>
        /// Updates the <see cref="DstMappingConfigurationDialogViewModel.AvailableParameterTypes"/>
        /// </summary>
        /// <param name="allowScalarParameterType">A value indicating whether the <see cref="ScalarParameterType"/>s should be included in the <see cref="DstMappingConfigurationDialogViewModel.AvailableParameterTypes"/></param>
        void UpdateAvailableParameterType(bool? allowScalarParameterType = null);

        /// <summary>
        /// Updates the mapping based on the available 10-25 elements
        /// </summary>
        void UpdatePropertiesBasedOnMappingConfiguration();
    }
}
