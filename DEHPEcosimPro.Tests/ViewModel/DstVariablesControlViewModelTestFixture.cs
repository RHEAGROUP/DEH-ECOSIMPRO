// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstVariablesControlViewModelTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Tests.ViewModel
{
    using System;
    using System.Collections.Generic;

    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel;

    using Moq;

    using NUnit.Framework;

    using Opc.Ua;
    using Opc.Ua.Client;

    [TestFixture]
    public class DstVariablesControlViewModelTestFixture
    {
        private DstVariablesControlViewModel viewModel;
        private Mock<IDstController> dstController;
        private Mock<INavigationService> navigationService;
        private Mock<IHubController> hubController;

        [SetUp]
        public void Setup()
        {
            this.dstController = new Mock<IDstController>();

            this.dstController.Setup(x => x.AddSubscription(It.IsAny<ReferenceDescription>()));
            
            this.dstController.Setup(x => x.Variables).Returns(
                new List<(ReferenceDescription Reference, DataValue Value)>()
                {
                    (new ReferenceDescription()
                    {
                        NodeId = new ExpandedNodeId(Guid.NewGuid()), DisplayName = new LocalizedText("", "DummyVariable0")
                    }, new DataValue()),
                    (new ReferenceDescription()
                    {
                        NodeId = new ExpandedNodeId(Guid.NewGuid()), DisplayName = new LocalizedText("", "DummyVariable1")
                    }, new DataValue()),
                    (new ReferenceDescription()
                    {
                        NodeId = new ExpandedNodeId(Guid.NewGuid()), DisplayName = new LocalizedText("", "DummyVariable2")
                    }, new DataValue()),
                });

            this.navigationService = new Mock<INavigationService>();
            this.hubController = new Mock<IHubController>();

            this.viewModel = new DstVariablesControlViewModel(this.dstController.Object, this.navigationService.Object, this.hubController.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsEmpty(this.viewModel.Variables);
            Assert.IsFalse(this.viewModel.IsBusy);
        }

        [Test]
        public void VerifyUpdateProperties()
        {
            this.dstController.Setup(x => x.IsSessionOpen).Returns(true);
            this.viewModel.UpdateProperties();
            Assert.AreEqual(3, this.viewModel.Variables.Count);

            this.dstController.Verify(x => x.AddSubscription(It.IsAny<ReferenceDescription>()), Times.Exactly(3));
            
            this.dstController.Setup(x => x.IsSessionOpen).Returns(false);
            this.viewModel.UpdateProperties();

            this.dstController.Verify(x => x.IsSessionOpen, Times.Exactly(5));
            this.dstController.Verify(x => x.ClearSubscriptions(), Times.Exactly(2));
        }
    }
}
