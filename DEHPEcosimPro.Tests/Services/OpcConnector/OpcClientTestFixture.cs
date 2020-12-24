// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OpcClientTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Tests.Services.OpcConnector
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using DEHPCommon.Enumerators;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.Enumerator;
    using DEHPEcosimPro.Services.OpcConnector;
    using DEHPEcosimPro.Services.OpcConnector.Interfaces;

    using Moq;

    using NUnit.Framework;

    using Opc.Ua;
    using Opc.Ua.Client;

    [TestFixture]
    public class OpcClientTestFixture
    {
        private const string Endpoint = "opc.tcp://test:16700";
        private Mock<IStatusBarControlViewModel> statusBarViewModel;
        private Mock<IOpcSessionHandler> sessionHandler;
        private ReferenceDescriptionCollection referenceDescriptionCollection;
        private Mock<IOpcSessionReconnectHandler> reconnectHandler;
        private OpcClientService client;

        [SetUp]
        public async Task Setup()
        {
            this.referenceDescriptionCollection = new ReferenceDescriptionCollection()
            {
                new ReferenceDescription()
                {
                    NodeId = new ExpandedNodeId(new NodeId(0))
                },
                new ReferenceDescription()
                {
                    NodeId = new ExpandedNodeId(new NodeId(1))
                },
                new ReferenceDescription()
                {
                    NodeId = new ExpandedNodeId(new NodeId(2))
                }
            };

            this.statusBarViewModel = new Mock<IStatusBarControlViewModel>();
            this.statusBarViewModel.Setup(x => x.Append(It.IsAny<string>(), It.IsAny<StatusBarMessageSeverity>()));

            this.reconnectHandler = new Mock<IOpcSessionReconnectHandler>();
            this.reconnectHandler.Setup(x => x.Activate());
            this.reconnectHandler.Setup(x => x.Deactivate());
            this.reconnectHandler.Setup(x => x.BeginReconnect(It.IsAny<Session>(), It.IsAny<EventHandler>(), It.IsAny<int>()));

            this.sessionHandler = new Mock<IOpcSessionHandler>();
            this.sessionHandler.Setup(x => x.NamespaceUris).Returns(new NamespaceTable());
            this.sessionHandler.Setup(x => x.KeepAliveStopped).Returns(false);
            this.sessionHandler.Setup(x => x.DefaultSubscription).Returns(new Subscription());
            this.sessionHandler.Setup(x => x.AddSubscription(It.IsAny<Subscription>())).Returns(true);
            this.sessionHandler.Setup(x => x.RemoveSubscription(It.IsAny<Subscription>())).Returns(true);
            this.sessionHandler.Setup(x => x.ClearSubscriptions()).Returns(true);
            this.sessionHandler.Setup(x => x.FetchReferences(It.IsAny<NodeId>())).Returns(this.referenceDescriptionCollection);
            this.sessionHandler.Setup(x => x.SetSession(this.reconnectHandler.Object));
            this.sessionHandler.Setup(x => x.SelectEndpoint(Endpoint, true, 15000)).Returns(new EndpointDescription(Endpoint));
            this.sessionHandler.Setup(x => x.CallMethod(It.IsAny<NodeId>(), It.IsAny<NodeId>(), It.IsAny<object[]>())).Returns(new List<object>() { 1 });

            this.sessionHandler.Setup(x => x.Browse(It.IsAny<NodeId>(), It.IsAny<NodeId>(), It.IsAny<bool>(), It.IsAny<uint>(), out It.Ref<byte[]>.IsAny, out this.referenceDescriptionCollection));

            this.sessionHandler.Setup(x => x.CreateSession(It.IsAny<ApplicationConfiguration>(), It.IsAny<ConfiguredEndpoint>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<uint>(), It.IsAny<IUserIdentity>(), It.IsAny<IList<string>>())).Returns(Task.CompletedTask);
            
            this.client = new OpcClientService(this.statusBarViewModel.Object, this.sessionHandler.Object, this.reconnectHandler.Object) { RefreshInterval = 1000};
            await this.client.Connect(Endpoint);
        }

        [Test]
        public async Task VerifyConnect()
        {
            this.client = new OpcClientService(this.statusBarViewModel.Object, this.sessionHandler.Object, this.reconnectHandler.Object) { RefreshInterval = 1000 };
            await this.client.Connect(Endpoint);
            Assert.AreEqual(OpcClientStatusCode.Connected, this.client.OpcClientStatusCode);
            Assert.IsTrue(this.client.References.Count > 0);

            this.sessionHandler.Setup(x => x.Browse(It.IsAny<NodeId>(), It.IsAny<NodeId>(), It.IsAny<bool>(), It.IsAny<uint>(), out It.Ref<byte[]>.IsAny, out this.referenceDescriptionCollection))
                .Throws<HttpRequestException>();

            Assert.ThrowsAsync<HttpRequestException>(async () => await this.client.Connect(Endpoint));
        }

        [Test]
        public void VerifySubscription()
        {
            var identifier = (NodeId)this.referenceDescriptionCollection.First().NodeId;
            Assert.DoesNotThrow(() => this.client.AddSubscription(identifier));
            this.sessionHandler.Setup(x => x.AddSubscription(It.IsAny<Subscription>())).Throws<InvalidOperationException>();
            this.statusBarViewModel.Verify(x => x.Append(It.IsAny<string>(), It.IsAny<StatusBarMessageSeverity>()), Times.Exactly(18));
            Assert.DoesNotThrow(() => this.client.AddSubscription(identifier));
            this.statusBarViewModel.Verify(x => x.Append(It.IsAny<string>(), It.IsAny<StatusBarMessageSeverity>()), Times.Exactly(20));
            var errorMessage = $"Error creating subscription for attribute id = 13";
            this.statusBarViewModel.Verify(x => x.Append(errorMessage, StatusBarMessageSeverity.Error), Times.Exactly(1));
        }

        [Test]
        public void VerifyMethodCall()
        {
            Assert.DoesNotThrow(() => this.client.CallMethod(new NodeId(2), new NodeId(5), 1));
            this.sessionHandler.Verify(x => x.CallMethod(It.IsAny<NodeId>(), It.IsAny<NodeId>(), It.IsAny<object[]>()), Times.Once);
        }

        [Test]
        public void VerifyClose()
        {
            Assert.DoesNotThrow(() => this.client.CloseSession());
            this.sessionHandler.Verify(x => x.CloseSession(true), Times.Once);
        }
    }
}
