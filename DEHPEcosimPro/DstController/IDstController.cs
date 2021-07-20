// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IDstController.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2020 RHEA System S.A.
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

namespace DEHPEcosimPro.DstController
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using CDP4Common.EngineeringModelData;

    using DEHPCommon.Enumerators;
    using DEHPCommon.MappingEngine;

    using DEHPEcosimPro.ViewModel.Rows;
    using DEHPEcosimPro.Services.OpcConnector;
    using DEHPEcosimPro.ViewModel.Dialogs;

    using Opc.Ua;

    using ReactiveUI;

    /// <summary>
    /// Interface definition for <see cref="DstController"/>
    /// </summary>
    public interface IDstController
    {
        /// <summary>
        /// A value indicating whether the selected values saved in the mapping can be loaded
        /// </summary>
        bool CanLoadSelectedValues { get; set; }

        /// <summary>
        /// Assert whether the <see cref="OpcSessionHandler.OpcSession"/> is Open
        /// </summary>
        bool IsSessionOpen { get; set; }

        /// <summary>
        /// The endpoint url of the currently open session
        /// </summary>
        string ServerAddress { get; }

        /// <summary>
        /// The refresh interval for subscriptions in milliseconds
        /// </summary>
        int RefreshInterval { get; }

        /// <summary>
        /// Gets or sets the <see cref="MappingDirection"/>
        /// </summary>
        MappingDirection MappingDirection { get; set; }

        /// <summary>
        /// Gets the references variables available from the connected OPC server
        /// </summary>
        IList<(ReferenceDescription Reference, DataValue Node)> Variables { get; }

        /// <summary>
        /// Gets the Methods available from the connected OPC server
        /// </summary>
        IList<ReferenceDescription> Methods { get; }

        /// <summary>
        /// Gets the all references available from the connected OPC server
        /// </summary>
        IList<ReferenceDescription> References { get; }

        /// <summary>
        /// Gets the colection of mapped <see cref="Parameter"/>s And <see cref="ParameterOverride"/>s through their container
        /// </summary>
        ReactiveList<ElementBase> DstMapResult { get; }

        /// <summary>
        /// Gets the colection of <see cref="ElementBase"/> that are selected to be transfered
        /// </summary>
        ReactiveList<ElementBase> SelectedDstMapResultToTransfer { get; }

        /// <summary>
        /// Gets the colection of <see cref="MappedElementDefinitionRowViewModel"/> that are selected to be transfered
        /// </summary>
        ReactiveList<MappedElementDefinitionRowViewModel> SelectedHubMapResultToTransfer { get; }

        /// <summary>
        /// Gets a <see cref="Dictionary{TKey, TValue}"/> of all mapped parameter and the associate <see cref="VariableRowViewModel"/>
        /// </summary>
        Dictionary<ParameterOrOverrideBase, VariableRowViewModel> ParameterVariable { get; }

        /// <summary>
        /// Gets the colection of mapped <see cref="ReferenceDescription"/>
        /// </summary>
        ReactiveList<MappedElementDefinitionRowViewModel> HubMapResult { get; }

        /// <summary>
        /// Gets the collection of <see cref="VariableRowViewModel"/>
        /// </summary>
        ReactiveList<VariableRowViewModel> VariableRowViewModels { get; }

        /// <summary>
        /// Gets the OPC Time <see cref="NodeId"/>
        /// </summary>
        NodeId TimeNodeId { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the experiment is running
        /// </summary>
        bool IsExperimentRunning { get; set; }

        /// <summary>
        /// Loads the saved mapping and applies the mapping rule to the <see cref="VariableRowViewModel"/>
        /// </summary>
        /// <returns>The number of mapped things loaded</returns>
        int LoadMapping();

        /// <summary>
        /// Connects to the provided endpoint
        /// </summary>
        /// <param name="endpoint">The end point url eg. often opc.tcp:// representing the opc protocol</param>
        /// <param name="autoAcceptConnection">An assert whether the certificate should be auto accepted if valid</param>
        /// <param name="credential">The <see cref="IUserIdentity"/> default = null in case server does not require authentication</param>
        /// <param name="samplingInterval">The <see cref="int"/> sampling interval in millisecond</param>
        /// <returns>A <see cref="Task"/></returns>
        Task Connect(string endpoint, bool autoAcceptConnection = true, IUserIdentity credential = null, int samplingInterval = 1000);

        /// <summary>
        /// Reads and returns the server start time, in UTC, of the currently open session
        /// </summary>
        /// <returns>null if the session is closed or the ServerStatus.StartTime node was not found</returns>
        DateTime? GetServerStartTime();

        /// <summary>
        /// Reads and returns the current server time, in UTC, of the currently open session
        /// </summary>
        /// <returns>null if the session is closed or the ServerStatus.CurrentTime node was not found</returns>
        DateTime? GetCurrentServerTime();

        /// <summary>
        /// Adds one subscription for the <paramref name="nodeId"/>
        /// </summary>
        /// <param name="nodeId">The <see cref="NodeId"/></param>
        void AddSubscription(NodeId nodeId);

        /// <summary>
        /// Adds one subscription for the <paramref name="reference"/>
        /// </summary>
        /// <param name="reference">The <see cref="ReferenceDescription"/></param>
        void AddSubscription(ReferenceDescription reference);

        /// <summary>
        /// Calls the specified method and returns the output arguments.
        /// </summary>
        /// <param name="methodBrowseName">The BrowseName of the server method</param>
        /// <returns>The <see cref="IList{T}"/> of output argument values, or null if the no method was found with the provided BrowseName</returns>
        IList<object> CallServerMethod(string methodBrowseName);

        /// <summary>
        /// Removes all active subscriptions from the session.
        /// </summary>
        void ClearSubscriptions();

        /// <summary>
        /// Closes the <see cref="OpcSessionHandler.OpcSession"/>
        /// </summary>
        void CloseSession();

        /// <summary>
        /// Map the provided collection using the corresponding rule in the assembly and the <see cref="MappingEngine"/>
        /// </summary>
        /// <param name="dstVariables">The <see cref="List{T}"/> of <see cref="VariableRowViewModel"/> data</param>
        /// <returns>A <see cref="Task"/></returns>
        void Map(List<VariableRowViewModel> dstVariables);

        /// <summary>
        /// Map the provided collection using the corresponding rule in the assembly and the <see cref="MappingEngine"/>
        /// </summary>
        /// <param name="mappedElement">The <see cref="List{T}"/> of <see cref="MappedElementDefinitionRowViewModel"/></param>
        /// <returns>A <see cref="Task"/></returns>
        void Map(List<MappedElementDefinitionRowViewModel> mappedElement);

        /// <summary>
        /// Transfers again all already transfered value to the dst in case of OPC server reset
        /// </summary>
        void ReTransferMappedThingsToDst();

        /// <summary>
        /// Transfers the mapped variables to the Dst data source
        /// </summary>
        void TransferMappedThingsToDst();

        /// <summary>
        /// Gets a value indicating if the <paramref name="reference"/> value can be overridden 
        /// </summary>
        /// <param name="reference"></param>
        /// <returns>An assert</returns>
        bool IsVariableWritable(ReferenceDescription reference);

        /// <summary>
        /// Writes the <see cref="double"/> <paramref name="value"/> to the <paramref name="nodeId"/>
        /// </summary>
        /// <param name="nodeId">The <see cref="NodeId"/> on which to write the <paramref name="value"/></param>
        /// <param name="value">The <see cref="double"/> value</param>
        /// <returns>An assert</returns>
        bool WriteToDst(NodeId nodeId, double value);

        /// <summary>
        /// Reads a node and gets its states information
        /// </summary>
        /// <param name="reference">The <see cref="ReferenceDescription"/> to read</param>
        /// <returns>The <see cref="DataValue"/></returns>
        DataValue ReadNode(ReferenceDescription reference);

        /// <summary>
        /// Reads all values for <see cref="DstController.Variables"/> based on <paramref name="time"/>
        /// </summary>
        /// <param name="time">The current time</param>
        void ReadAllNode(double time);

        /// <summary>
        /// Resets all <see cref="VariableRowViewModel"/> by deleting the collected values
        /// </summary>
        void ResetVariables();

        /// <summary>
        /// Sets the next experiment step in the OPC server and retrives new values for <see cref="DstController.Variables"/>
        /// </summary>
        void GetNextExperimentStep();

        /// <summary>
        /// Transfers the mapped variables to the Hub data source
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        Task TransferMappedThingsToHub();

        /// <summary>
        /// Updates the <see cref="IValueSet"/> of all <see cref="Parameter"/> and all <see cref="ParameterOverride"/>
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        Task UpdateParametersValueSets();
    }
}
