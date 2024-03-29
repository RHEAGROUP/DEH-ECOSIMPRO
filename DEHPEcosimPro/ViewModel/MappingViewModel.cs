﻿// --------------------------------------------------------------------------------------------------------------------
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
    using System.Collections.Generic;
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
        /// Gets or sets the collection of <see cref="MappingRowViewModel"/>
        /// </summary>
        public ReactiveList<MappingRowViewModel> MappingRows { get; set; } = new ();

        /// <summary>
        /// Gets or sets the selected <see cref="MappingRowViewModel"/>
        /// </summary>
        public ReactiveList<MappingRowViewModel> SelectedRows { get; set; } = new ();

        /// <summary>
        /// Initializes a new <see cref="MappingViewModel"/>
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="hubController">The <see cref="IHubController"/>"/></param>
        public MappingViewModel(IDstController dstController, IHubController hubController)
        {
            this.dstController = dstController;
            this.hubController = hubController;

            this.dstController.DstMapResult.ItemsAdded.Subscribe(this.UpdateMappedThings);

            this.dstController.DstMapResult.IsEmptyChanged.Where(x => !x).Subscribe(_ => this.ComputeDstMapResult());

            this.dstController.DstMapResult.IsEmptyChanged.Where(x => x).Subscribe(_ =>
                this.MappingRows.RemoveAll(this.MappingRows
                        .Where(x => x.Direction == MappingDirection.FromDstToHub).ToList()));

            this.dstController.HubMapResult.IsEmptyChanged.Where(x => !x).Subscribe(_ => this.ComputeHubMapResult());

            this.dstController.HubMapResult.IsEmptyChanged.Where(x => x).Subscribe(_ => 
                this.MappingRows.RemoveAll(this.MappingRows
                        .Where(x => x.Direction == MappingDirection.FromHubToDst).ToList()));

            this.dstController.HubMapResult.ItemsAdded.Subscribe(this.UpdateMappedThings);

            this.WhenAnyValue(x => x.dstController.MappingDirection)
                .Subscribe(this.UpdateMappingRowsDirection);
        }

        /// <summary>
        /// Updates the row according to the new <see cref="IDstController.MappingDirection"/>
        /// </summary>
        /// <param name="mappingDirection"></param>
        public void UpdateMappingRowsDirection(MappingDirection mappingDirection)
        {
            foreach (var mappingRowViewModel in this.MappingRows)
            {
                mappingRowViewModel.UpdateDirection(mappingDirection);
            }
        }

        /// <summary>
        /// Computes rows to represents <see cref="IDstController.HubMapResult"/>
        /// </summary>
        private void ComputeHubMapResult()
        {
            foreach (var mappedElementDefinitionRowViewModel in this.dstController.HubMapResult)
            {
                this.UpdateMappedThings(mappedElementDefinitionRowViewModel);
            }
        }

        /// <summary>
        /// Computes rows to represents <see cref="IDstController.DstMapResult"/>
        /// </summary>
        private void ComputeDstMapResult()
        {
            foreach (var mappedElementDefinitionRowViewModel in this.dstController.DstMapResult)
            {
                this.UpdateMappedThings(mappedElementDefinitionRowViewModel);
            }
        }

        /// <summary>
        /// Updates the <see cref="MappingRows"/>
        /// </summary>
        /// <param name="mappedElement">The <see cref="MappedElementDefinitionRowViewModel"/></param>
        private void UpdateMappedThings(MappedElementDefinitionRowViewModel mappedElement)
        {
            this.MappingRows.RemoveAll(this.MappingRows
                .Where(m => m.HubThing.Name == mappedElement.SelectedParameter.ModelCode()
                            && m.DstThing.Name == mappedElement.SelectedVariable.Name
                            && m.Direction == MappingDirection.FromHubToDst).ToList());

            this.MappingRows.Add(new MappingRowViewModel(this.dstController.MappingDirection, mappedElement));
        }

        /// <summary>
        /// Updates the <see cref="MappingRows"/>
        /// </summary>
        /// <param name="element">The <see cref="ElementBase"/></param>
        private void UpdateMappedThings(ElementBase element)
        {
            var parametersNodeId = element switch
            {
                ElementDefinition elementDefinition => this.GetParameters(elementDefinition),
                ElementUsage elementUsage => this.GetParameters(elementUsage),
                _ => new List<(ParameterOrOverrideBase parameter, VariableRowViewModel variable)>()
            };

            foreach (var parameterVariable in parametersNodeId)
            {
                this.MappingRows.RemoveAll(this.MappingRows.Where(m => m.DstThing.Name == parameterVariable.variable.Name).ToList());
                this.MappingRows.Add(new MappingRowViewModel(this.dstController.MappingDirection, parameterVariable));
            }
        }

        /// <summary>
        /// Queries the parameters in the <see cref="IDstController.ParameterVariable"/> with their associated <see cref="Opc.Ua.NodeId"/> and a collection of their original references
        /// </summary>
        /// <param name="element">The <see cref="ElementDefinition"/></param>
        /// <returns>A List{(ParameterOrOverrideBase parameter, object variable)}</returns>
        private List<(ParameterOrOverrideBase parameter, VariableRowViewModel variable)> GetParameters(ElementDefinition element)
        {
            var modified  = this.dstController
                        .ParameterVariable.Where(x =>
                            x.Key.Container is ElementDefinition elementDefinition 
                            && elementDefinition.Iid == element.Iid 
                            && elementDefinition.ShortName == element.ShortName).ToList();

            var originals = this.hubController.OpenIteration.Element
                .FirstOrDefault(x => x.Iid == element.Iid)?
                .Parameter.Where(x => modified
                    .Select(o => o.Key)
                    .Any(p => p.Iid == x.Iid)).ToList() 
                            ?? modified.Select(x => x.Key).OfType<Parameter>().ToList();

            var result = 
                originals.Select(parameterOverride => (parameterOverride as ParameterOrOverrideBase, 
                    modified.FirstOrDefault(p => p.Key.Iid == parameterOverride.Iid 
                                                 && p.Key.ParameterType.Name == parameterOverride.ParameterType.Name).Value)).ToList();

            result.AddRange(modified.Where(x => originals.All(o => o.Iid != x.Key.Iid))
                .Select(x => (x.Key, x.Value)));

            return result;
        }

        /// <summary>
        /// Queries the parameters in the <see cref="IDstController.ParameterVariable"/> with their associated <see cref="Opc.Ua.NodeId"/> and a collection of their original references
        /// </summary>
        /// <param name="element">The <see cref="ElementUsage"/></param>
        /// <returns>A List{(ParameterOrOverrideBase parameter, object variable)}</returns>
        private List<(ParameterOrOverrideBase parameter, VariableRowViewModel variable)> GetParameters(ElementUsage element)
        {
            var result = new List<(ParameterOrOverrideBase, VariableRowViewModel)>();

            var modified = this.dstController
                        .ParameterVariable.Where(x =>
                            x.Key.Container.Iid == element.Iid).ToList();

            var originals = this.hubController.OpenIteration.Element
                .FirstOrDefault(x => x.Iid == element.ElementDefinition.Iid)?
                .ReferencingElementUsages().FirstOrDefault(x => x.Iid == element.Iid)?.ParameterOverride
                .Where(p => modified.Any(x => p.Iid == x.Key.Iid))
                            ?? new List<ParameterOverride>();

            foreach (var parameterOverride in originals)
            {
                result.Add((parameterOverride, modified.FirstOrDefault(p => p.Key.Iid == parameterOverride.Iid).Value));
            }

            return result;
        }
    }
}
