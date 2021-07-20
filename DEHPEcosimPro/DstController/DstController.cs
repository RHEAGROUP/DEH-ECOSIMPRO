// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstController.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.DstController
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using System.Windows;

    using CDP4Common;
    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.Types;

    using CDP4Dal;
    using CDP4Dal.Operations;

    using DEHPCommon.Enumerators;
    using DEHPCommon.Events;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.Services.ExchangeHistory;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.Views;

    using DEHPEcosimPro.Enumerator;
    using DEHPEcosimPro.Events;
    using DEHPEcosimPro.Services.MappingConfiguration;
    using DEHPEcosimPro.Services.OpcConnector;
    using DEHPEcosimPro.Services.OpcConnector.Interfaces;
    using DEHPEcosimPro.Services.TypeResolver.Interfaces;
    using DEHPEcosimPro.ViewModel.Rows;
    
    using NLog;

    using Opc.Ua;

    using ReactiveUI;

    using INavigationService = DEHPCommon.Services.NavigationService.INavigationService;

    /// <summary>
    /// The <see cref="DstController"/> takes care of retrieving data from and to EcosimPro
    /// </summary>
    public class DstController : ReactiveObject, IDstController
    {
        /// <summary>
        /// Gets the current class logger
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger(); 
        
        /// <summary>
        /// The <see cref="IOpcClientService"/> that handles the OPC connection with EcosimPro
        /// </summary>
        private readonly IOpcClientService opcClientService;

        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IOpcSessionHandler"/>
        /// </summary>
        private readonly IOpcSessionHandler sessionHandler;

        /// <summary>
        /// Backing field for <see cref="IsSessionOpen"/>
        /// </summary>
        private bool isSessionOpen;

        /// <summary>
        /// The <see cref="IMappingEngine"/>
        /// </summary>
        private readonly IMappingEngine mappingEngine;

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel"/>
        /// </summary>
        private readonly IStatusBarControlViewModel statusBar;

        /// <summary>
        /// The <see cref="INavigationService"/>
        /// </summary>
        private readonly INavigationService navigation;

        /// <summary>
        /// The <see cref="IExchangeHistoryService"/>
        /// </summary>
        private readonly IExchangeHistoryService exchangeHistory;

        /// <summary>
        /// The <see cref="IObjectTypeResolverService"/>
        /// </summary>
        private readonly IObjectTypeResolverService objectTypeResolver;

        /// <summary>
        /// The <see cref="IMappingConfigurationService"/>
        /// </summary>
        private readonly IMappingConfigurationService mappingConfigurationService;

        /// <summary>
        /// Backing field for the <see cref="MappingDirection"/>
        /// </summary>
        private MappingDirection mappingDirection;

        /// <summary>
        /// The collection of (<see cref="NodeId"/> nodeId, <see cref="string"/> value) to be retransfered
        /// </summary>
        private readonly List<(NodeId NodeId, string Value)> transferedHubMapResult = new List<(NodeId NodeId, string Value)>();
        
        /// <summary>
        /// Backing field for <see cref="IsExperimentRunning"/> 
        /// </summary>
        private bool isExperimentRunning;

        /// <summary>
        /// Backing field for <see cref="CanLoadSelectedValues"/>
        /// </summary>
        private bool canLoadSelectedValues;

        /// <summary>
        /// A value indicating whether the selected values saved in the mapping can be loaded
        /// </summary>
        public bool CanLoadSelectedValues
        {
            get => this.canLoadSelectedValues;
            set => this.RaiseAndSetIfChanged(ref this.canLoadSelectedValues, value);
        }

        /// <summary>
        /// Gets this running tool name
        /// </summary>
        public static readonly string ThisToolName = typeof(DstController).Assembly.GetName().Name;

        /// <summary>
        /// Assert whether the <see cref="OpcSessionHandler.OpcSession"/> is Open
        /// </summary>
        public bool IsSessionOpen
        {
            get => this.isSessionOpen;
            set => this.RaiseAndSetIfChanged(ref this.isSessionOpen, value);
        }

        /// <summary>
        /// The endpoint url of the currently open session
        /// </summary>
        public string ServerAddress => this.opcClientService.EndpointUrl;

        /// <summary>
        /// The refresh interval for subscriptions in milliseconds
        /// </summary>
        public int RefreshInterval => this.opcClientService.RefreshInterval;

        /// <summary>
        /// Gets or sets the <see cref="MappingDirection"/>
        /// </summary>
        public MappingDirection MappingDirection
        {
            get => this.mappingDirection;
            set => this.RaiseAndSetIfChanged(ref this.mappingDirection, value);
        }

        /// <summary>
        /// Gets the references variables available from the connected OPC server
        /// </summary>
        public IList<(ReferenceDescription Reference, DataValue Node)> Variables { get; private set; } = new List<(ReferenceDescription, DataValue)>();

        /// <summary>
        /// Gets the Methods available from the connected OPC server
        /// </summary>
        public IList<ReferenceDescription> Methods { get; private set; } = new List<ReferenceDescription>();

        /// <summary>
        /// Gets the all references available from the connected OPC server
        /// </summary>
        public IList<ReferenceDescription> References => this.opcClientService.References;

        /// <summary>
        /// Gets the colection of mapped <see cref="Parameter"/>s And <see cref="ParameterOverride"/>s through their container
        /// </summary>
        public ReactiveList<ElementBase> DstMapResult { get; private set; } = new ReactiveList<ElementBase>();

        /// <summary>
        /// Gets the colection of <see cref="ElementBase"/> that are selected to be transfered
        /// </summary>
        public ReactiveList<ElementBase> SelectedDstMapResultToTransfer { get; private set; } = new ReactiveList<ElementBase>();

        /// <summary>
        /// Gets the colection of <see cref="MappedElementDefinitionRowViewModel"/> that are selected to be transfered
        /// </summary>
        public ReactiveList<MappedElementDefinitionRowViewModel> SelectedHubMapResultToTransfer { get; private set; } = new ReactiveList<MappedElementDefinitionRowViewModel>();

        /// <summary>
        /// Gets a <see cref="Dictionary{TKey, TValue}"/> of all mapped parameter and the associate <see cref="VariableRowViewModel"/>
        /// </summary>
        public Dictionary<ParameterOrOverrideBase, VariableRowViewModel> ParameterVariable { get; } = new Dictionary<ParameterOrOverrideBase, VariableRowViewModel>();

        /// <summary>
        /// Gets the colection of mapped <see cref="ReferenceDescription"/>
        /// </summary>
        public ReactiveList<MappedElementDefinitionRowViewModel> HubMapResult { get; private set; } = new ReactiveList<MappedElementDefinitionRowViewModel>();

        /// <summary>
        /// Gets the collection of <see cref="VariableRowViewModel"/>
        /// </summary>
        public ReactiveList<VariableRowViewModel> VariableRowViewModels { get; } = new ReactiveList<VariableRowViewModel>();
        
        /// <summary>
        /// Gets the OPC Time <see cref="NodeId"/>
        /// </summary>
        public NodeId TimeNodeId { get; private set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the experiment is running
        /// </summary>
        public bool IsExperimentRunning
        {
            get => this.isExperimentRunning;
            set => this.RaiseAndSetIfChanged(ref this.isExperimentRunning, value);
        }
        
        /// <summary>
        /// Initializes a new <see cref="DstController"/>
        /// </summary>
        /// <param name="opcClientService">The <see cref="IOpcClientService"/></param>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="sessionHandler">The <see cref="IOpcSessionHandler"/></param>
        /// <param name="mappingEngine">The <see cref="IMappingEngine"/></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel"/></param>
        /// <param name="navigationService">The <see cref="INavigationService"/></param>
        /// <param name="exchangeHistory">The <see cref="IExchangeHistoryService"/></param>
        /// <param name="objectTypeResolver">The <see cref="IObjectTypeResolverService"/></param>
        /// <param name="mappingConfigurationService">The <see cref="IMappingConfigurationService"/></param>
        public DstController(IOpcClientService opcClientService, IHubController hubController,
            IOpcSessionHandler sessionHandler, IMappingEngine mappingEngine,
            IStatusBarControlViewModel statusBar, INavigationService navigationService,
            IExchangeHistoryService exchangeHistory, IObjectTypeResolverService objectTypeResolver,
            IMappingConfigurationService mappingConfigurationService)
        {
            this.opcClientService = opcClientService;
            this.hubController = hubController;
            this.sessionHandler = sessionHandler;
            this.mappingEngine = mappingEngine;
            this.statusBar = statusBar;
            this.navigation = navigationService;
            this.exchangeHistory = exchangeHistory;
            this.objectTypeResolver = objectTypeResolver;
            this.mappingConfigurationService = mappingConfigurationService;
            this.InitializeObservables();
        }

        /// <summary>
        /// Initializes this <see cref="DstController"/> observable
        /// </summary>
        private void InitializeObservables()
        {
            this.WhenAnyValue(x => x.opcClientService.OpcClientStatusCode)
                .Subscribe(this.WhenOpcConnectionStatusChange);

            this.WhenAnyValue(x => x.IsExperimentRunning)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(this.WhenIsExperimentRunningChanged);
        }

        /// <summary>
        /// Updates the <see cref="CanLoadSelectedValues"/> and calls the <see cref="LoadMapping"/>
        /// </summary>
        /// <param name="isRunning">A value indicating whether the experiment is running</param>
        private void WhenIsExperimentRunningChanged(bool isRunning)
        {
            if (this.CanLoadSelectedValues && !isRunning)
            {
                var numberOfMappedThings = this.LoadMapping();
                this.statusBar.Append($"{numberOfMappedThings} mapped element(s) has been loaded from the saved mapping configuration " +
                                      $"{this.mappingConfigurationService.ExternalIdentifierMap.Name}");

                this.canLoadSelectedValues = false;
            }

            if (isRunning)
            {
                this.canLoadSelectedValues = true;
            }
        }

        /// <summary>
        /// Updates the <see cref="Variables"/> when ever the opc connection status changes
        /// </summary>
        /// <param name="clientStatusCode">The <see cref="OpcClientStatusCode"/></param>
        private void WhenOpcConnectionStatusChange(OpcClientStatusCode clientStatusCode)
        {
            try
            {
                var isOpcSessionOpen = clientStatusCode == OpcClientStatusCode.Connected;

                if (isOpcSessionOpen)
                {
                    foreach (var reference in this.opcClientService.References)
                    {
                        if (reference.NodeClass == NodeClass.Variable && reference.NodeId.NamespaceIndex == 4)
                        {
                            this.Variables.Add((reference, this.opcClientService.ReadNode((NodeId) reference.NodeId)));
                        }

                        else if (reference.NodeClass == NodeClass.Method)
                        {
                            this.Methods.Add(reference);
                        }
                    }

                    this.TimeNodeId = (NodeId) this.References.FirstOrDefault(x => x.BrowseName.Name == "TIME")?.NodeId;

                    this.VariableRowViewModels.AddRange(this.Variables.Select(r => new VariableRowViewModel(r)));
                    this.LoadMapping();
                }
                else
                {
                    this.Variables.Clear();
                    Application.Current.Dispatcher.Invoke(() => this.VariableRowViewModels.Clear());
                    this.Methods.Clear();
                    this.TimeNodeId = null;
                    this.ClearSubscriptions();
                }

                this.IsSessionOpen = isOpcSessionOpen;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Loads the saved mapping and applies the mapping rule to the <see cref="VariableRowViewModel"/>
        /// </summary>
        /// <returns>The number of mapped things loaded</returns>
        public int LoadMapping()
        {
            return this.LoadMappingToHub() + this.LoadMappingToDst();
        }

        /// <summary>
        /// Loads the saved mapping to the dst
        /// </summary>
        /// <returns>The number of mapped things loaded</returns>
        private int LoadMappingToDst()
        {
            if (this.mappingConfigurationService.LoadMappingFromHubToDst(this.VariableRowViewModels) is { } mappedElements
                            && mappedElements.Any())
            {
                mappedElements.ForEach(x => x.VerifyValidity());
                var validMappedElements = mappedElements.Where(x => x.IsValid).ToList();

                this.Map(validMappedElements);

                return validMappedElements.Count;
            }

            return 0;
        }

        /// <summary>
        /// Loads the saved mapping to the hub and applies the mapping rule
        /// </summary>
        /// <returns>The number of mapped things loaded</returns>
        private int LoadMappingToHub()
        {
            if (this.mappingConfigurationService.LoadMappingFromDstToHub(this.VariableRowViewModels) is { } mappedVariables
                && mappedVariables.Any())
            {
                this.mappingConfigurationService.SelectValues(mappedVariables);

                var validMappedVariables = new List<VariableRowViewModel>();

                Application.Current.Dispatcher.Invoke(() =>
                    validMappedVariables = mappedVariables.Where(x => x.IsValid()).ToList());

                if (validMappedVariables.Any())
                {
                    this.ParameterVariable.Clear();
                    this.Map(validMappedVariables);
                }

                return validMappedVariables.Count;
            }

            return 0;
        }

        /// <summary>
        /// Connects to the provided endpoint
        /// </summary>
        /// <param name="endpoint">The end point url eg. often opc.tcp:// representing the opc protocol</param>
        /// <param name="autoAcceptConnection">An assert whether the certificate should be auto accepted if valid</param>
        /// <param name="credential">The <see cref="IUserIdentity"/> default = null in case server does not require authentication</param>
        /// <param name="samplingInterval">The <see cref="int"/> sampling interval in millisecond</param>
        /// <returns>A <see cref="Task"/></returns>
        public async Task Connect(string endpoint, bool autoAcceptConnection = true, IUserIdentity credential = null, int samplingInterval = 1000)
        {
            this.opcClientService.RefreshInterval = samplingInterval;
            await this.opcClientService.Connect(endpoint, autoAcceptConnection, credential);
        }

        /// <summary>
        /// Reads and returns the server start time, in UTC, of the currently open session
        /// </summary>
        /// <returns>null if the session is closed or the ServerStatus.StartTime node was not found</returns>
        public DateTime? GetServerStartTime()
        {
            if (this.IsSessionOpen)
            {
                return (DateTime?)this.opcClientService.ReadNode(Opc.Ua.Variables.Server_ServerStatus_StartTime)?.Value;
            }

            return null;
        }

        /// <summary>
        /// Reads and returns the current server time, in UTC, of the currently open session
        /// </summary>
        /// <returns>null if the session is closed or the ServerStatus.CurrentTime node was not found</returns>
        public DateTime? GetCurrentServerTime()
        {
            if (this.IsSessionOpen)
            {
                return (DateTime?)this.opcClientService.ReadNode(Opc.Ua.Variables.Server_ServerStatus_CurrentTime)?.Value;
            }

            return null;
        }

        /// <summary>
        /// Adds one subscription for the <paramref name="nodeId"/>
        /// </summary>
        /// <param name="nodeId">The <see cref="NodeId"/></param>
        public void AddSubscription(NodeId nodeId)
        {
            this.opcClientService.AddSubscription(nodeId);
        }

        /// <summary>
        /// Adds one subscription for the <paramref name="reference"/>
        /// </summary>
        /// <param name="reference">The <see cref="ReferenceDescription"/></param>
        public void AddSubscription(ReferenceDescription reference)
        {
            this.AddSubscription((NodeId)reference.NodeId);
        }

        /// <summary>
        /// Calls the specified method and returns the output arguments.
        /// </summary>
        /// <param name="methodBrowseName">The BrowseName of the server method</param>
        /// <returns>The <see cref="IList{T}"/> of output argument values, or null if the no method was found with the provided BrowseName</returns>
        public IList<object> CallServerMethod(string methodBrowseName)
        {
            var serverMethodsNode = this.References.FirstOrDefault(r => r.BrowseName.Name == "server_methods")?.NodeId;
            var methodNode = this.Methods.FirstOrDefault(m => m.BrowseName.Name == methodBrowseName)?.NodeId;

            if (serverMethodsNode != null && methodNode != null)
            {
                return this.opcClientService.CallMethod(
                    new NodeId(serverMethodsNode.Identifier, serverMethodsNode.NamespaceIndex),
                    new NodeId(methodNode.Identifier, methodNode.NamespaceIndex),
                    string.Empty);
            }

            return default;
        }

        /// <summary>
        /// Removes all active subscriptions from the session.
        /// </summary>
        public void ClearSubscriptions()
        {
            this.sessionHandler.ClearSubscriptions();
        }

        /// <summary>
        /// Closes the <see cref="OpcSessionHandler.OpcSession"/>
        /// </summary>
        public void CloseSession()
        {
            this.Methods.Clear();
            this.Variables.Clear();
            this.transferedHubMapResult.Clear();
            this.opcClientService.CloseSession();
            this.IsSessionOpen = false;
        }

        /// <summary>
        /// Updates <see cref="ParameterVariable"/> by adding or replacing values
        /// </summary>
        /// <param name="parameterNodeIds">The  </param>
        private void UpdateParameterNodeId(Dictionary<ParameterOrOverrideBase, VariableRowViewModel> parameterNodeIds)
        {
            foreach (var keyValue in parameterNodeIds)
            {
                this.ParameterVariable[keyValue.Key] = keyValue.Value;
            }
        }

        /// <summary>
        /// Map the provided collection using the corresponding rule in the assembly and the <see cref="MappingEngine"/>
        /// </summary>
        /// <param name="dstVariables">The <see cref="List{T}"/> of <see cref="VariableRowViewModel"/> data</param>
        /// <returns>A <see cref="Task"/></returns>
        public void Map(List<VariableRowViewModel> dstVariables)
        {
            if (this.mappingEngine.Map(dstVariables) is (Dictionary<ParameterOrOverrideBase, VariableRowViewModel> parameterNodeIds, List<ElementBase> elements) && elements.Any())
            {
                this.UpdateParameterNodeId(parameterNodeIds);
                this.DstMapResult.AddRange(elements);
            }

            CDPMessageBus.Current.SendMessage(new UpdateObjectBrowserTreeEvent());
        }
        
        /// <summary>
        /// Map the provided collection using the corresponding rule in the assembly and the <see cref="MappingEngine"/>
        /// </summary>
        /// <param name="mappedElement">The <see cref="List{T}"/> of <see cref="MappedElementDefinitionRowViewModel"/></param>
        /// <returns>A <see cref="Task"/></returns>
        public void Map(List<MappedElementDefinitionRowViewModel> mappedElement)
        {
            if (mappedElement.Any())
            {
                this.HubMapResult.AddRange(mappedElement);
            }

            CDPMessageBus.Current.SendMessage(new UpdateDstVariableTreeEvent());
        }

        /// <summary>
        /// Transfers again all already transfered value to the dst in case of OPC server reset
        /// </summary>
        public void ReTransferMappedThingsToDst()
        {
            foreach (var (nodeId, value) in this.transferedHubMapResult)
            {
                this.opcClientService.WriteNode(nodeId,
                    double.Parse(value, CultureInfo.InvariantCulture), false);

                CDPMessageBus.Current.SendMessage(new OpcVariableChangedEvent()
                {
                    Id = nodeId.Identifier,
                    Value = value
                });
            }
        }

        /// <summary>
        /// Transfers the mapped variables to the Dst data source
        /// </summary>
        public async Task TransferMappedThingsToDst()
        {
            foreach (var mappedElement in this.SelectedHubMapResultToTransfer
                .Where(
                    mappedElement => this.opcClientService.WriteNode(
                        (NodeId) mappedElement.SelectedVariable.Reference.NodeId,
                        this.objectTypeResolver.Resolve(mappedElement.SelectedValue.Value)))
                .ToList())
            {
                CDPMessageBus.Current.SendMessage(new OpcVariableChangedEvent(mappedElement));

                this.UpdateTransferedHubMapResult(mappedElement);

                this.SelectedHubMapResultToTransfer.Remove(mappedElement);
                this.HubMapResult.Remove(mappedElement);

                this.mappingConfigurationService.AddToExternalIdentifierMap(mappedElement);

                this.exchangeHistory.Append(
                    $"Value [{mappedElement.SelectedValue.Representation}] from {mappedElement.SelectedParameter.ModelCode()} has been transfered to {mappedElement.SelectedVariable.Name}");
            }

            var (iteration, transaction) = this.GetIterationTransaction();
            this.mappingConfigurationService.PersistExternalIdentifierMap(transaction, iteration);
            transaction.CreateOrUpdate(iteration);
            await this.hubController.Write(transaction);
            await this.hubController.Refresh();
            this.mappingConfigurationService.RefreshExternalIdentifierMap();

            this.LoadMappingToDst();

            CDPMessageBus.Current.SendMessage(new UpdateDstVariableTreeEvent(true));
        }
        
        /// <summary>
        /// Updates the <see cref="transferedHubMapResult"/>
        /// </summary>
        /// <param name="mappedElement">The <see cref="MappedElementDefinitionRowViewModel"/></param>
        private void UpdateTransferedHubMapResult(MappedElementDefinitionRowViewModel mappedElement)
        {
            var value = mappedElement.SelectedValue.Value;
            var referenceNodeId = (NodeId)mappedElement.SelectedVariable.Reference.NodeId;

            if (this.transferedHubMapResult.FirstOrDefault(x => x.NodeId.Identifier == referenceNodeId.Identifier) is { } alreadyTransfered)
            {
                this.transferedHubMapResult.Remove(alreadyTransfered);
            }

            this.transferedHubMapResult.Add((referenceNodeId, value));
        }

        /// <summary>
        /// Gets a value indicating if the <paramref name="reference"/> value can be overridden 
        /// </summary>
        /// <param name="reference"></param>
        /// <returns>An assert</returns>
        public bool IsVariableWritable(ReferenceDescription reference)
        {
            var referenceNodeId = (NodeId) reference.NodeId;
            return this.opcClientService.WriteNode(referenceNodeId, this.opcClientService.ReadNode(referenceNodeId).Value, false);
        }

        /// <summary>
        /// Writes the <see cref="double"/> <paramref name="value"/> to the <paramref name="nodeId"/>
        /// </summary>
        /// <param name="nodeId">The <see cref="NodeId"/> on which to write the <paramref name="value"/></param>
        /// <param name="value">The <see cref="double"/> value</param>
        /// <returns>An assert</returns>
        public bool WriteToDst(NodeId nodeId, double value)
        {
            return this.opcClientService.WriteNode(nodeId, value);
        }

        /// <summary>
        /// Reads a node and gets its states information
        /// </summary>
        /// <param name="reference">The <see cref="ReferenceDescription"/> to read</param>
        /// <returns>The <see cref="DataValue"/></returns>
        public DataValue ReadNode(ReferenceDescription reference)
        {
            var referenceNodeId = (NodeId) reference.NodeId;
            return this.opcClientService.ReadNode(referenceNodeId);
        }

        /// <summary>
        /// Reads all values for <see cref="Variables"/> based on <paramref name="time"/>
        /// </summary>
        /// <param name="time">The current time</param>
        public void ReadAllNode(double time)
        {
            foreach (var (reference, _) in this.Variables)
            {
                var value = this.opcClientService.ReadNode((NodeId) reference.NodeId);
                CDPMessageBus.Current.SendMessage(new OpcVariableChangedEvent(reference, value, time));
            }
        }

        /// <summary>
        /// Resets all <see cref="VariableRowViewModel"/> by deleting the collected values
        /// </summary>
        public void ResetVariables()
        {
            foreach (var (reference, _) in this.Variables)
            {
                var value = this.opcClientService.ReadNode((NodeId)reference.NodeId);
                CDPMessageBus.Current.SendMessage(new OpcVariableChangedEvent(reference, value, 0, true));
            }
        }
            
        /// <summary>
        /// Sets the next experiment step in the OPC server and retrives new values for <see cref="Variables"/>
        /// </summary>
        public void GetNextExperimentStep()
        {
            this.CallServerMethod("method_integ_cint");

            var result = Convert.ToDouble(this.opcClientService.ReadNode(this.TimeNodeId).Value);

            var cint = this.ReadNode(this.References
                .FirstOrDefault(x => x.BrowseName.Name == "CINT")).Value;

            var places = Convert.ToDouble(cint).ToString(CultureInfo.InvariantCulture).Split('.')[1];

            var time = Math.Round(result, places.Length, MidpointRounding.AwayFromZero);

            this.ReadAllNode(time);
        }

        /// <summary>
        /// Transfers the mapped variables to the Hub data source
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        public async Task TransferMappedThingsToHub()
        {
            try
            {
                var (iterationClone, transaction) = this.GetIterationTransaction();

                if (!(this.SelectedDstMapResultToTransfer.Any() && this.TrySupplyingAndCreatingLogEntry(transaction)))
                {
                    return;
                }

                foreach (var element in this.SelectedDstMapResultToTransfer)
                {
                    switch (element)
                    {
                        case ElementDefinition elementDefinition:
                        {
                            var elementClone = this.TransactionCreateOrUpdate(transaction, elementDefinition, iterationClone.Element);

                            foreach (var parameter in elementDefinition.Parameter)
                            {
                                this.TransactionCreateOrUpdate(transaction, parameter, elementClone.Parameter);
                            }

                            break;
                        }
                        case ElementUsage elementUsage:
                        {
                            foreach (var parameterOverride in elementUsage.ParameterOverride)
                            {
                                var elementUsageClone = elementUsage.Clone(false);
                                transaction.CreateOrUpdate(elementUsageClone);

                                this.TransactionCreateOrUpdate(transaction, parameterOverride, elementUsageClone.ParameterOverride);
                            }

                            break;
                        }
                    }
                }

                transaction.CreateOrUpdate(iterationClone);

                this.mappingConfigurationService.AddToExternalIdentifierMap(this.ParameterVariable);
                this.mappingConfigurationService.PersistExternalIdentifierMap(transaction, iterationClone);

                await this.hubController.Write(transaction);

                await this.UpdateParametersValueSets();

                await this.hubController.Refresh();

                this.mappingConfigurationService.RefreshExternalIdentifierMap();

                this.DstMapResult.Clear();
                this.SelectedDstMapResultToTransfer.Clear();
                this.ParameterVariable.Clear();

                this.LoadMappingToHub();

                CDPMessageBus.Current.SendMessage(new UpdateObjectBrowserTreeEvent(true));
            }
            catch (Exception e)
            {
                this.logger.Error(e);
                throw;
            }
        }

        /// <summary>
        /// Initializes a new <see cref="IThingTransaction"/> based on the current open <see cref="Iteration"/>
        /// </summary>
        /// <returns>A <see cref="ValueTuple"/> Containing the <see cref="Iteration"/> clone and the <see cref="IThingTransaction"/></returns>
        private (Iteration clone, ThingTransaction transaction) GetIterationTransaction()
        {
            var iterationClone = this.hubController.OpenIteration.Clone(false);
            return (iterationClone, new ThingTransaction(TransactionContextResolver.ResolveContext(iterationClone), iterationClone));
        }

        /// <summary>
        /// Updates the <see cref="IValueSet"/> of all <see cref="Parameter"/> and all <see cref="ParameterOverride"/>
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        public async Task UpdateParametersValueSets()
        {
            var (iterationClone, transaction) = this.GetIterationTransaction();

            this.UpdateParametersValueSets(transaction, this.SelectedDstMapResultToTransfer.OfType<ElementDefinition>().SelectMany(x => x.Parameter));
            this.UpdateParametersValueSets(transaction, this.SelectedDstMapResultToTransfer.OfType<ElementUsage>().SelectMany(x => x.ParameterOverride));

            transaction.CreateOrUpdate(iterationClone);
            
            await this.hubController.Write(transaction);
        }

        /// <summary>
        /// Updates the specified <see cref="Parameter"/> <see cref="IValueSet"/>
        /// </summary>
        /// <param name="transaction">the <see cref="IThingTransaction"/></param>
        /// <param name="parameters">The collection of <see cref="Parameter"/></param>
        private void UpdateParametersValueSets(IThingTransaction transaction, IEnumerable<Parameter> parameters)
        {
            foreach (var parameter in parameters)
            {
                this.hubController.GetThingById(parameter.Iid, this.hubController.OpenIteration, out Parameter newParameter);
                
                var newParameterCloned = newParameter.Clone(false);

                for (var index = 0; index < parameter.ValueSet.Count; index++)
                {
                    var clone = newParameterCloned.ValueSet[index].Clone(false);
                    this.UpdateValueSet(clone, parameter.ValueSet[index]);
                    transaction.CreateOrUpdate(clone);
                }

                transaction.CreateOrUpdate(newParameterCloned);
            }
        }

        /// <summary>
        /// Updates the specified <see cref="ParameterOverride"/> <see cref="IValueSet"/>
        /// </summary>
        /// <param name="transaction">the <see cref="IThingTransaction"/></param>
        /// <param name="parameters">The collection of <see cref="ParameterOverride"/></param>
        private void UpdateParametersValueSets(IThingTransaction transaction, IEnumerable<ParameterOverride> parameters)
        {
            foreach (var parameter in parameters)
            {
                this.hubController.GetThingById(parameter.Iid, this.hubController.OpenIteration, out ParameterOverride newParameter);
                var newParameterClone = newParameter.Clone(true);

                for (var index = 0; index < parameter.ValueSet.Count; index++)
                {
                    var clone = newParameterClone.ValueSet[index];
                    this.UpdateValueSet(clone, parameter.ValueSet[index]);
                    transaction.CreateOrUpdate(clone);
                }

                transaction.CreateOrUpdate(newParameterClone);
            }
        }

        /// <summary>
        /// Sets the value of the <paramref name="valueSet"></paramref> to the <paramref name="clone"/>
        /// </summary>
        /// <param name="clone">The clone to update</param>
        /// <param name="valueSet">The <see cref="IValueSet"/> of reference</param>
        private void UpdateValueSet(ParameterValueSetBase clone, IValueSet valueSet)
        {
            this.exchangeHistory.Append(clone, valueSet);
            
            clone.Computed = valueSet.Computed;
            clone.ValueSwitch = valueSet.ValueSwitch;
        }

        /// <summary>
        /// Registers the provided <paramref cref="Thing"/> to be created or updated by the <paramref name="transaction"/>
        /// </summary>
        /// <typeparam name="TThing">The type of the <paramref name="containerClone"/></typeparam>
        /// <param name="transaction">The <see cref="IThingTransaction"/></param>
        /// <param name="thing">The <see cref="Thing"/></param>
        /// <param name="containerClone">The <see cref="ContainerList{T}"/> of the cloned container</param>
        /// <returns>A cloned <typeparamref name="TThing"/></returns>
        private TThing TransactionCreateOrUpdate<TThing>(IThingTransaction transaction, TThing thing, ContainerList<TThing> containerClone) where TThing : Thing
        {
            var clone = thing.Clone(false);

            if (clone.Iid == Guid.Empty)
            {
                clone.Iid = Guid.NewGuid();
                thing.Iid = clone.Iid;
                transaction.Create(clone);
                containerClone.Add((TThing) clone);
                this.exchangeHistory.Append(clone, ChangeKind.Create);
            }
            else
            {
                transaction.CreateOrUpdate(clone);
                this.exchangeHistory.Append(clone, ChangeKind.Update);
            }

            return (TThing) clone;
        }
        
        /// <summary>
        /// Pops the <see cref="CreateLogEntryDialog"/> and based on its result, either registers a new ModelLogEntry to the <see cref="transaction"/> or not
        /// </summary>
        /// <param name="transaction">The <see cref="IThingTransaction"/> that will get the changes registered to</param>
        /// <returns>A boolean result, true if the user pressed OK, otherwise false</returns>
        private bool TrySupplyingAndCreatingLogEntry(ThingTransaction transaction)
        {
            var vm = new CreateLogEntryDialogViewModel();

            var dialogResult = this.navigation
                .ShowDxDialog<CreateLogEntryDialog, CreateLogEntryDialogViewModel>(vm);

            if (dialogResult != true)
            {
                return false;
            }

            this.hubController.RegisterNewLogEntryToTransaction(vm.LogEntryContent, transaction);
            return true;
        }
    }
}
