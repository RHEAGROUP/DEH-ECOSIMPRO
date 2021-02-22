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
    using System.Linq;
    using System.Threading.Tasks;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.Types;

    using CDP4Dal;
    using CDP4Dal.Operations;

    using DEHPCommon.Enumerators;
    using DEHPCommon.Events;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.Views;

    using DEHPEcosimPro.Enumerator;
    using DEHPEcosimPro.Events;
    using DEHPEcosimPro.Services.OpcConnector;
    using DEHPEcosimPro.Services.OpcConnector.Interfaces;
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
        /// Backing field for the <see cref="MappingDirection"/>
        /// </summary>
        private MappingDirection mappingDirection;

        /// <summary>
        /// Gets this running tool name
        /// </summary>
        public string ThisToolName => this.GetType().Assembly.GetName().Name;

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
        /// Gets a <see cref="Dictionary{TKey, TValue}"/> of all mapped parameter and the associate <see cref="NodeId.Identifier"/>
        /// </summary>
        public Dictionary<ParameterOrOverrideBase, object> ParameterNodeIds { get; } = new Dictionary<ParameterOrOverrideBase, object>();

        /// <summary>
        /// Gets the colection of mapped <see cref="ReferenceDescription"/>
        /// </summary>
        public ReactiveList<MappedElementDefinitionRowViewModel> HubMapResult { get; private set; } = new ReactiveList<MappedElementDefinitionRowViewModel>();

        /// <summary>
        /// Gets or sets the <see cref="ExternalIdentifierMap"/>
        /// </summary>
        public ExternalIdentifierMap ExternalIdentifierMap { get; set; }

        /// <summary>
        /// Gets the collection of <see cref="IdCorrespondences"/>
        /// </summary>
        public List<IdCorrespondence> IdCorrespondences { get; } = new List<IdCorrespondence>();

        /// <summary>
        /// Initializes a new <see cref="DstController"/>
        /// </summary>
        /// <param name="opcClientService">The <see cref="IOpcClientService"/></param>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="sessionHandler">The <see cref="IOpcSessionHandler"/></param>
        /// <param name="mappingEngine">The <see cref="IMappingEngine"/></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel"/></param>
        /// <param name="navigationService">The <see cref="INavigationService"/></param>
        public DstController(IOpcClientService opcClientService, IHubController hubController,
            IOpcSessionHandler sessionHandler, IMappingEngine mappingEngine,
            IStatusBarControlViewModel statusBar, INavigationService navigationService)
        {
            this.opcClientService = opcClientService;
            this.hubController = hubController;
            this.sessionHandler = sessionHandler;
            this.mappingEngine = mappingEngine;
            this.statusBar = statusBar;
            this.navigation = navigationService;

            this.WhenAnyValue(x => x.opcClientService.OpcClientStatusCode).Subscribe(clientStatusCode =>
            {
                var isOpcSessionOpen = clientStatusCode == OpcClientStatusCode.Connected;

                if (isOpcSessionOpen)
                {
                    foreach (var reference in this.opcClientService.References)
                    {
                        if (reference.NodeClass == NodeClass.Variable && reference.NodeId.NamespaceIndex == 4)
                        {
                            this.Variables.Add((reference, this.opcClientService.ReadNode((NodeId)reference.NodeId)));
                        }

                        else if (reference.NodeClass == NodeClass.Method)
                        {
                            this.Methods.Add(reference);
                        }
                    }
                }
                else
                {
                    this.Variables.Clear();
                    this.Methods.Clear();
                }

                this.IsSessionOpen = isOpcSessionOpen;
            });
        }

        /// <summary>
        /// Connects to the provided endpoint
        /// </summary>
        /// <param name="endpoint">The end point url eg. often opc.tcp:// representing the opc protocol</param>
        /// <param name="autoAcceptConnection">An assert whether the certificate should be auto accepted if valid</param>
        /// <param name="credential">The <see cref="IUserIdentity"/> default = null in case server does not require authentication</param>
        /// <returns>A <see cref="Task"/></returns>
        public async Task Connect(string endpoint, bool autoAcceptConnection = true, IUserIdentity credential = null)
        {
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
            var serverMethodsNode = this.References.SingleOrDefault(r => r.BrowseName.Name == "server_methods")?.NodeId;
            var methodNode = this.Methods.SingleOrDefault(m => m.BrowseName.Name == methodBrowseName)?.NodeId;

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
            this.opcClientService.CloseSession();
            this.IsSessionOpen = false;
        }

        /// <summary>
        /// Map the provided collection using the corresponding rule in the assembly and the <see cref="MappingEngine"/>
        /// </summary>
        /// <param name="dstVariables">The <see cref="List{T}"/> of <see cref="VariableRowViewModel"/> data</param>
        /// <returns>A <see cref="Task"/></returns>
        public void Map(List<VariableRowViewModel> dstVariables)
        {
            if (this.mappingEngine.Map(dstVariables) is (Dictionary<ParameterOrOverrideBase, object> parameterNodeIds, List<ElementBase> elements) && elements.Any())
            {
                this.DstMapResult.AddRange(elements);
                this.UpdateParmeterNodeId(parameterNodeIds);
            }

            this.UpdateExternalIdentifierMap();
            CDPMessageBus.Current.SendMessage(new UpdateObjectBrowserTreeEvent());
        }
        
        /// <summary>
        /// Updates <see cref="ParameterNodeIds"/> by adding or replacing values
        /// </summary>
        /// <param name="parameterNodeIds">The  </param>
        private void UpdateParmeterNodeId(Dictionary<ParameterOrOverrideBase, object> parameterNodeIds)
        {
            foreach (var keyValue in parameterNodeIds)
            {
                this.ParameterNodeIds[keyValue.Key] = keyValue.Value;
            }
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

            this.UpdateExternalIdentifierMap();
            CDPMessageBus.Current.SendMessage(new UpdateDstVariableTreeEvent());
        }

        /// <summary>
        /// Transfers the mapped variables to the Dst data source
        /// </summary>
        public void TransferMappedThingsToDst()
        {
            foreach (var mappedElement in this.HubMapResult
                .Where(
                    mappedElement => this.opcClientService.WriteNode(
                        (NodeId) mappedElement.SelectedVariable.Reference.NodeId,
                        double.Parse(mappedElement.SelectedValue.Value)))
                .ToList())
            {
                this.HubMapResult.Remove(mappedElement);

                this.IdCorrespondences.Add(new IdCorrespondence(Guid.NewGuid(), null, null)
                {
                    ExternalId = mappedElement.SelectedVariable.Name,
                    InternalThing = ((Thing) mappedElement.SelectedValue.Container).Iid
                });
            }

            CDPMessageBus.Current.SendMessage(new UpdateDstVariableTreeEvent(true));
        }

        /// <summary>
        /// Gets a value indicating if the <paramref name="reference"/> value can be overridden 
        /// </summary>
        /// <param name="reference"></param>
        /// <returns>An assert</returns>
        public bool IsVariableWritable(ReferenceDescription reference)
        {
            var referenceNodeId = (NodeId) reference.NodeId;
            return this.opcClientService.WriteNode(referenceNodeId, this.opcClientService.ReadNode(referenceNodeId).Value);
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
        /// Transfers the mapped variables to the Hub data source
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        public async Task TransferMappedThingsToHub()
        {
            try
            {
                var (iterationClone, transaction) = this.GetIterationTransaction();

                if (!(this.DstMapResult.Any() && this.TrySupplyingAndCreatingLogEntry(transaction)))
                {
                    return;
                }

                foreach (var element in this.DstMapResult)
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

                this.PersistExternalIdentifierMap(transaction, iterationClone);

                transaction.CreateOrUpdate(iterationClone);

                await this.hubController.Write(transaction);

                await this.UpdateParametersValueSets();

                await this.hubController.Refresh();
                this.DstMapResult.Clear();
                this.ParameterNodeIds.Clear();

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

            this.UpdateParametersValueSets(transaction, this.DstMapResult.OfType<ElementDefinition>().SelectMany(x => x.Parameter));
            this.UpdateParametersValueSets(transaction, this.DstMapResult.OfType<ElementUsage>().SelectMany(x => x.ParameterOverride));

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
                    UpdateValueSet(clone, parameter.ValueSet[index]);
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
                    UpdateValueSet(clone, parameter.ValueSet[index]);
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
        private static void UpdateValueSet(ParameterValueSetBase clone, IValueSet valueSet)
        {
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
                this.AddIdCorrespondence(clone);
            }
            else
            {
                transaction.CreateOrUpdate(clone);
            }

            return (TThing) clone;
        }

        /// <summary>
        /// If the <see cref="Thing"/> is new save the mapping
        /// </summary>
        /// <param name="clone">The <see cref="Thing"/></param>
        private void AddIdCorrespondence(Thing clone)
        {
            string externalId;

            switch (clone)
            {
                case INamedThing namedThing:
                    externalId = namedThing.Name;
                    break;
                case ParameterOrOverrideBase parameterOrOverride:
                    externalId = parameterOrOverride.ParameterType.Name;
                    break;
                default:
                    return;
            }

            this.AddToExternalIdentifierMap(clone.Iid, externalId);
        }

        /// <summary>
        /// Updates the configured mapping 
        /// </summary>
        public void UpdateExternalIdentifierMap()
        {
            var unsusedIdCorrespondences = this.ExternalIdentifierMap.Correspondence
                .Where(x => this.IdCorrespondences
                    .All(c => c.Iid != x.Iid || c.ExternalId != x.ExternalId))
                .ToList();

            this.ExternalIdentifierMap.Correspondence.Clear();
            this.ExternalIdentifierMap.Correspondence.AddRange(this.IdCorrespondences);
            this.ExternalIdentifierMap.Correspondence.AddRange(unsusedIdCorrespondences);
            this.IdCorrespondences.Clear();
        }

        /// <summary>
        /// Updates the configured mapping, registering the <see cref="ExternalIdentifierMap"/> and its <see cref="IdCorrespondence"/>
        /// to a <see name="IThingTransaction"/>
        /// </summary>
        /// <param name="transaction">The <see cref="IThingTransaction"/></param>
        /// <param name="iterationClone">The <see cref="Iteration"/> clone</param>
        private void PersistExternalIdentifierMap(IThingTransaction transaction, Iteration iterationClone)
        {
            this.UpdateExternalIdentifierMap();
            
            var externalIdentifierMapClone = this.ExternalIdentifierMap.Clone(false);

            if (externalIdentifierMapClone.Iid == Guid.Empty)
            {
                externalIdentifierMapClone.Iid = Guid.NewGuid();
                this.ExternalIdentifierMap.Iid = externalIdentifierMapClone.Iid;
                iterationClone.ExternalIdentifierMap.Add(externalIdentifierMapClone);
            }

            var idCorrespondencesToPersist = this.ExternalIdentifierMap.Correspondence.ToList();
            
            foreach (var correspondence in idCorrespondencesToPersist)
            {
                var correspondenceClone = correspondence.Clone(false);

                if (correspondence.Iid == Guid.Empty)
                {
                    correspondence.Iid = Guid.NewGuid();
                    correspondenceClone.Iid = correspondence.Iid;
                    externalIdentifierMapClone.Correspondence.Add(correspondenceClone);
                    transaction.Create(correspondenceClone);
                }
                else
                {
                    correspondenceClone.Container = externalIdentifierMapClone;
                    transaction.CreateOrUpdate(correspondenceClone);
                }
            }

            transaction.CreateOrUpdate(externalIdentifierMapClone);

            this.statusBar.Append("Mapping configuration processed");
        }

        /// <summary>
        /// Creates and sets the <see cref="ExternalIdentifierMap"/>
        /// </summary>
        /// <param name="newName">The model name to use for creating the new <see cref="ExternalIdentifierMap"/></param>
        /// <returns>A newly created <see cref="ExternalIdentifierMap"/></returns>
        public ExternalIdentifierMap CreateExternalIdentifierMap(string newName)
        {
            return new ExternalIdentifierMap()
            {
                Name = newName,
                ExternalToolName = this.ThisToolName,
                ExternalModelName = newName,
                Owner = this.hubController.CurrentDomainOfExpertise,
                Container = this.hubController.OpenIteration
            };
        }

        /// <summary>
        /// Adds one correspondance to the <see cref="IDstController.IdCorrespondences"/>
        /// </summary>
        /// <param name="internalId">The thing that <see cref="externalId"/> corresponds to</param>
        /// <param name="externalId">The external thing that <see cref="internalId"/> corresponds to</param>
        public void AddToExternalIdentifierMap(Guid internalId, string externalId)
        {
            if (internalId != Guid.Empty && !this.ExternalIdentifierMap.Correspondence
                    .Any(x => x.ExternalId == externalId && x.InternalThing == internalId)
                && !this.IdCorrespondences.Any(x => x.ExternalId == externalId && x.InternalThing == internalId))
            {
                this.IdCorrespondences.Add(new IdCorrespondence()
                {
                    ExternalId = externalId,
                    InternalThing = internalId
                });
            }
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
