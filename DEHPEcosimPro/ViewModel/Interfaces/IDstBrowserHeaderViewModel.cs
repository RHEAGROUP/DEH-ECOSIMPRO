// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IDstBrowserHeaderViewModel.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.ViewModel.Interfaces
{
    using System;
    using System.Reactive;

    using ReactiveUI;

    /// <summary>
    /// Interface definition for <see cref="DstBrowserHeaderViewModel"/>
    /// </summary>
    public interface IDstBrowserHeaderViewModel
    {
        /// <summary>
        /// Gets or sets a value indicating whether the TimeStep and the StepTime are editable
        /// </summary>
        bool AreTimeStepAnStepTimeEditable { get; set; }

        /// <summary>
        /// Gets or sets the experiment TIME
        /// </summary>
        double ExperimentTime { get; set; }

        /// <summary>
        /// Gets or sets the experiment button text
        /// </summary>
        string ExperimentButtonText { get; set; }

        /// <summary>
        /// Gets or sets the experiment progress value
        /// </summary>
        double ExperimentProgress { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the experiment is running
        /// </summary>
        bool IsExperimentRunning { get; set; }

        /// <summary>
        /// Gets or sets the CINT of the experiment
        /// </summary>
        double SelectedStepping { get; set; }

        /// <summary>
        /// Gets or sets the TSTOP of the experiment
        /// </summary>
        double SelectedStopStep { get; set; }

        /// <summary>
        /// Gets or sets the URI of the connected data source
        /// </summary>
        string ServerAddress { get; set; }

        /// <summary>
        /// Gets or sets the total number of variables in the open session
        /// </summary>
        int VariablesCount { get; set; }

        /// <summary>
        /// Gets or sets the date and time, in UTC, from which the server has been up and running
        /// </summary>
        DateTime? ServerStartTime { get; set; }

        /// <summary>
        /// Gets or sets the current date/time, in UTC, of the server
        /// </summary>
        DateTime? CurrentServerTime { get; set; }

        /// <summary>
        /// <see cref="ReactiveCommand{T}"/> for calling the 'Run' server method
        /// </summary>
        ReactiveCommand<object> CallRunMethodCommand { get; set; }

        /// <summary>
        /// <see cref="ReactiveCommand{T}"/> for calling the 'Reset' server method
        /// </summary>
        ReactiveCommand<object> CallResetMethodCommand { get; set; }

        /// <summary>
        /// Updates the view model's properties
        /// </summary>
        void UpdateProperties();
    }
}
