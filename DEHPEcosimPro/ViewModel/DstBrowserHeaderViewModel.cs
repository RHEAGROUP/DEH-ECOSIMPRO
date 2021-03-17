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
    using System.Reactive.Linq;

    using CDP4Dal;

    using DEHPCommon.Enumerators;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Events;
    using DEHPEcosimPro.ViewModel.Interfaces;
    using DEHPEcosimPro.Views;

    using Opc.Ua;

    using ReactiveUI;

    /// <summary>
    /// The view model for <see cref="DstBrowserHeader"/>
    /// </summary>
    public class DstBrowserHeaderViewModel : ReactiveObject, IDstBrowserHeaderViewModel
    {
        /// <summary>
        /// The <see cref="NodeId"/> of the ServerStatus.CurrentTime node in an OPC session
        /// </summary>
        private readonly NodeId currentServerTimeNodeId = new NodeId(Variables.Server_ServerStatus_CurrentTime);

        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel"/> instance
        /// </summary>
        private readonly IStatusBarControlViewModel statusBarControlViewModel;

        /// <summary>
        /// Backing field for <see cref="ServerAddress"/>
        /// </summary>
        private string serverAddress;

        /// <summary>
        /// Backing field for <see cref="SamplingInterval"/>
        /// </summary>
        private int samplingInterval;

        /// <summary>
        /// Backing field for <see cref="VariablesCount"/>
        /// </summary>
        private int variablesCount;

        /// <summary>
        /// Backing field for <see cref="ServerStartTime"/> 
        /// </summary>
        private DateTime? serverStartTime;

        /// <summary>
        /// Backing field for <see cref="CurrentServerTime"/> 
        /// </summary>
        private DateTime? currentServerTime;

        /// <summary>
        /// Backing field for <see cref="SelectedStopStep"/> 
        /// </summary>
        private double selectedStopStep = 5;

        /// <summary>
        /// Backing field for <see cref="SelectedStepping"/> 
        /// </summary>
        private double selectedStepping = .01;

        /// <summary>
        /// Backing field for <see cref="IsExperimentRunning"/> 
        /// </summary>
        private bool isExperimentRunning;

        /// <summary>
        /// Backing field for <see cref="ExperimentProgress"/> 
        /// </summary>
        private double experimentProgress;

        /// <summary>
        /// Backing field for <see cref="ExperimentButtonText"/> 
        /// </summary>
        private string experimentButtonText = "Run the experiment";

        /// <summary>
        /// The <see cref="NodeId"/> of CINT
        /// </summary>
        private NodeId steppingNodeId;

        /// <summary>
        /// The <see cref="NodeId"/> of TSTOP
        /// </summary>
        private NodeId stopStepNodeId;

        /// <summary>
        /// Backing field for <see cref="ExperimentTime"/>
        /// </summary>
        private double experimentTime;
        
        /// <summary>
        /// The <see cref="Stopwatch"/>
        /// </summary>
        private Stopwatch stopWatch;

        /// <summary>
        /// Backing field for <see cref="AreTimeStepAnStepTimeEditable"/>
        /// </summary>
        private bool areTimeStepAnStepTimeEditable;

        /// <summary>
        /// Bqcking field for <see cref="CanRunExperiment"/>
        /// </summary>
        private bool canRunExperiment;

        /// <summary>
        /// Initializes a new instance of the <see cref="DstBrowserHeaderViewModel"/>
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="statusBarControlViewModel">The <see cref="IStatusBarControlViewModel"/></param>
        public DstBrowserHeaderViewModel(IDstController dstController, IStatusBarControlViewModel statusBarControlViewModel)
        {
            this.dstController = dstController;
            this.statusBarControlViewModel = statusBarControlViewModel;

            this.WhenAnyValue(x => x.dstController.IsSessionOpen)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateProperties());

            CDPMessageBus.Current.Listen<OpcVariableChangedEvent>().Where(x => Equals(x.Id, this.currentServerTimeNodeId.Identifier))
                .Subscribe(e => this.CurrentServerTime = (DateTime)e.Value);

            var canCallServerMethods = this.WhenAnyValue(vm => vm.dstController.IsSessionOpen);

            this.CallRunMethodCommand = ReactiveCommand.Create(canCallServerMethods);
            this.CallRunMethodCommand.Subscribe(_ => this.CallServerMethod("method_run"));

            this.CallResetMethodCommand = ReactiveCommand.Create(canCallServerMethods);
            this.CallResetMethodCommand.Subscribe(_ => this.CallServerMethod("method_reset"));
        }

        /// <summary>
        /// Gets the token that stops the experiment
        /// </summary>
        public CancellationTokenSource CancelToken { get; set; }

        /// <summary>
        /// A value indicating whether the <see cref="CallRunMethodCommand"/> can execute
        /// </summary>
        public string ServerAddress
        {
            get => this.serverAddress;
            set => this.RaiseAndSetIfChanged(ref this.serverAddress, value);
        }

        /// <summary>
        /// Gets or sets the time, in milliseconds, between which data is recorded
        /// </summary>
        public int SamplingInterval
        {
            get => this.samplingInterval;
            set => this.RaiseAndSetIfChanged(ref this.samplingInterval, value);
        }

        /// <summary>
        /// Gets or sets the total number of variables in the open session
        /// </summary>
        public int VariablesCount
        {
            get => this.variablesCount;
            set => this.RaiseAndSetIfChanged(ref this.variablesCount, value);
        }

        /// <summary>
        /// Gets or sets the date and time, in UTC, from which the server has been up and running
        /// </summary>
        public DateTime? ServerStartTime
        {
            get => this.serverStartTime;
            set => this.RaiseAndSetIfChanged(ref this.serverStartTime, value);
        }

        /// <summary>
        /// Gets or sets the current date/time, in UTC, of the server
        /// </summary>
        public DateTime? CurrentServerTime
        {
            get => this.currentServerTime;
            set => this.RaiseAndSetIfChanged(ref this.currentServerTime, value);
        }

        /// <summary>
        /// <see cref="ReactiveCommand{T}"/> for calling the 'Run' server method
        /// </summary>
        public ReactiveCommand<object> CallRunMethodCommand { get; set; }

        /// <summary>
        /// <see cref="ReactiveCommand{T}"/> for calling the 'Reset' server method
        /// </summary>
        public ReactiveCommand<object> CallResetMethodCommand { get; set; }

        /// <summary>
        /// Updates the view model's properties
        /// </summary>
        public void UpdateProperties()
        {
            if (this.dstController.IsSessionOpen)
            {
                this.ServerAddress = this.dstController.ServerAddress;
                this.SamplingInterval = this.dstController.RefreshInterval;
                this.VariablesCount = this.dstController.Variables.Count;
                this.ServerStartTime = this.dstController.GetServerStartTime();
                this.CurrentServerTime = this.dstController.GetCurrentServerTime();

                this.dstController.AddSubscription(this.currentServerTimeNodeId);
            }
            else
            {
                this.ExperimentButtonText = "Run";
                this.ExperimentProgress = 0;
                this.IsExperimentRunning = false;
                this.CancelToken = null;
                this.ServerAddress = string.Empty;
                this.SamplingInterval = 0;
                this.VariablesCount = 0;
                this.ServerStartTime = null;
                this.CurrentServerTime = null;

                this.dstController.ClearSubscriptions();
            }
        }

        /// <summary>
        /// Calls a server method and reports its execution state to the represented status bar
        /// </summary>
        /// <param name="methodBrowseName">The BrowseName of the server method</param>
        private void CallServerMethod(string methodBrowseName)
        {
            if (string.IsNullOrEmpty(methodBrowseName))
            {
                this.CancelToken?.Cancel();
                return;
            }

            else if (Math.Abs(this.ExperimentTime - this.SelectedStopStep) <= 0)
            {
                var result = this.navigationService
                    .ShowDxDialog<ExperimentResetAndReRunConfirmDialog>();

                if (result != true)
                {
                    return;
                }

                this.Reset(false);
            }

            if(this.dstController.WriteToDst(this.stopStepNodeId, this.SelectedStopStep) 
               && this.dstController.WriteToDst(this.steppingNodeId, this.SelectedStepping))
            {
                this.IsExperimentRunning = true;
                this.stopWatch = new Stopwatch();
                this.stopWatch.Start();
                this.CancelToken = new CancellationTokenSource();

                Task.Run(this.RunExperimentTask, this.CancelToken.Token).ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            this.logger.Error(t.Exception);
                        }
                    });
            }
        }

        /// <summary>
        /// Loops through all steps starting at <see cref="ExperimentTime"/> and calls <see cref="CallNextCint"/>
        /// </summary>
        public void RunExperimentTask()
        {
            for (var step = this.ExperimentTime; step <= this.selectedStopStep / this.selectedStepping; step++)
            {
                this.CallNextCint();
            }
        }

        /// <summary>
        /// Action on each timer call back
        /// </summary>
        private void CallNextCint()
        {
            if (this.CancelToken.IsCancellationRequested || Math.Abs(this.ExperimentTime - this.SelectedStopStep) <= 0)
            {
                this.EndRun();
            }
            else
            {
                this.ExperimentButtonText = $"Running ({this.ExperimentProgress}%) Press to pause";
                this.dstController.GetNextExperimentStep();
            }
        }

        /// <summary>
        /// Resets experiment variables
        /// </summary>
        private void EndRun()
        {
            this.ExperimentButtonText = this.ExperimentTime < this.SelectedStopStep ? $"Paused ({ this.ExperimentProgress}%) Press to Continue" : "Run";
            this.IsExperimentRunning = false;

            if (this.stopWatch is {})
            {
                this.stopWatch.Stop();
                this.statusBarControlViewModel.Append($"Experiment has run in {this.stopWatch.ElapsedMilliseconds} ms");
                this.stopWatch = null;
            }
        }

        /// <summary>
        /// Executes the <see cref="CallResetMethodCommand"/> by calling the OPC reset method and reports its execution state to the represented status bar
        /// </summary>
        /// <param name="showWarn">A value indicating whether a warning should be displayed allowing the user to cancel</param>
        public void Reset(bool showWarn = true)
        {
            try
            {
                var callMethodResult = this.dstController.CallServerMethod(methodBrowseName);

                if (callMethodResult != null)
                {
                    this.statusBarControlViewModel.Append($"{string.Join(", ", callMethodResult)} executed successfully.");
                }
                else
                {
                    this.statusBarControlViewModel.Append($"No method was found with the BrowseName '{methodBrowseName}'", StatusBarMessageSeverity.Error);
                }
            }
            catch (Exception exception)
            {
                this.statusBarControlViewModel.Append($"Executing method {methodBrowseName} failed: {exception.Message}", StatusBarMessageSeverity.Error);
            }
        }
    }
}
