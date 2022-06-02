// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IMappingConfigurationService.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Services.MappingConfiguration
{
    using System;
    using System.Collections.Generic;

    using CDP4Common.EngineeringModelData;

    using CDP4Dal.Operations;

    using DEHPCommon.Enumerators;

    using DEHPEcosimPro.ViewModel.Rows;

    /// <summary>
    /// The <see cref="IMappingConfigurationService"/> is the interface definition for <see cref="MappingConfigurationService"/>
    /// </summary>
    public interface IMappingConfigurationService
    {
        /// <summary>
        /// Gets or sets the <see cref="ExternalIdentifierMap"/>
        /// </summary>
        ExternalIdentifierMap ExternalIdentifierMap { get; set; }

        /// <summary>
        /// Get a value indicating wheter the current <see cref="ExternalIdentifierMap" /> is the default one
        /// </summary>
        bool IsTheCurrentIdentifierMapTemporary { get; }

        /// <summary>
        /// Selects the values as defined in the <see cref="MappingConfigurationService.ExternalIdentifierMap"/> for the mappin
        /// </summary>
        void SelectValues(IEnumerable<VariableRowViewModel> variableRowViewModels);

        /// <summary>
        /// Loads the mapping configuration and generates the map result respectively
        /// </summary> 
        /// <param name="variables">The collection of <see cref="VariableRowViewModel"/></param>
        /// <returns>A collection of <see cref="VariableRowViewModel"/></returns>
        List<MappedElementDefinitionRowViewModel> LoadMappingFromHubToDst(IList<VariableRowViewModel> variables);

        /// <summary>
        /// Loads the mapping configuration and generates the map result respectively
        /// </summary>
        /// <param name="variables">The collection of <see cref="VariableRowViewModel"/></param>
        /// <returns>A collection of <see cref="VariableRowViewModel"/></returns>
        List<VariableRowViewModel> LoadMappingFromDstToHub(IList<VariableRowViewModel> variables);

        /// <summary>
        /// Updates the configured mapping, registering the <see cref="MappingConfigurationService.ExternalIdentifierMap"/> and its <see cref="IdCorrespondence"/>
        /// to a <see name="IThingTransaction"/>
        /// </summary>
        /// <param name="transaction">The <see cref="IThingTransaction"/></param>
        /// <param name="iterationClone">The <see cref="Iteration"/> clone</param>
        void PersistExternalIdentifierMap(IThingTransaction transaction, Iteration iterationClone);

        /// <summary>
        /// Creates the <see cref="MappingConfigurationService.ExternalIdentifierMap" />
        /// </summary>
        /// <param name="newName">
        /// The model name to use for creating the new
        /// <see cref="MappingConfigurationService.ExternalIdentifierMap" />
        /// </param>
        /// <param name="addTheTemporyMapping">a value indicating whether the current temporary should be transfered to new one</param>
        /// <returns>A newly created <see cref="MappingConfigurationService.ExternalIdentifierMap" /></returns>
        ExternalIdentifierMap CreateExternalIdentifierMap(string newName, bool addTheTemporyMapping);

        /// <summary>
        /// Adds one correspondance to the <see cref="MappingConfigurationService.ExternalIdentifierMap"/>
        /// </summary>
        /// <param name="internalId">The thing that <see cref="externalId"/> corresponds to</param>
        /// <param name="valueIndex">The value index</param>
        /// <param name="externalId">The external thing that <see cref="internalId"/> corresponds to</param>
        /// <param name="mappingDirection">The <see cref="MappingDirection"/> the mapping belongs</param>
        void AddToExternalIdentifierMap(Guid internalId, double valueIndex, object externalId, MappingDirection mappingDirection);

        /// <summary>
        /// Adds one correspondance to the <see cref="MappingConfigurationService.ExternalIdentifierMap"/>
        /// </summary>
        /// <param name="mappedElement">The <see cref="mappedElement"/></param>
        void AddToExternalIdentifierMap(MappedElementDefinitionRowViewModel mappedElement);

        /// <summary>
        /// Adds one correspondance to the <see cref="MappingConfigurationService.ExternalIdentifierMap"/>
        /// </summary>
        /// <param name="internalId">The thing that <see cref="externalId"/> corresponds to</param>
        /// <param name="externalId">The external thing that <see cref="internalId"/> corresponds to</param>
        /// <param name="mappingDirection">The <see cref="MappingDirection"/> the mapping belongs</param>
        void AddToExternalIdentifierMap(Guid internalId, object externalId, MappingDirection mappingDirection);

        /// <summary>
        /// Adds as many correspondence as <paramref name="parameterVariable"/> values
        /// </summary>
        /// <param name="parameterVariable">The <see cref="Dictionary{T,T}"/> of mapped <see cref="ParameterOrOverrideBase"/> / <see cref="VariableRowViewModel"/></param>
        void AddToExternalIdentifierMap(Dictionary<ParameterOrOverrideBase, VariableRowViewModel> parameterVariable);

        /// <summary>
        /// Adds one correspondence to the <see cref="MappingConfigurationService.ExternalIdentifierMap"/>
        /// </summary>
        /// <param name="internalId">The thing that <paramref name="externalIdentifier"/> corresponds to</param>
        /// <param name="externalIdentifier">The external thing that <see cref="internalId"/> corresponds to</param>
        void AddToExternalIdentifierMap(Guid internalId, ExternalIdentifier externalIdentifier);

        /// <summary>
        /// Refreshes the <see cref="MappingConfigurationService.ExternalIdentifierMap"/> usually done after a session write
        /// </summary>
        void RefreshExternalIdentifierMap();
    }
}
