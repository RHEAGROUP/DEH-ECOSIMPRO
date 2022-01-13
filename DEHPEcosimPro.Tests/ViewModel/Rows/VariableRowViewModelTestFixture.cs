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

    using CDP4Common.SiteDirectoryData;

    using CDP4Dal;

    using DEHPEcosimPro.Events;
    using DEHPEcosimPro.ViewModel.Rows;

    using DevExpress.Xpf.Editors.Helpers;

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
            Assert.AreEqual(value, viewModel.ActualValue);
            Assert.IsNotEmpty(viewModel.Values);
            Assert.AreEqual(value, viewModel.InitialValue);
            Assert.IsNull(viewModel.AverageValue);
            Assert.IsNull(viewModel.SelectedScale);
            Assert.IsNull(viewModel.IsVariableMappingValid);
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
                new TimeTaggedValueRowViewModel(131234, .01), 
                new TimeTaggedValueRowViewModel(-143298.5224323, .01),
                new TimeTaggedValueRowViewModel(2u, .01),
                new TimeTaggedValueRowViewModel(44.87613, .01),
                new TimeTaggedValueRowViewModel(0.42e2, .01),
                new TimeTaggedValueRowViewModel(.12387, .01),
                new TimeTaggedValueRowViewModel(2ul, .01)
            };

            viewModel.Values.AddRange(newValues);

            Assert.AreEqual(-1496.6653040374986d, viewModel.ComputeAverageValue());
            viewModel.Values.Add(new TimeTaggedValueRowViewModel("15%", .02));
            Assert.AreEqual("-", viewModel.ComputeAverageValue());
        }

        [Test]
        public void VerifyDiscreetSampling()
        {
            var viewModel = new VariableRowViewModel((new ReferenceDescription()
            {
                NodeId = new ExpandedNodeId(Guid.NewGuid()),
                DisplayName = new LocalizedText("", "DummyVariable0")
            }, new DataValue() { Value = .2}));

            var newValues = new List<TimeTaggedValueRowViewModel>()
            {
                new TimeTaggedValueRowViewModel(13123324, .01),
                new TimeTaggedValueRowViewModel(-98.52243, .02),
                new TimeTaggedValueRowViewModel(292312443u, .03),
                new TimeTaggedValueRowViewModel(44.87613, .04),
                new TimeTaggedValueRowViewModel(0.432e2, .05),
                new TimeTaggedValueRowViewModel(.12387, .06),
                new TimeTaggedValueRowViewModel(223ul, .07),
                new TimeTaggedValueRowViewModel(67ul, .08),
                new TimeTaggedValueRowViewModel(34, .09),
                new TimeTaggedValueRowViewModel(1, .1),
                new TimeTaggedValueRowViewModel(1.2, .11),
                new TimeTaggedValueRowViewModel(-2342, .12),
                new TimeTaggedValueRowViewModel(38831.2, .13),
            };

            viewModel.SelectedTimeStep = 1;
            viewModel.Values.AddRange(newValues.OrderBy(x => x.TimeStep).ToList());
            Assert.IsEmpty(viewModel.SelectedValues);
            viewModel.ApplyTimeStep();
            Assert.IsNotEmpty(viewModel.SelectedValues);
            Assert.AreEqual(1, viewModel.SelectedValues.Count);

            viewModel.SelectedTimeStep = .01;
            viewModel.ApplyTimeStep();
            Assert.AreEqual(14, viewModel.SelectedValues.Count);

            viewModel.SelectedTimeStep = .1;
            viewModel.ApplyTimeStep();
            Assert.AreEqual(2, viewModel.SelectedValues.Count);

            viewModel.SelectedTimeStep = 1;
            viewModel.ApplyTimeStep();
            Assert.AreEqual(1, viewModel.SelectedValues.Count);

            viewModel.SelectedTimeStep = 0;
            viewModel.ApplyTimeStep();
            Assert.AreEqual(14, viewModel.SelectedValues.Count);
        }

        [Test]
        public void VerifyAveragedSampling()
        {
            var viewModel = new VariableRowViewModel((new ReferenceDescription()
            {
                NodeId = new ExpandedNodeId(Guid.NewGuid()),
                DisplayName = new LocalizedText("", "DummyVariable0")
            }, new DataValue() { Value = .2 }));

            var newValues = new List<TimeTaggedValueRowViewModel>()
            {
                new TimeTaggedValueRowViewModel(2, 0),
                new TimeTaggedValueRowViewModel(3, 1),
                new TimeTaggedValueRowViewModel(2, 2),
                new TimeTaggedValueRowViewModel(1, 3),
                new TimeTaggedValueRowViewModel(0, 4),
                new TimeTaggedValueRowViewModel(0, 5),
                new TimeTaggedValueRowViewModel(1, 6),
                new TimeTaggedValueRowViewModel(3, 7),
                new TimeTaggedValueRowViewModel(4, 8)
            };

            viewModel.SelectedTimeStep = 3;
            viewModel.Values.Clear();
            viewModel.Values.AddRange(newValues.OrderBy(x => x.TimeStep).ToList());
            Assert.IsEmpty(viewModel.SelectedValues);
            viewModel.IsAveraged = true;

            viewModel.ApplyTimeStep();
            Assert.IsNotEmpty(viewModel.SelectedValues);
            Assert.AreEqual(3, viewModel.SelectedValues.Count);

            Assert.AreEqual(0, viewModel.SelectedValues[0].TimeStep);
            Assert.AreEqual(2.333, Math.Round(viewModel.SelectedValues[0].AveragedValue.TryConvertToDouble(), 3));

            Assert.AreEqual(3, viewModel.SelectedValues[1].TimeStep);
            Assert.AreEqual(0.333, Math.Round(viewModel.SelectedValues[1].AveragedValue.TryConvertToDouble(), 3));

            Assert.AreEqual(6, viewModel.SelectedValues[2].TimeStep);
            Assert.AreEqual(2.667, Math.Round(viewModel.SelectedValues[2].AveragedValue.TryConvertToDouble(), 3));

            viewModel.SelectedTimeStep = 2;
            viewModel.Values.Clear();
            viewModel.SelectedValues.Clear();
            viewModel.Values.AddRange(newValues.OrderBy(x => x.TimeStep).ToList());

            viewModel.ApplyTimeStep();
            Assert.IsNotEmpty(viewModel.SelectedValues);
            Assert.AreEqual(5, viewModel.SelectedValues.Count);

            Assert.AreEqual(0, viewModel.SelectedValues[0].TimeStep);
            Assert.AreEqual((2d + 3d) / 2d, Math.Round(viewModel.SelectedValues[0].AveragedValue.TryConvertToDouble(), 3));

            Assert.AreEqual(2, viewModel.SelectedValues[1].TimeStep);
            Assert.AreEqual((2d + 1d) / 2d, Math.Round(viewModel.SelectedValues[1].AveragedValue.TryConvertToDouble(), 3));

            Assert.AreEqual(4, viewModel.SelectedValues[2].TimeStep);
            Assert.AreEqual((0d + 0d) / 2d, Math.Round(viewModel.SelectedValues[2].AveragedValue.TryConvertToDouble(), 3));

            Assert.AreEqual(6, viewModel.SelectedValues[3].TimeStep);
            Assert.AreEqual((1d + 3d) / 2d, Math.Round(viewModel.SelectedValues[3].AveragedValue.TryConvertToDouble(), 3));

            Assert.AreEqual(8, viewModel.SelectedValues[4].TimeStep);
            Assert.AreEqual(4d, Math.Round(viewModel.SelectedValues[4].AveragedValue.TryConvertToDouble(), 3));
        }

        [Test]
        public void VerifyIsValid()
        {
            var viewModel = new VariableRowViewModel((new ReferenceDescription()
            {
                NodeId = new ExpandedNodeId(Guid.NewGuid()),
                DisplayName = new LocalizedText("", "DummyVariable0")
            }, new DataValue() { Value = .2 }));

            Assert.IsFalse(viewModel.IsValid());
            Assert.IsNull(viewModel.IsVariableMappingValid);
            viewModel.SelectedValues.Add(new TimeTaggedValueRowViewModel(131234, .01));
            Assert.IsFalse(viewModel.IsValid());
            Assert.IsNull(viewModel.IsVariableMappingValid);
            viewModel.SelectedParameterType = new DateTimeParameterType();
            Assert.IsTrue(viewModel.IsValid());
            Assert.IsFalse(viewModel.IsVariableMappingValid);

            var sampledFunctionParameterType = new SampledFunctionParameterType(Guid.NewGuid(), null, null)
            {
                Name = "TextXQuantity",
                IndependentParameterType =
                {
                    new IndependentParameterTypeAssignment(Guid.NewGuid(), null, null)
                    {
                        ParameterType = new SimpleQuantityKind(Guid.NewGuid(), null, null)
                        {
                            Name = "Time", PossibleScale =
                            {
                                new RatioScale() { Name = "millisecond" },
                                new RatioScale() { Name = "second" },
                                new RatioScale() { Name = "minute" },
                                new RatioScale() { Name = "hour" },
                                new RatioScale() { Name = "Day" }
                            }
                        }
                    }
                },

                DependentParameterType =
                {
                    new DependentParameterTypeAssignment(Guid.NewGuid(),null,null)
                    {
                        ParameterType = new DateTimeParameterType(Guid.NewGuid(),null,null)
                        {
                            Name = "DependentDateTime"
                        }
                    }
                }
            };

            viewModel.SelectedParameterType = sampledFunctionParameterType;
            Assert.IsTrue(viewModel.IsValid());
            Assert.IsFalse(viewModel.IsVariableMappingValid);

            sampledFunctionParameterType.DependentParameterType.Clear();
            var scale = new RatioScale() { NumberSet = NumberSetKind.REAL_NUMBER_SET };

            sampledFunctionParameterType.DependentParameterType.Add(
                new DependentParameterTypeAssignment(Guid.NewGuid(), null, null)
            {
                ParameterType = new SimpleQuantityKind(Guid.NewGuid(), null, null)
                {
                    Name = "DependentQuantityKing", DefaultScale = scale, PossibleScale = { scale }
                }
            });

            viewModel.SelectedScale = scale;
            Assert.IsTrue(viewModel.IsValid());
            Assert.IsTrue(viewModel.IsVariableMappingValid);
            viewModel.SelectedParameterType = new SimpleQuantityKind() { PossibleScale = {scale}, DefaultScale = scale};
            Assert.IsTrue(viewModel.IsValid());
            Assert.IsTrue(viewModel.IsVariableMappingValid);
        }

        [Test]
        public void VerifyIntToDoubleConversion()
        {
            var id = Guid.NewGuid();
            const string name = "DummyVariable0";
            const int value = 2;

            var viewModel = new VariableRowViewModel((new ReferenceDescription()
            {
                NodeId = new ExpandedNodeId(id),
                DisplayName = new LocalizedText("", name)
            }, new DataValue() { Value = value, ServerTimestamp = DateTime.MinValue }));

            Assert.AreEqual(name, viewModel.Name);
            Assert.AreEqual(value, viewModel.ActualValue);
            Assert.AreNotEqual(value.GetType(), viewModel.ActualValue.GetType());
            Assert.IsNotEmpty(viewModel.Values);
            Assert.AreEqual(value, viewModel.InitialValue);
            Assert.IsNull(viewModel.AverageValue);
            Assert.IsNull(viewModel.SelectedScale);
            Assert.IsNull(viewModel.IsVariableMappingValid);
        }
    }
}
