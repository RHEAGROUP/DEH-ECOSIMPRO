// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstControllerTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Tests.DstController
{
    using System;
    using System.Threading.Tasks;

    using DEHPCommon.HubController.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Services.OpcConnector.Interfaces;

    using Moq;

    using NUnit.Framework;

    using Opc.Ua;
    using Opc.Ua.Client;

    [TestFixture]
    public class DstControllerTestFixture
    {
        private DstController controller;
        private Mock<IOpcClientService> opcClient;
        private Mock<IHubController> hubController;
        private Mock<IOpcSessionHandler> opcSessionHandler;

        [SetUp]
        public void Setup()
        {
            this.hubController = new Mock<IHubController>();
            this.opcSessionHandler = new Mock<IOpcSessionHandler>();
    
            this.opcClient = new Mock<IOpcClientService>();
            this.opcClient.Setup(x => x.Connect(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IUserIdentity>())).Returns(Task.CompletedTask);
            this.opcClient.Setup(x => x.CloseSession());
            this.controller = new DstController(this.opcClient.Object, this.hubController.Object, this.opcSessionHandler.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsFalse(this.controller.IsSessionOpen);
            Assert.IsEmpty(this.controller.Variables);
            Assert.IsNull(this.controller.References);
            Assert.IsEmpty(this.controller.Methods);
        }

        [Test]
        public void VerifyConnect()
        {
            Assert.DoesNotThrowAsync(async () => await this.controller.Connect("endpoint"));
            this.opcClient.Verify(x => x.Connect(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IUserIdentity>()), Times.Once);
        }

        [Test]
        public void VerifyClose()
        {
            this.controller.CloseSession();
            this.opcClient.Verify(x => x.CloseSession(), Times.Once);
        }

        [Test]
        public void VerifyAddSubscription()
        {
            Assert.DoesNotThrow(() =>
            {
                this.controller.AddSubscription(new ReferenceDescription() { NodeId = new ExpandedNodeId(Guid.NewGuid()) });
            this.controller.AddSubscription(new ReferenceDescription() { NodeId = new ExpandedNodeId(Guid.NewGuid())}, (item, args) => { });
            });

            this.opcClient.Verify(x => x.AddSubscription(It.IsAny<NodeId>(), It.IsAny<MonitoredItemNotificationEventHandler>()), Times.Exactly(2));

            this.controller.ClearSubscriptions();
            this.opcSessionHandler.Verify(x => x.ClearSubscriptions(), Times.Once);
        }
    }
}
