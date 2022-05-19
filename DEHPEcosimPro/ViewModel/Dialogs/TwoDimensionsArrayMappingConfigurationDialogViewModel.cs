// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TwoDimensionsArrayMappingConfigurationDialogViewModel.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2022 RHEA System S.A.
// 
//    Author: Sam Gerené, Alex Vorobiev, Alexander van Delft, Nathanael Smiechowski, Antoine Théate.
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
    using System.Linq;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.ViewModel.Rows;

    using ReactiveUI;

    /// <summary>
    /// ViewModel of the dialog to set the <see cref="IParameterTypeAssignment"/> to map to the correct column/row
    /// of an <see cref="ArrayVariableRowViewModel"/>
    /// </summary>
    public class TwoDimensionsArrayMappingConfigurationDialogViewModel: ReactiveObject, ICloseWindowViewModel
    {
        /// <summary>
        /// Backing field for <see cref="SelectedItem"/>
        /// </summary>
        private IParameterTypeAssignment selectedItem;

        /// <summary>
        /// Initializes a new instance of the <see cref="TwoDimensionsArrayMappingConfigurationDialogViewModel" /> class.
        /// </summary>
        /// <param name="parameter">The <see cref="ParameterOrOverrideBase"/></param>
        public TwoDimensionsArrayMappingConfigurationDialogViewModel(ParameterOrOverrideBase parameter)
        {
            var sfpt = parameter.ParameterType as SampledFunctionParameterType;
            this.ParameterTypeAssignments.AddRange(sfpt.IndependentParameterType);
            this.ParameterTypeAssignments.AddRange(sfpt.DependentParameterType);

            this.SelectedItem = sfpt.IndependentParameterType.First();
        }

        /// <summary>
        /// Gets or sets the selected item in the <see cref="ParameterTypeAssignments"/> collection
        /// </summary>
        public IParameterTypeAssignment SelectedItem
        {
            get => this.selectedItem;
            set => this.RaiseAndSetIfChanged(ref this.selectedItem, value);
        }

        /// <summary>
        /// A collection of <see cref="IParameterTypeAssignment"/>
        /// </summary>
        public ReactiveList<IParameterTypeAssignment> ParameterTypeAssignments { get; } = new();

        /// <summary>
        /// Gets or sets the behavior instance
        /// </summary>
        public ICloseWindowBehavior CloseWindowBehavior { get; set; }
    }
}
