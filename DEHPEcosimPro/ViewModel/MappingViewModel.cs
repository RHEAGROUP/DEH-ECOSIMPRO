// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappingViewModel.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.ViewModel
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;

    using CDP4Common.EngineeringModelData;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel.Interfaces;
    using DEHPEcosimPro.ViewModel.Rows;
    
    using ReactiveUI;

    /// <summary>
    /// View Model for showing mapped things in the main window
    /// </summary>
    public class MappingViewModel : ReactiveObject, IMappingViewModel
    {
        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IDstVariablesControlViewModel"/>
        /// </summary>
        private readonly IDstVariablesControlViewModel dstVariablesControlViewModel;

        /// <summary>
        /// Gets or sets the collection of <see cref="MappingRows"/>
        /// </summary>
        public ReactiveList<MappingRowViewModel> MappingRows { get; set; } = new ReactiveList<MappingRowViewModel>();

        /// <summary>
        /// Initializes a new <see cref="MappingViewModel"/>
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="dstVariablesControlViewModel">The <see cref="IDstVariablesControlViewModel"/></param>
        public MappingViewModel(IDstController dstController, IHubController hubController, IDstVariablesControlViewModel dstVariablesControlViewModel)
        {
            this.dstController = dstController;
            this.hubController = hubController;
            this.dstVariablesControlViewModel = dstVariablesControlViewModel;

            this.dstController.DstMapResult.ItemsAdded.Subscribe(this.UpdateMappedThings);

            this.dstController.DstMapResult.IsEmptyChanged.Where(x => x).Subscribe(_ =>
                this.MappingRows.RemoveAll(this.MappingRows
                        .Where(x => x.Direction == MappingDirection.FromDstToHub)));

            this.dstController.HubMapResult.ItemsAdded.Subscribe(this.UpdateMappedThings);
            
            this.dstController.DstMapResult.IsEmptyChanged.Where(x => x).Subscribe(_ => 
                this.MappingRows.RemoveAll(this.MappingRows
                        .Where(x => x.Direction == MappingDirection.FromHubToDst)));
        }

        /// <summary>
        /// Updates the <see cref="MappingRows"/>
        /// </summary>
        /// <param name="element">The <see cref="ElementBase"/></param>
        private void UpdateMappedThings(ElementDefinition element)
        {
            var parameters = this.dstController
                .ParameterNodeIds.Where(x =>
                    x.Key.GetContainerOfType<ElementDefinition>().Iid == element.Iid).ToList();

            var originals = this.hubController.OpenIteration.Element
                .SelectMany(x => x.Parameter)
                .Where(x => parameters
                    .Select(o => o.Key)
                    .Any(p => p.Iid == x.Iid));

            foreach (var parameter in originals)
            {
                var nodeId = parameters.FirstOrDefault(x => x.Key.Iid == parameter.Iid).Value;
                
                this.MappingRows.Add(new MappingRowViewModel(parameter, 
                    this.dstVariablesControlViewModel.Variables.FirstOrDefault(x => x.Reference.NodeId.Identifier == nodeId)));
            }
        }

        /// <summary>
        /// Updates the <see cref="MappingRows"/>
        /// </summary>
        /// <param name="mappedElement">The <see cref="MappedElementDefinitionRowViewModel"/></param>
        private void UpdateMappedThings(MappedElementDefinitionRowViewModel mappedElement)
        {
            this.MappingRows.Add(new MappingRowViewModel(mappedElement));
        }
    }
}
