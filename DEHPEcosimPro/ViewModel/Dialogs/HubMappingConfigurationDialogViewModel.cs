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
    using CDP4Common.EngineeringModelData;

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;

    using DEHPEcosimPro.DstController;
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
        /// Gets or sets the collection of ElementDefinition that hold parameter value to map
        /// </summary>
        public ReactiveList<ElementDefinitionRowViewModel> Elements { get; set; }

        /// <summary>
        /// Gets or sets the collection of <see cref="MappedElementDefinition"/>
        /// </summary>
        public ReactiveList<MappedElementDefinition> MappedElements { get; set; }

        /// <summary>
        /// Initializes a new <see cref="HubMappingConfigurationDialogViewModel"/>
        /// </summary>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        public HubMappingConfigurationDialogViewModel(IHubController hubController, IDstController dstController) : 
            base(hubController, dstController)
        {
        }
    }
}
