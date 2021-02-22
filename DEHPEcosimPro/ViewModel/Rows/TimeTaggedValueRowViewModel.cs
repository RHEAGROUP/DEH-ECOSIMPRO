// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TimeTaggedValueRowViewModel.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2021 RHEA System S.A.
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

namespace DEHPEcosimPro.ViewModel.Rows
{
    using System;

    using ReactiveUI;

    /// <summary>
    /// The <see cref="TimeTaggedValueRowViewModel"/> represents one value with its associated timestamp
    /// </summary>
    public class TimeTaggedValueRowViewModel : ReactiveObject
    {
        /// <summary>
        /// Backing field for <see cref="Value"/>
        /// </summary>
        private object value;

        /// <summary>
        /// Gets the value of the represented reference
        /// </summary>
        public object Value
        {
            get => this.value;
            set => this.RaiseAndSetIfChanged(ref this.value, value);
        }

        /// <summary>
        /// Backing field for <see cref="TimeStamp"/>
        /// </summary>
        private DateTime timeStamp;

        /// <summary>
        /// Gets the value of the represented reference
        /// </summary>
        public DateTime TimeStamp
        {
            get => this.timeStamp;
            set => this.RaiseAndSetIfChanged(ref this.timeStamp, value);
        }

        /// <summary>
        /// Backing field for <see cref="TimeDelta"/>
        /// </summary>
        private TimeSpan timeDelta;

        /// <summary>
        /// Gets the value of the represented reference
        /// </summary>
        public TimeSpan TimeDelta
        {
            get => this.timeDelta;
            set => this.RaiseAndSetIfChanged(ref this.timeDelta, value);
        }

        /// <summary>
        /// Initializes a new <see cref="TimeTaggedValueRowViewModel"/>
        /// </summary>
        /// <param name="value">The <see cref="object"/> value</param>
        /// <param name="serverTimestamp">The <see cref="DateTime"/> timeStamp</param>
        /// <param name="timestampOfReference">The <see cref="TimeDelta"/></param>
        public TimeTaggedValueRowViewModel(object value, DateTime serverTimestamp, DateTime timestampOfReference = default)
        {
            this.Value = value;
            this.TimeStamp = serverTimestamp;
            this.TimeDelta = timestampOfReference == default ? default : serverTimestamp - timestampOfReference;
        }
    }
}
