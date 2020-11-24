// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstAdapter.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.DstAdapter
{
    using System.Threading.Tasks;

    using DEHPEcosimPro.Enumerator;
    using DEHPEcosimPro.Services.OpcConnector.Interfaces;

    using Opc.Ua;

    using ReactiveUI;

    /// <summary>
    /// The <see cref="DstAdapter"/> takes care of retrieving data from and to EcosimPro
    /// </summary>
    public class DstAdapter : ReactiveObject, IDstAdapter
    {
        /// <summary>
        /// The <see cref="IOpcClientService"/> that handles the OPC connection with EcosimPro
        /// </summary>
        private readonly IOpcClientService opcClientService;
        
        /// <summary>
        /// Assert whether the <see cref="Services.OpcConnector.OpcSessionHandler.Session"/> is Open
        /// </summary>
        public bool IsSessionOpen =>  this.opcClientService.OpcClientStatusCode == OpcClientStatusCode.Connected;

        /// <summary>
        /// Initializes a new <see cref="DstAdapter"/>
        /// </summary>
        /// <param name="opcClientService">The <see cref="IOpcClientService"/></param>
        public DstAdapter(IOpcClientService opcClientService)
        {
            this.opcClientService = opcClientService;
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
        /// Closes the <see cref="Services.OpcConnector.OpcSessionHandler.Session"/>
        /// </summary>
        public void CloseSession()
        {
            this.opcClientService.CloseSession();
        }
    }
}
