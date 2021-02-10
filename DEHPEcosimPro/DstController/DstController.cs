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

    using Opc.Ua;

    using ReactiveUI;

    /// <summary>
    /// The <see cref="DstController"/> takes care of retrieving data from and to EcosimPro
    /// </summary>
    public class DstController : ReactiveObject, IDstController
    {
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
        /// Gets the colection of mapped <see cref="ElementDefinition"/>s and <see cref="Parameter"/>s
        /// </summary>
        public ReactiveList<ElementDefinition> DstMapResult { get; private set; } = new ReactiveList<ElementDefinition>();

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
        public DstController(IOpcClientService opcClientService, IHubController hubController,
            IOpcSessionHandler sessionHandler, IMappingEngine mappingEngine, IStatusBarControlViewModel statusBar)
        {
            this.opcClientService = opcClientService;
            this.hubController = hubController;
            this.sessionHandler = sessionHandler;
            this.mappingEngine = mappingEngine;
            this.statusBar = statusBar;

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
        public async Task Map(List<VariableRowViewModel> dstVariables)
        {
            if (this.mappingEngine.Map(dstVariables) is List<ElementDefinition> mapResult && mapResult.Any())
            {
                this.DstMapResult.AddRange(mapResult);
            }

            await this.UpdateExternalIdentifierMap();
            CDPMessageBus.Current.SendMessage(new UpdateObjectBrowserTreeEvent());
        }

        /// <summary>
        /// Map the provided collection using the corresponding rule in the assembly and the <see cref="MappingEngine"/>
        /// </summary>
        /// <param name="mappedElement">The <see cref="List{T}"/> of <see cref="MappedElementDefinitionRowViewModel"/></param>
        /// <returns>A <see cref="Task"/></returns>
        public async Task Map(List<MappedElementDefinitionRowViewModel> mappedElement)
        {
            if (mappedElement.Any())
            {
                this.HubMapResult.AddRange(mappedElement);
            }

            await this.UpdateExternalIdentifierMap();
            CDPMessageBus.Current.SendMessage(new UpdateDstVariableTreeEvent());
        }

        /// <summary>
        /// Transfers the mapped variables to the Dst data source
        /// </summary>
        public void TransferMappedThingsToDst()
        {
            foreach (var mappedElement in this.HubMapResult.ToList()
                .Where(
                    mappedElement => this.opcClientService.WriteNode(
                        (NodeId)mappedElement.SelectedVariable.Reference.NodeId,
                        double.Parse(mappedElement.SelectedValue.Value))))
            {
                this.HubMapResult.Remove(mappedElement);
                this.IdCorrespondences.Add(new IdCorrespondence(Guid.NewGuid(), null, null)
                {
                    ExternalId = mappedElement.SelectedVariable.Name,
                    InternalThing = ((Thing)mappedElement.SelectedValue.Container).Iid
                });
            }

            CDPMessageBus.Current.SendMessage(new UpdateDstVariableTreeEvent() { Reset = true });
        }

        /// <summary>
        /// Gets a value indicating if the <paramref name="reference"/> value can be overridden 
        /// </summary>
        /// <param name="reference"></param>
        /// <returns>An assert</returns>
        public bool IsVariableWritable(ReferenceDescription reference)
        {
            var referenceNodeId = (NodeId)reference.NodeId;
            return this.opcClientService.WriteNode(referenceNodeId, this.opcClientService.ReadNode(referenceNodeId).Value);
        }

        /// <summary>
        /// Reads a node and gets its states information
        /// </summary>
        /// <param name="reference">The <see cref="ReferenceDescription"/> to read</param>
        /// <returns>The <see cref="DataValue"/></returns>
        public DataValue ReadNode(ReferenceDescription reference)
        {
            var referenceNodeId = (NodeId)reference.NodeId;
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
                var iterationClone = this.hubController.OpenIteration.Clone(false);
                var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(iterationClone), iterationClone);

                if (!this.TrySupplyingAndCreatingLogEntry(transaction))
                {
                    return;
                }

                foreach (var elementDefinition in this.DstMapResult)
                {
                    var elementDefinitionCloned = this.TransactionCreateOrUpdate(transaction, elementDefinition, iterationClone.Element);

                    foreach (var parameter in elementDefinition.Parameter)
                    {
                        _ = this.TransactionCreateOrUpdate(transaction, parameter, elementDefinitionCloned.Parameter);
                    }

                    foreach (var parameterOverride in elementDefinition.ContainedElement.SelectMany(x => x.ParameterOverride))
                    {
                        var elementUsageClone = (ElementUsage)parameterOverride.Container.Clone(false);
                        transaction.CreateOrUpdate(elementUsageClone);

                        _ = this.TransactionCreateOrUpdate(transaction, parameterOverride, elementUsageClone.ParameterOverride);
                    }
                }

                transaction.CreateOrUpdate(iterationClone);

                await this.hubController.Write(transaction);

                await this.UpdateParametersValueSets();

                await this.UpdateExternalIdentifierMap();

                await this.hubController.Refresh();
                CDPMessageBus.Current.SendMessage(new UpdateObjectBrowserTreeEvent(true));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Updates the <see cref="IValueSet"/> of all <see cref="Parameter"/> and all <see cref="ParameterOverride"/>
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        private async Task UpdateParametersValueSets()
        {
            await this.UpdateParametersValueSets(this.DstMapResult.SelectMany(x => x.Parameter));
            await this.UpdateParametersValueSets(this.DstMapResult.SelectMany(x => x.ContainedElement.SelectMany(p => p.ParameterOverride)));
        }

        /// <summary>
        /// Updates the specified <see cref="Parameter"/> <see cref="IValueSet"/>
        /// </summary>
        /// <param name="parameters">The collection of <see cref="Parameter"/></param>
        private async Task UpdateParametersValueSets(IEnumerable<Parameter> parameters)
        {
            foreach (var parameter in parameters)
            {
                this.hubController.GetThingById(parameter.Iid, this.hubController.OpenIteration, out Parameter newParameter);
                var container = newParameter.Clone(false);

                var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(container), container);

                for (var index = 0; index < parameter.ValueSet.Count; index++)
                {
                    var clone = newParameter.ValueSet[index].Clone(false);
                    UpdateValueSet(clone, parameter.ValueSet[index]);
                    transaction.CreateOrUpdate(clone);
                }

                transaction.CreateOrUpdate(container);

                await this.hubController.Write(transaction);
            }
        }

        /// <summary>
        /// Updates the specified <see cref="ParameterOverride"/> <see cref="IValueSet"/>
        /// </summary>
        /// <param name="parameters">The collection of <see cref="ParameterOverride"/></param>
        private async Task UpdateParametersValueSets(IEnumerable<ParameterOverride> parameters)
        {
            foreach (var parameter in parameters)
            {
                this.hubController.GetThingById(parameter.Iid, this.hubController.OpenIteration, out ParameterOverride newParameter);
                var container = newParameter.Clone(false);

                var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(container), container);

                for (var index = 0; index < parameter.ValueSet.Count; index++)
                {
                    var clone = newParameter.ValueSet[index].Clone(false);
                    UpdateValueSet(clone, parameter.ValueSet[index]);
                    transaction.CreateOrUpdate(clone);
                }

                transaction.CreateOrUpdate(container);

                await this.hubController.Write(transaction);
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
                containerClone.Add((TThing)clone);
                this.AddIdCorrespondence(clone);
            }
            else
            {
                transaction.CreateOrUpdate(clone);
            }

            return (TThing)clone;
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

            this.IdCorrespondences.Add(new IdCorrespondence(Guid.NewGuid(), null, null)
            {
                ExternalId = externalId,
                InternalThing = clone.Iid
            });
        }

        /// <summary>
        /// Updates the configured mapping
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        public async Task UpdateExternalIdentifierMap()
        {
            var idCorrespondencesToAdd = this.ExternalIdentifierMap.Correspondence
                .Where(x => this.IdCorrespondences.All(c => c.Iid != x.Iid || (c.ExternalId != x.ExternalId))).ToList();

            this.IdCorrespondences.AddRange(idCorrespondencesToAdd);

            if (this.ExternalIdentifierMap.Correspondence.Any())
            {
                await this.hubController.Delete<ExternalIdentifierMap, IdCorrespondence>(
                    this.ExternalIdentifierMap.Correspondence.ToList(),
                    (e, c) => e.Correspondence.Remove(c));
            }

            var container = this.ExternalIdentifierMap.Clone(false);
            var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(container), container);
            container.Correspondence.Clear();
            container.Correspondence.AddRange(this.IdCorrespondences);

            foreach (var correspondence in this.IdCorrespondences)
            {
                correspondence.Container = this.ExternalIdentifierMap;
                var clonedCorrespondence = correspondence.Clone(false);
                clonedCorrespondence.Container = container;
                transaction.CreateOrUpdate(clonedCorrespondence);
            }

            transaction.CreateOrUpdate(container);
            await this.hubController.Write(transaction);

            this.ExternalIdentifierMap.Correspondence.Clear();
            this.ExternalIdentifierMap.Correspondence.AddRange(this.IdCorrespondences);
            this.IdCorrespondences.Clear();
            this.statusBar.Append("Mapping configuration saved");
        }

        /// <summary>
        /// Creates and sets the <see cref="ExternalIdentifierMap"/>
        /// </summary>
        /// <param name="newName">The model name to use for creating the new <see cref="ExternalIdentifierMap"/></param>
        /// <returns>A newly created <see cref="ExternalIdentifierMap"/></returns>
        public async Task<ExternalIdentifierMap> CreateExternalIdentifierMap(string newName)
        {
            var externalIdentifierMap = new ExternalIdentifierMap(Guid.NewGuid(), null, null)
            {
                Name = newName,
                ExternalToolName = this.ThisToolName,
                ExternalModelName = newName,
                Owner = this.hubController.CurrentDomainOfExpertise,
                Container = this.hubController.OpenIteration
            };

            await this.hubController.CreateOrUpdate<Iteration, ExternalIdentifierMap>(externalIdentifierMap,
                (i, m) => i.ExternalIdentifierMap.Add(m), true);

            return externalIdentifierMap;
        }

        /// <summary>
        /// Adds one correspondance to the <see cref="IDstController.IdCorrespondences"/>
        /// </summary>
        /// <param name="internalId">The thing that <see cref="externalId"/> corresponds to</param>
        /// <param name="externalId">The external thing that <see cref="internalId"/> corresponds to</param>
        public void AddToExternalIdentifierMap(Guid internalId, string externalId)
        {
            if (internalId != Guid.Empty && !this.ExternalIdentifierMap.Correspondence.Any(
                x => x.ExternalId == externalId && x.InternalThing == internalId))
            {
                this.IdCorrespondences.Add(new IdCorrespondence(Guid.NewGuid(), null, null)
                {
                    ExternalId = externalId,
                    InternalThing = internalId
                });
            }
        }

        /// <summary>
        /// Pops the <see cref="CreateLogEntryDialog"/> and based on its result, either registers a new ModelLogEntry to the <see cref="transaction"/> or not
        /// </summary>
        /// <param name="transaction">The <see cref="ThingTransaction"/> that will get the changes registered to</param>
        /// <returns>A boolean result, true if the user pressed OK, otherwise false</returns>
        private bool TrySupplyingAndCreatingLogEntry(ThingTransaction transaction)
        {
            var vm = new CreateLogEntryDialogViewModel();
            var dialog = new CreateLogEntryDialog
            {
                DataContext = vm
            };

            if (dialog.ShowDialog() != true)
            {
                return false;
            }

            this.hubController.RegisterNewLogEntryToTransaction(vm.LogEntryContent, transaction);
            return true;
        }
    }
}
