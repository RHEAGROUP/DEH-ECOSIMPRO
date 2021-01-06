// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstBrowserHeaderViewModel.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2020 RHEA System S.A.
// 
//    Author: Sam Gerené, Alex Vorobiev, Alexander van Delft, Nathanael Smiechowski, Ahmed Abulwafa Ahmed
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

namespace DEHPEcosimPro.ViewModel
{
    using System;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel.Interfaces;
    using DEHPEcosimPro.Views;

    using ReactiveUI;

    /// <summary>
    /// The view model for <see cref="DstBrowserHeader"/>
    /// </summary>
    public class DstBrowserHeaderViewModel : ReactiveObject, IDstBrowserHeaderViewModel
    {
        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// Backing field for <see cref="ServerAddress"/>
        /// </summary>
        private string serverAddress;

        /// <summary>
        /// Gets or sets the URI of the connected data source
        /// </summary>
        public string ServerAddress
        {
            get => this.serverAddress;
            set => this.serverAddress = value;
        }

        /// <summary>
        /// Backing field for <see cref="SamplingInterval"/>
        /// </summary>
        private int samplingInterval;

        /// <summary>
        /// Gets or sets the time, in milliseconds, between which data is recorded
        /// </summary>
        public int SamplingInterval
        {
            get => this.samplingInterval;
            set => this.samplingInterval = value;
        }

        /// <summary>
        /// Backing field for <see cref="VariablesCount"/>
        /// </summary>
        private int variablesCount;

        /// <summary>
        /// Gets or sets the total number of variables in the open session
        /// </summary>
        public int VariablesCount
        {
            get => this.variablesCount;
            set => this.variablesCount = value;
        }

        /// <summary>
        /// Backing field for <see cref="ServerUpFrom"/> 
        /// </summary>
        private DateTime serverUpFrom;

        /// <summary>
        /// Gets or sets the date and time from which the server has been up and running
        /// </summary>
        public DateTime ServerUpFrom
        {
            get => this.serverUpFrom;
            set => this.serverUpFrom = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DstBrowserHeaderViewModel"/>
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        public DstBrowserHeaderViewModel(IDstController dstController)
        {
            this.dstController = dstController;
        }

        /// <summary>
        /// Updates the view model's properties
        /// </summary>
        private void UpdateProperties()
        {

        }
    }
}
