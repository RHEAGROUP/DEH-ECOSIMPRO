// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstVariablesControlViewModel.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2020 RHEA System S.A.
// 
//    Author: Sam Gerené, Alex Vorobiev, Alexander van Delft, Nathanael Smiechowski, Arielle Petit.
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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Linq;

    using Autofac;
    using CDP4Common.EngineeringModelData;

    using CDP4Dal;
    using DEHPCommon.HubController.Interfaces;
    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Events;
    using DEHPEcosimPro.ViewModel.Interfaces;
    using DEHPEcosimPro.ViewModel.Rows;

    using NLog;

    using ReactiveUI;


    /// <summary>
    /// The <see cref="DifferenceViewModel"/> is the view model for displaying diference betwen the values of the selection
    /// </summary>
    public class DifferenceViewModel : ReactiveObject, IDifferenceViewModel
    {

        /// <summary>
        /// The <see cref="NLog"/> logger
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// List of parameter to show on the window
        /// </summary>
        public ReactiveList<ParameterDifferenceRowViewModel> Parameters { get; set; } = new ReactiveList<ParameterDifferenceRowViewModel>();

        /// <summary>
        /// Initializes a new instance of the <see cref="T:DEHPEcosimPro.ViewModel.DifferenceViewModel" /> class.
        /// </summary>
        /// <param name="hubController"><see cref="IHubController"/></param>
        /// <param name="dstController"><see cref="IDstController"/></param>
        public DifferenceViewModel(IHubController hubController, IDstController dstController)
        {
            this.hubController = hubController;
            this.dstController = dstController;

            CDPMessageBus.Current.Listen<DifferenceEvent<ParameterOrOverrideBase>>()
                .Subscribe(this.HandleDifferentEvent);


            CDPMessageBus.Current.Listen<DifferenceEvent<ElementDefinition>>()
                .Subscribe(this.HandleListOfDifferentEvent);

        }

        /// <summary>
        /// Handle the parameter
        /// </summary>
        /// <param name="parameterEvent"><see cref="DifferenceEvent<ParameterOrOverrideBase>"/></param>
        private void HandleDifferentEvent(DifferenceEvent<ParameterOrOverrideBase> parameterEvent)
        {
            this.CreateNewParameter((Parameter)parameterEvent.Thing, parameterEvent.HasTheSelectionChanged);
        }

        /// <summary>
        /// Handle the Parameters list
        /// </summary>
        /// <param name="elementDefinition"><see cref="DifferenceEvent<ElementDefinition>"/></param>
        private void HandleListOfDifferentEvent(DifferenceEvent<ElementDefinition> elementDefinition)
        {
            var listOfParameters = elementDefinition.Thing.Parameter;

            foreach (var thing in listOfParameters)
            {
                this.CreateNewParameter(thing, elementDefinition.HasTheSelectionChanged);
            }
        }

        /// <summary>
        ///  Add or Remove the thing to Parameters list
        /// </summary>
        /// <param name="newParameter"><see cref="Parameter"/></param>
        /// <param name="HasTheselectionChanged">From the parameter boolean HasTheSelectionChanged</param>
        private void CreateNewParameter(Parameter newParameter, bool HasTheselectionChanged)
        {

            this.hubController.GetThingById(newParameter.Iid, this.hubController.OpenIteration, out Parameter oldThing);
            
            if (HasTheselectionChanged)
            {
                var IsParameterAlreadyExisting = this.Parameters.Any(x => x.NewThing.Iid == newParameter.Iid);

                if (!IsParameterAlreadyExisting)
                {

                    this.Parameters.AddRange(new ParameterDifferenceViewModel(oldThing, (Parameter)newParameter, this.dstController).ListOfParameters);
                }
            }
            else
            {
                var toRemove = (List<ParameterDifferenceRowViewModel>) this.Parameters
                    .Where(x => newParameter.Iid == x.NewThing.Iid
                                && newParameter.ParameterType.ShortName == x.NewThing.ParameterType.ShortName).ToList();
                toRemove.ForEach(x => this.Parameters.Remove(x));
            }
        }

    }
}
