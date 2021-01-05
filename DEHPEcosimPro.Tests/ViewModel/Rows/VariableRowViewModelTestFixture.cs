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

    using CDP4Dal;

    using DEHPEcosimPro.Events;
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
            }, new DataValue() { Value = value, ServerTimestamp = DateTime.MinValue}));

            Assert.AreEqual(name, viewModel.Name);
            Assert.AreEqual(null, viewModel.LastNotificationTime);
            Assert.AreEqual(value, viewModel.ActualValue);
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
            }, new DataValue() { Value = 63.1, ServerTimestamp = DateTime.MinValue }));

            CDPMessageBus.Current.SendMessage(new OpcVariableChangedEvent()
            {
                TimeStamp = DateTime.MinValue.AddDays(1), Id = viewModel.Reference.NodeId.Identifier, Value = 20.9
            });
            
            Assert.AreEqual(2, viewModel.Values.Count);
            Assert.AreEqual(42, viewModel.AverageValue);

            Assert.Throws<NullReferenceException>(() => _ = new OpcVariableChangedEvent(new MonitoredItem()));
        }

        [Test]
        public void VerifyAverageCalculation()
        {
            var viewModel = new VariableRowViewModel((new ReferenceDescription()
            {
                NodeId = new ExpandedNodeId(Guid.NewGuid()),
                DisplayName = new LocalizedText("", "DummyVariable0")
            }, new DataValue() { Value = .2 }));

            var newValues = new List<(object Value, DateTime When)>()
            {
                (131234, DateTime.MinValue.AddDays(1)), 
                (-143298.5224323, DateTime.MinValue.AddDays(1)),
                (2u, DateTime.MinValue.AddDays(1)),
                (44.87613, DateTime.MinValue.AddDays(1)),
                (0.42e2, DateTime.MinValue.AddDays(1)),
                (.12387, DateTime.MinValue.AddDays(1)),
                (2ul, DateTime.MinValue.AddDays(1))
            };

            viewModel.Values.AddRange(newValues);

            Assert.AreEqual(-1496.6653040374986d, viewModel.ComputeAverageValue());
            viewModel.Values.Add(("15%", DateTime.MinValue.AddDays(2)));
            Assert.AreEqual("-", viewModel.ComputeAverageValue());
        }
    }
}
