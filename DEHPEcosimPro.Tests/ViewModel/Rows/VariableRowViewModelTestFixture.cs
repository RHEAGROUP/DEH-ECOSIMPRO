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

    using DEHPEcosimPro.Enumerator;
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

            var newValues = new List<TimeTaggedValueRowViewModel>()
            {
                new TimeTaggedValueRowViewModel(131234, DateTime.MinValue.AddDays(1)), 
                new TimeTaggedValueRowViewModel(-143298.5224323, DateTime.MinValue.AddDays(1)),
                new TimeTaggedValueRowViewModel(2u, DateTime.MinValue.AddDays(1)),
                new TimeTaggedValueRowViewModel(44.87613, DateTime.MinValue.AddDays(1)),
                new TimeTaggedValueRowViewModel(0.42e2, DateTime.MinValue.AddDays(1)),
                new TimeTaggedValueRowViewModel(.12387, DateTime.MinValue.AddDays(1)),
                new TimeTaggedValueRowViewModel(2ul, DateTime.MinValue.AddDays(1))
            };

            viewModel.Values.AddRange(newValues);

            Assert.AreEqual(-1496.6653040374986d, viewModel.ComputeAverageValue());
            viewModel.Values.Add(new TimeTaggedValueRowViewModel("15%", DateTime.MinValue.AddDays(2)));
            Assert.AreEqual("-", viewModel.ComputeAverageValue());
        }

        [Test]
        public void VerifyDiscreetSampling()
        {
            var dateTime = DateTime.Now;
                
            var viewModel = new VariableRowViewModel((new ReferenceDescription()
            {
                NodeId = new ExpandedNodeId(Guid.NewGuid()),
                DisplayName = new LocalizedText("", "DummyVariable0")
            }, new DataValue() { Value = .2, ServerTimestamp = dateTime}));

            var newValues = new List<TimeTaggedValueRowViewModel>()
            {
                new TimeTaggedValueRowViewModel(13123324, dateTime.AddMilliseconds(1), dateTime),
                new TimeTaggedValueRowViewModel(-98.52243, dateTime.AddMilliseconds(3),dateTime),
                new TimeTaggedValueRowViewModel(292312443u, dateTime.AddMilliseconds(14), dateTime),
                new TimeTaggedValueRowViewModel(44.87613, dateTime.AddMilliseconds(15), dateTime),
                new TimeTaggedValueRowViewModel(0.432e2, dateTime.AddMilliseconds(160000), dateTime),
                new TimeTaggedValueRowViewModel(.12387, dateTime.AddMilliseconds(160001), dateTime),
                new TimeTaggedValueRowViewModel(223ul, dateTime.AddMilliseconds(5000000), dateTime),
                new TimeTaggedValueRowViewModel(67ul, dateTime.AddSeconds(5), dateTime),
                new TimeTaggedValueRowViewModel(34, dateTime.AddSeconds(8), dateTime),
                new TimeTaggedValueRowViewModel(1, dateTime.AddHours(1), dateTime),
                new TimeTaggedValueRowViewModel(1.2, dateTime.AddHours(21.2), dateTime),
                new TimeTaggedValueRowViewModel(-2342, dateTime.AddDays(1), dateTime),
                new TimeTaggedValueRowViewModel(38831.2, dateTime.AddDays(1.2), dateTime),
            };

            viewModel.SelectedTimeStep = 1;
            viewModel.SelectedTimeUnit = TimeUnit.MilliSecond;
            viewModel.Values.AddRange(newValues.OrderBy(x => x.TimeDelta).ToList());
            Assert.IsEmpty(viewModel.SelectedValues);
            viewModel.ApplyTimeStep();
            Assert.IsNotEmpty(viewModel.SelectedValues);
            Assert.AreEqual(14, viewModel.SelectedValues.Count);

            viewModel.SelectedTimeStep = 1;
            viewModel.SelectedTimeUnit = TimeUnit.Second;
            viewModel.ApplyTimeStep();
            Assert.AreEqual(9, viewModel.SelectedValues.Count);

            viewModel.SelectedTimeStep = 1;
            viewModel.SelectedTimeUnit = TimeUnit.Hour;
            viewModel.ApplyTimeStep();
            Assert.AreEqual(5, viewModel.SelectedValues.Count);

            viewModel.SelectedTimeStep = 1;
            viewModel.SelectedTimeUnit = TimeUnit.Day;
            viewModel.ApplyTimeStep();
            Assert.AreEqual(2, viewModel.SelectedValues.Count);

            viewModel.SelectedTimeStep = 0;
            viewModel.SelectedTimeUnit = TimeUnit.Day;
            viewModel.ApplyTimeStep();
            Assert.AreEqual(14, viewModel.SelectedValues.Count);

            viewModel.SelectedTimeStep = 3;
            viewModel.SelectedTimeUnit = (TimeUnit)254;
            Assert.Throws<ArgumentOutOfRangeException>(() => viewModel.ApplyTimeStep());
        }
    }
}
