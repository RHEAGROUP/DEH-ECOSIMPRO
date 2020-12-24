// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VariableRowViewModelTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Tests.ViewModel.Rows
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using DEHPEcosimPro.ViewModel.Rows;

    using NUnit.Framework;

    using Opc.Ua;
    using Opc.Ua.Client;

    [TestFixture]
    public class VariableRowViewModelTestFixture
    {
        [Test]
        public void VerifyProperties()
        {
            var id = Guid.NewGuid();
            const string name = "DummyVariable0";
            const double value = .2;

            var viewModel = new VariableRowViewModel((new ReferenceDescription()
            {
                NodeId = new ExpandedNodeId(id),
                DisplayName = new LocalizedText("", name)
            }, new DataValue() {Value = value}));

            Assert.AreEqual(NodeClass.Unspecified.ToString(), viewModel.NodeType);
            Assert.AreEqual(name, viewModel.Name);
            Assert.AreEqual(id.ToString(), viewModel.Id);
            Assert.AreEqual(value, viewModel.ActualValue);
            Assert.IsNull(viewModel.LastNotificationTime);
            Assert.IsNotEmpty(viewModel.Values);
            Assert.AreEqual(value, viewModel.InitialValue);
            Assert.IsNull(viewModel.AverageValue);
        }

        /// <summary>
        /// Throws <see cref="NullReferenceException"/>, because of the <see cref="MonitoredItem.LastValue"/>
        /// and the <see cref="MonitoredItem.Subscription"/> unaccessible setters
        /// </summary>
        [Test]
        public void VerifyOnNotification()
        {
            var viewModel = new VariableRowViewModel((new ReferenceDescription()
            {
                NodeId = new ExpandedNodeId(Guid.NewGuid()),
                DisplayName = new LocalizedText("", "DummyVariable0")
            }, new DataValue() { Value = .2 }));

            var monitoredItem = new MonitoredItem();
            
            Assert.Throws<NullReferenceException>(() => viewModel.OnNotification(monitoredItem, null));
        }

        [Test]
        public void VerifyAverageCalculation()
        {
            var viewModel = new VariableRowViewModel((new ReferenceDescription()
            {
                NodeId = new ExpandedNodeId(Guid.NewGuid()),
                DisplayName = new LocalizedText("", "DummyVariable0")
            }, new DataValue() { Value = .2 }));

            var newValues = new List<double>() { 131234, .01001023012f, 2u, 12312.4323423, 0.42e2, .09009, 2ul };

            viewModel.Values.AddRange(newValues.Cast<object>());

            Assert.AreEqual((newValues.Sum() + .2) / (newValues.Count + 1), viewModel.ComputeAverageValue());
        }
    }
}
