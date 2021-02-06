// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstNetChangePreviewViewModel.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.ViewModel.NetChangePreview
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;

    using CDP4Dal;

    using DEHPCommon.Events;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Events;
    using DEHPEcosimPro.ViewModel.Interfaces;

    using ReactiveUI;

    /// <summary>
    /// View model for this dst net change preview panel
    /// </summary>
    public class DstNetChangePreviewViewModel : DstVariablesControlViewModel, IDstNetChangePreviewViewModel
    {
        /// <summary>
        /// Initializes a new <see cref="DstNetChangePreviewViewModel"/>
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="navigationService">The <see cref="INavigationService"/></param>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel"/></param>
        public DstNetChangePreviewViewModel(IDstController dstController, INavigationService navigationService,
            IHubController hubController, IStatusBarControlViewModel statusBar) : base(dstController, navigationService, hubController, statusBar)
        {
            CDPMessageBus.Current.Listen<UpdateDstVariableTreeEvent>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => this.UpdateTree(x.Reset));
        }

        /// <summary>
        /// Updates the tree
        /// </summary>
        /// <param name="shouldReset">A value indicating whether the tree should remove the element in preview</param>
        public void UpdateTree(bool shouldReset)
        {
            if (shouldReset)
            {
                this.Reload();
            }
            else
            {
                this.IsBusy = true;
                this.ComputeValues();
                this.IsBusy = false;
            }
        }

        /// <summary>
        /// Resets the variable tree
        /// </summary>
        private void Reload()
        {
            foreach (var variable in this.Variables)
            {
                variable.ShouldListenToChangeMessage = true;
                CDPMessageBus.Current.SendMessage(new DstHighlightEvent(variable.Reference.NodeId.Identifier, false));
            }
        }

        /// <summary>
        /// Computes the old values for each <see cref="P:DEHPCommon.UserInterfaces.ViewModels.ObjectBrowserViewModel.Things" />
        /// </summary>
        public void ComputeValues()
        {
            foreach (var mappedElement in this.DstController.HubMapResult)
            {
                var variableChanged = this.Variables.FirstOrDefault(
                    x => x.Reference.NodeId.Identifier == mappedElement.SelectedVariable.Reference.NodeId.Identifier);
                
                if (variableChanged is null)
                {
                    continue;
                }

                CDPMessageBus.Current.SendMessage(new DstHighlightEvent(variableChanged.Reference.NodeId.Identifier));
                variableChanged.ActualValue = mappedElement.SelectedValue.Value;
            }
        }
    }
}
