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
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using CDP4Dal;

    using DEHPCommon.Enumerators;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Events;
    using DEHPEcosimPro.ViewModel.Interfaces;
    using DEHPEcosimPro.Views;
    using DEHPEcosimPro.Views.Dialogs;

    using NLog;

    using Opc.Ua;

    using ReactiveUI;

    /// <summary>
    /// The view model for <see cref="DstBrowserHeader"/>
    /// </summary>
    public class DstBrowserHeaderViewModel : ReactiveObject, IDstBrowserHeaderViewModel
    {
        /// <summary>
        /// The <see cref="NLog"/> logger
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

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
        /// The <see cref="INavigationService"/>
        /// </summary>
        private readonly INavigationService navigationService;

        /// <summary>
        /// Backing field for <see cref="ServerAddress"/>
        /// </summary>
        private string serverAddress;

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
        /// Saves the initial value of <see cref="SelectedStepping"/> before the experiment runs
        /// </summary>
        private double initialSelectedStepping;
        
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
        /// Backing field for <see cref="CanRunExperiment"/>
        /// </summary>
        private bool canRunExperiment;

        /// <summary>
        /// Backing field for <see cref="IsExperimentRunning"/>
        /// </summary>
        private bool isExperimentRunning;

        /// <summary>
        /// Gets or sets a value indicating whether the experiment is running
        /// </summary>
        public bool IsExperimentRunning
        {
            get => this.isExperimentRunning;
            set => this.RaiseAndSetIfChanged(ref this.isExperimentRunning, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DstBrowserHeaderViewModel"/>
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="statusBarControlViewModel">The <see cref="IStatusBarControlViewModel"/></param>
        /// <param name="navigationService">The <see cref="INavigationService"/></param>
        public DstBrowserHeaderViewModel(IDstController dstController, IStatusBarControlViewModel statusBarControlViewModel,
        INavigationService navigationService)
        {
            this.dstController = dstController;
            this.statusBarControlViewModel = statusBarControlViewModel;
            this.navigationService = navigationService;

            this.WhenAnyValue(x => x.dstController.IsSessionOpen)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.UpdateProperties());

            CDPMessageBus.Current.Listen<OpcVariableChangedEvent>().Where(x => Equals(x.Id, this.currentServerTimeNodeId.Identifier))
                .Subscribe(e => this.CurrentServerTime = (DateTime)e.Value);

            this.WhenAnyValue(x => x.SelectedStepping, x => x.SelectedStopStep)
                .Subscribe(_ => this.UpdateCanRunExperiment());

            this.CallRunMethodCommand = ReactiveCommand.Create(
                this.WhenAnyValue(x => x.CanRunExperiment));
            
            this.CallRunMethodCommand.Subscribe(_ => this.RunExperiment());

            this.CallResetMethodCommand = ReactiveCommand.Create(
                this.WhenAnyValue(vm => vm.dstController.IsSessionOpen));
            
            this.CallResetMethodCommand.Subscribe(_ => this.Reset());

            this.WhenAnyValue(x => x.dstController.IsExperimentRunning)
                .Subscribe(x => this.IsExperimentRunning = x);

            this.WhenAny(x => x.ExperimentTime, 
                x => x.dstController.IsSessionOpen,
                    (time, isConnected)
                                => Math.Abs(time.Value) <= 0 && isConnected.Value)
                .Subscribe(x => this.AreTimeStepAnStepTimeEditable = x);
        }

        /// <summary>
        /// Gets the token that stops the experiment
        /// </summary>
        public CancellationTokenSource CancelToken { get; set; }

        /// <summary>
        /// A value indicating whether the <see cref="CallRunMethodCommand"/> can execute
        /// </summary>
        public bool CanRunExperiment
        {
            get => this.canRunExperiment;
            set => this.RaiseAndSetIfChanged(ref this.canRunExperiment, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the TimeStep and the StepTime are editable
        /// </summary>
        public bool AreTimeStepAnStepTimeEditable
        {
            get => this.areTimeStepAnStepTimeEditable;
            set => this.RaiseAndSetIfChanged(ref this.areTimeStepAnStepTimeEditable, value);
        }

        /// <summary>
        /// Gets or sets the experiment TIME
        /// </summary>
        public double ExperimentTime
        {
            get => this.experimentTime;
            set
            {
                this.RaiseAndSetIfChanged(ref this.experimentTime, value);

                if (this.dstController.IsExperimentRunning)
                {
                    this.ExperimentProgress = Math.Round((value / this.SelectedStopStep) * 100, 2);
                }
            }
        }

        /// <summary>
        /// Gets or sets the experiment button text
        /// </summary>
        public string ExperimentButtonText
        {
            get => this.experimentButtonText;
            set => this.RaiseAndSetIfChanged(ref this.experimentButtonText, value);
        }
        
        /// <summary>
        /// Gets or sets the experiment progress value
        /// </summary>
        public double ExperimentProgress
        {
            get => this.experimentProgress;
            set => this.RaiseAndSetIfChanged(ref this.experimentProgress, value);
        }

        /// <summary>
        /// Gets or sets the CINT of the experiment
        /// </summary>
        public double SelectedStepping
        {
            get => this.selectedStepping;
            set => this.RaiseAndSetIfChanged(ref this.selectedStepping, value);
        }
        
        /// <summary>
        /// Gets or sets the TSTOP of the experiment
        /// </summary>
        public double SelectedStopStep
        {
            get => this.selectedStopStep;
            set => this.RaiseAndSetIfChanged(ref this.selectedStopStep, value);
        }
        
        /// <summary>
        /// Gets or sets the URI of the connected data source
        /// </summary>
        public string ServerAddress
        {
            get => this.serverAddress;
            set => this.RaiseAndSetIfChanged(ref this.serverAddress, value);
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
        /// Gets the <see cref="NodeId"/>s of CINT and TSTOP
        /// </summary>
        private void GetTstopCintNodeId()
        {
            this.stopStepNodeId = (NodeId)this.dstController.References
                .FirstOrDefault(x => x.BrowseName.Name == "TSTOP")?.NodeId;

            this.steppingNodeId = (NodeId)this.dstController.References
                .FirstOrDefault(x => x.BrowseName.Name == "CINT")?.NodeId;

            CDPMessageBus.Current.Listen<OpcVariableChangedEvent>()
                .Where(x => x.Id == this.dstController.TimeNodeId.Identifier)
                .Subscribe(x => this.ExperimentTime = Convert.ToDouble(x.Value));
        }

        /// <summary>
        /// Updates the view model's properties
        /// </summary>
        public void UpdateProperties()
        {
            this.UpdateCanRunExperiment();

            if (this.dstController.IsSessionOpen)
            {
                this.ExperimentTime = Convert.ToDouble(
                    this.dstController.ReadNode(new ReferenceDescription() { NodeId = this.dstController.TimeNodeId }).Value);

                this.ServerAddress = this.dstController.ServerAddress;
                this.VariablesCount = this.dstController.Variables.Count;
                this.ServerStartTime = this.dstController.GetServerStartTime();
                this.CurrentServerTime = this.dstController.GetCurrentServerTime();
                this.dstController.AddSubscription(this.currentServerTimeNodeId);
                this.GetTstopCintNodeId();
            }
            else
            {
                this.ExperimentButtonText = "Run";
                this.ExperimentProgress = 0;
                this.dstController.IsExperimentRunning = false;
                this.CancelToken = null;
                this.ServerAddress = string.Empty;
                this.VariablesCount = 0;
                this.ServerStartTime = null;
                this.CurrentServerTime = null;
                this.dstController.ClearSubscriptions();
            }
        }

        /// <summary>
        /// Updates the <see cref="canRunExperiment"/>
        /// </summary>
        private void UpdateCanRunExperiment()
        {
            this.CanRunExperiment = this.dstController.IsSessionOpen
                                    && this.SelectedStepping > 0
                                    && this.SelectedStopStep > 0;
        }

        /// <summary>
        /// Runs the experiment
        /// </summary>
        public void RunExperiment()
        {
            if (this.dstController.IsExperimentRunning)
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
                this.dstController.IsExperimentRunning = true;
                this.stopWatch = new Stopwatch();
                this.stopWatch.Start();
                this.CancelToken = new CancellationTokenSource();
                this.initialSelectedStepping = this.SelectedStepping;

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
            if (this.CancelToken.IsCancellationRequested || Math.Abs(this.ExperimentTime - this.SelectedStopStep) <= 0 )
            {
                this.EndRun();
            }
            else
            {
                this.ExperimentButtonText = $"Running ({this.ExperimentProgress}%) Press to pause";

                if (this.ExperimentTime + this.SelectedStepping > this.SelectedStopStep)
                { 
                    this.SelectedStepping = this.SelectedStopStep - this.ExperimentTime;
                }

                this.dstController.GetNextExperimentStep();
            }
        }

        /// <summary>
        /// Resets experiment variables
        /// </summary>
        private void EndRun()
        {
            this.ExperimentButtonText = this.ExperimentTime < this.SelectedStopStep ? $"Paused ({ this.ExperimentProgress}%) Press to Continue" : "Run";
            this.dstController.IsExperimentRunning = false;
            this.SelectedStepping = this.initialSelectedStepping;

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
                var result = showWarn 
                    ? this.navigationService.ShowDxDialog<ExperimentResetWarningDialog>()
                    : true;

                if (result != true)
                {
                    return;
                }
                
                var callMethodResult = this.dstController.CallServerMethod("method_reset");

                if (callMethodResult != null)
                {
                    this.statusBarControlViewModel.Append($"Reset executed successfully.");
                    this.dstController.ResetVariables();
                    this.dstController.ReTransferMappedThingsToDst();
                    this.ExperimentButtonText = "Run";
                }
                else
                {
                    this.statusBarControlViewModel.Append("No method was found with the name 'method_reset'", StatusBarMessageSeverity.Error);
                }
            }
            catch (Exception exception)
            {
                this.statusBarControlViewModel.Append($"Executing method Reset failed: {exception.Message}", StatusBarMessageSeverity.Error);
            }
        }
    }
}
