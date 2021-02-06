// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IOpcClientService.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Services.OpcConnector.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using DEHPEcosimPro.Enumerator;

    using Opc.Ua;
    using Opc.Ua.Client;

    /// <summary>
    /// Interface definition for <see cref="OpcClientService"/>
    /// </summary>
    public interface IOpcClientService
    {
        /// <summary>
        /// The endpoint url
        /// </summary>
        string EndpointUrl { get; }

        /// <summary>
        /// The refresh interval for subscriptions in milliseconds
        /// </summary>
        int RefreshInterval { get; set; }

        /// <summary>
        /// Gets the <see cref="OpcClientStatusCode"/> reflecting the connection status of this <see cref="OpcClientService"/>
        /// </summary>
        OpcClientStatusCode OpcClientStatusCode { get; }

        /// <summary>
        /// Gets the collection of <see cref="ReferenceDescription"/> that holds for instance <see cref="ReferenceDescription.NodeId"/>
        /// </summary>
        ReferenceDescriptionCollection References { get; }

        /// <summary>
        /// Connects the client to the endpoint opening a <see cref="Opc.Ua.Client.Session"/>
        /// </summary>
        /// <param name="endpoint">The end point url eg. often opc.tcp:// representing the opc protocol</param>
        /// <param name="autoAcceptConnection">An assert whether the certificate should be auto accepted if valid</param>
        /// <param name="credential">The <see cref="IUserIdentity"/> default = null in case server does not require authentication</param>
        /// <returns>A <see cref="Task"/></returns>
        Task Connect(string endpoint, bool autoAcceptConnection = true, IUserIdentity credential = null);

        /// <summary>
        /// Closes the <see cref="Opc.Ua.Client.Session"/>
        /// </summary>
        void CloseSession();

        /// <summary>
        /// Adds a subscription based on the nodeId to monitor
        /// </summary>
        /// <param name="nodeId">The the <see cref="NodeId"/> to monitor</param>
        void AddSubscription(NodeId nodeId);

        /// <summary>
        /// Calls the specified method and returns the output arguments.
        /// </summary>
        /// <param name="objectId">The <see cref="NodeId"/> object Id</param>
        /// <param name="methodId">The <see cref="NodeId"/> method Id </param>
        /// <param name="arguments">The arguments to input</param>
        /// <returns>The <see cref="IList{T}"/> of output argument values.</returns>
        IList<object> CallMethod(NodeId objectId, NodeId methodId, params object[] arguments);

        /// <summary>
        /// Reads a node and gets its states information
        /// </summary>
        /// <param name="nodeId">The <see cref="NodeId"/> to read</param>
        /// <returns>The <see cref="DataValue"/></returns>
        DataValue ReadNode(NodeId nodeId);

        /// <summary>
        /// Writes a value to a node
        /// </summary>
        /// <param name="nodeId">The <see cref="NodeId"/> of the node to update</param>
        /// <param name="value">The value to write</param>
        /// <returns>A value indicating whether the write operation succeed</returns>
        bool WriteNode(NodeId nodeId, object value);

        /// <summary>
        /// The <see cref="OpcClientService.CertificateValidator"/> validates
        /// </summary>
        /// <param name="validator">A <see cref="OpcClientService.CertificateValidator"/></param>
        /// <param name="e">The <see cref="CertificateValidationEventArgs"/></param>
        void CertificateValidator(CertificateValidator validator, CertificateValidationEventArgs e);
    }
}
