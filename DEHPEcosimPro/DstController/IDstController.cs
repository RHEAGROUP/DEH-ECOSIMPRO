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

    using Opc.Ua;
    using Opc.Ua.Client;

    /// <summary>
    /// Interface definition for <see cref="DstController"/>
    /// </summary>
    public interface IDstController
    {
        /// <summary>
        /// Assert whether the <see cref="Services.OpcConnector.OpcSessionHandler.Session"/> is Open
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
        /// Connects to the provided endpoint
        /// </summary>
        /// <param name="endpoint">The end point url eg. often opc.tcp:// representing the opc protocol</param>
        /// <param name="autoAcceptConnection">An assert whether the certificate should be auto accepted if valid</param>
        /// <param name="credential">The <see cref="IUserIdentity"/> default = null in case server does not require authentication</param>
        /// <returns>A <see cref="Task"/></returns>
        Task Connect(string endpoint, bool autoAcceptConnection = true, IUserIdentity credential = null);

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
        public void AddSubscription(NodeId nodeId);

        /// <summary>
        /// Adds one subscription for the <paramref name="reference"/>
        /// </summary>
        /// <param name="reference">The <see cref="ReferenceDescription"/></param>
        void AddSubscription(ReferenceDescription reference);

        /// <summary>
        /// Removes all active subscriptions from the session.
        /// </summary>
        void ClearSubscriptions();

        /// <summary>
        /// Closes the <see cref="Services.OpcConnector.OpcSessionHandler.Session"/>
        /// </summary>
        void CloseSession();
    }
}
