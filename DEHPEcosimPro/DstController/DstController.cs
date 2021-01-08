﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstController.cs" company="RHEA System S.A.">
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
    using System.Linq;
    using System.Threading.Tasks;

    using DEHPCommon.HubController.Interfaces;

    using DEHPEcosimPro.Enumerator;
    using DEHPEcosimPro.Services.OpcConnector;
    using DEHPEcosimPro.Services.OpcConnector.Interfaces;

    using Opc.Ua;

    using ReactiveUI;

    /// <summary>
    /// The <see cref="DstController"/> takes care of retrieving data from and to EcosimPro
    /// </summary>
    public class DstController : ReactiveObject, IDstController
    {
        /// <summary>
        /// The <see cref="IOpcClientService"/> that handles the OPC connection with EcosimPro
        /// </summary>
        private readonly IOpcClientService opcClientService;

        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IOpcSessionHandler"/>
        /// </summary>
        private readonly IOpcSessionHandler sessionHandler;

        /// <summary>
        /// Backing field for <see cref="IsSessionOpen"/>
        /// </summary>
        private bool isSessionOpen;

        /// <summary>
        /// Assert whether the <see cref="OpcSessionHandler.OpcSession"/> is Open
        /// </summary>
        public bool IsSessionOpen
        {
            get => this.isSessionOpen;
            set => this.RaiseAndSetIfChanged(ref this.isSessionOpen, value);
        }

        /// <summary>
        /// The endpoint url of the currently open session
        /// </summary>
        public string ServerAddress => this.opcClientService.EndpointUrl;

        /// <summary>
        /// The refresh interval for subscriptions in milliseconds
        /// </summary>
        public int RefreshInterval => this.opcClientService.RefreshInterval;

        /// <summary>
        /// Gets the references variables available from the connected OPC server
        /// </summary>
        public IList<(ReferenceDescription Reference, DataValue Node)> Variables { get; private set; } = new List<(ReferenceDescription, DataValue)>();

        /// <summary>
        /// Gets the Methods available from the connected OPC server
        /// </summary>
        public IList<ReferenceDescription> Methods { get; private set; } = new List<ReferenceDescription>();

        /// <summary>
        /// Gets the all references available from the connected OPC server
        /// </summary>
        public IList<ReferenceDescription> References => this.opcClientService.References;

        /// <summary>
        /// Initializes a new <see cref="DstController"/>
        /// </summary>
        /// <param name="opcClientService">The <see cref="IOpcClientService"/></param>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="sessionHandler">The <<see cref="IOpcSessionHandler"/></param>
        public DstController(IOpcClientService opcClientService, IHubController hubController, IOpcSessionHandler sessionHandler)
        {
            this.opcClientService = opcClientService;
            this.hubController = hubController;
            this.sessionHandler = sessionHandler;

            this.WhenAnyValue(x => x.opcClientService.OpcClientStatusCode).Subscribe(clientStatusCode =>
            {
                var isOpcSessionOpen = clientStatusCode == OpcClientStatusCode.Connected;

                if (isOpcSessionOpen)
                {
                    foreach (var reference in this.opcClientService.References)
                    {
                        if (reference.NodeClass == NodeClass.Variable && reference.NodeId.NamespaceIndex == 4)
                        {
                            this.Variables.Add((reference, this.opcClientService.ReadNode((NodeId)reference.NodeId)));
                        }

                        else if (reference.NodeClass == NodeClass.Method)
                        {
                            this.Methods.Add(reference);
                        }
                    }
                }

                this.IsSessionOpen = isOpcSessionOpen;
            });
        }

        /// <summary>
        /// Connects to the provided endpoint
        /// </summary>
        /// <param name="endpoint">The end point url eg. often opc.tcp:// representing the opc protocol</param>
        /// <param name="autoAcceptConnection">An assert whether the certificate should be auto accepted if valid</param>
        /// <param name="credential">The <see cref="IUserIdentity"/> default = null in case server does not require authentication</param>
        /// <returns>A <see cref="Task"/></returns>
        public async Task Connect(string endpoint, bool autoAcceptConnection = true, IUserIdentity credential = null)
        {
            await this.opcClientService.Connect(endpoint, autoAcceptConnection, credential);
        }

        /// <summary>
        /// Reads and returns the server start time, in UTC, of the currently open session
        /// </summary>
        /// <returns>null if the session is closed or the ServerStatus.StartTime node was not found</returns>
        public DateTime? GetServerStartTime()
        {
            var serverStartTimeNode = this.opcClientService.ReadNode(Opc.Ua.Variables.Server_ServerStatus_StartTime);
            if (this.IsSessionOpen && serverStartTimeNode != null)
            {
                return (DateTime)serverStartTimeNode.Value;
            }

            return null;
        }

        /// <summary>
        /// Reads and returns the current server time, in UTC, of the currently open session
        /// </summary>
        /// <returns>null if the session is closed or the ServerStatus.CurrentTime node was not found</returns>
        public DateTime? GetCurrentServerTime()
        {
            var currentServerTimeNode = this.opcClientService.ReadNode(Opc.Ua.Variables.Server_ServerStatus_CurrentTime);
            if (this.IsSessionOpen && currentServerTimeNode != null)
            {
                return (DateTime)currentServerTimeNode.Value;

            }

            return null;
        }

        /// <summary>
        /// Adds one subscription for the <paramref name="nodeId"/>
        /// </summary>
        /// <param name="nodeId">The <see cref="NodeId"/></param>
        public void AddSubscription(NodeId nodeId)
        {
            this.opcClientService.AddSubscription(nodeId);
        }

        /// <summary>
        /// Adds one subscription for the <paramref name="reference"/>
        /// </summary>
        /// <param name="reference">The <see cref="ReferenceDescription"/></param>
        public void AddSubscription(ReferenceDescription reference)
        {
            this.AddSubscription((NodeId)reference.NodeId);
        }

        /// <summary>
        /// Calls the specified method and returns the output arguments.
        /// </summary>
        /// <param name="methodBrowseName">The BrowseName of the server method</param>
        /// <param name="arguments">The arguments to input</param>
        /// <returns>The <see cref="IList{T}"/> of output argument values, or null if the no method was found with the provided BrowseName</returns>
        public IList<object> CallServerMethod(string methodBrowseName, params object[] arguments)
        {
            var serverMethodsNode = this.References.SingleOrDefault(r => r.BrowseName.Name == "server_methods")?.NodeId;
            var methodNode = this.Methods.SingleOrDefault(m => m.BrowseName.Name == methodBrowseName)?.NodeId;

            if (serverMethodsNode != null && methodNode != null)
            {
                return this.opcClientService.CallMethod(
                    new NodeId(serverMethodsNode.Identifier, serverMethodsNode.NamespaceIndex),
                    new NodeId(methodNode.Identifier, methodNode.NamespaceIndex),
                    string.Empty);
            }

            return null;
        }

        /// <summary>
        /// Removes all active subscriptions from the session.
        /// </summary>
        public void ClearSubscriptions()
        {
            this.sessionHandler.ClearSubscriptions();
        }

        /// <summary>
        /// Closes the <see cref="OpcSessionHandler.OpcSession"/>
        /// </summary>
        public void CloseSession()
        {
            this.Methods.Clear();
            this.Variables.Clear();
            this.opcClientService.CloseSession();
        }
    }
}
