﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IHubMappingConfigurationDialogViewModel.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.ViewModel.Dialogs.Interfaces
{
    using CDP4Common.EngineeringModelData;

    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;

    using DEHPEcosimPro.ViewModel.Rows;

    using ReactiveUI;

    /// <summary>
    /// Interface definition for the <see cref="HubMappingConfigurationDialogViewModel"/>
    /// </summary>
    public interface IHubMappingConfigurationDialogViewModel
    {
        /// <summary>
        /// Gets or sets the collection of available variables
        /// </summary>
        ReactiveList<VariableBaseRowViewModel> AvailableVariables { get; set; }

        /// <summary>
        /// Gets or sets the collection of available variables
        /// </summary>
        VariableBaseRowViewModel SelectedVariable { get; set; }

        /// <summary>
        /// Gets or sets the collection of <see cref="ElementDefinitionRowViewModel"/> that hold parameter value to map
        /// </summary>
        ReactiveList<ElementDefinitionRowViewModel> Elements { get; set; }

        /// <summary>
        /// Gets or sets the collection of <see cref="ElementDefinition"/> that hold parameter value to map
        /// </summary>
        ReactiveList<ElementDefinition> ElementDefinitions { get; }

        /// <summary>
        /// Gets or sets the collection of <see cref="Parameter"/> that hold parameter value to map
        /// </summary>
        ReactiveList<ParameterOrOverrideBase> Parameters { get; set; }

        /// <summary>
        /// Gets or sets the collection of string value
        /// </summary>
        ReactiveList<ValueSetValueRowViewModel> Values { get; set; }

        /// <summary>
        /// Gets the collection of <see cref="MappedElementDefinitionRowViewModel"/>
        /// </summary>
        ReactiveList<MappedElementDefinitionRowViewModel> MappedElements { get; }

        /// <summary>
        /// Gets or sets the selected <see cref="MappedElementDefinitionRowViewModel"/>
        /// </summary>
        MappedElementDefinitionRowViewModel SelectedMappedElement { get; set; }

        /// <summary>
        /// Gets or sets the selected <see cref="IRowViewModelBase{T}"/>
        /// </summary>
        object SelectedThing { get; set; }
    }
}
