// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HubDataSourceViewModel.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.ViewModel
{
    using System;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using System.Windows.Threading;

    using Autofac;

    using CDP4Dal;

    using DEHPCommon;
    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.Services.ObjectBrowserTreeSelectorService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserInterfaces.ViewModels.PublicationBrowser;
    using DEHPCommon.UserInterfaces.ViewModels.Rows.ElementDefinitionTreeRows;
    using DEHPCommon.UserInterfaces.Views;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Events;
    using DEHPEcosimPro.ViewModel.Dialogs.Interfaces;
    using DEHPEcosimPro.ViewModel.Interfaces;
    using DEHPEcosimPro.Views.Dialogs;

    using ReactiveUI;

    /// <summary>
    /// View model that represents a data source panel which holds a tree like browser, a informational header and
    /// some control regarding the connection to the data source
    /// </summary>
    public sealed class HubDataSourceViewModel : DataSourceViewModel, IHubDataSourceViewModel
    {
        /// <summary>
        /// The <see cref="IHubController"/>
        /// </summary>
        private readonly IHubController hubController;

        /// <summary>
        /// The <see cref="IObjectBrowserTreeSelectorService"/>
        /// </summary>
        private readonly IObjectBrowserTreeSelectorService treeSelectorService;

        /// <summary>
        /// The <see cref="IDstController"/>
        /// </summary>
        private readonly IDstController dstController;
        
        /// <summary>
        /// Gets the <see cref="IHubSessionControlViewModel"/>
        /// </summary>
        public IHubSessionControlViewModel SessionControl { get; }

        /// <summary>
        /// Initializes a new <see cref="HubDataSourceViewModel"/>
        /// </summary>
        /// <param name="navigationService">The <see cref="INavigationService"/></param>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        /// <param name="objectBrowser">The <see cref="IObjectBrowserViewModel"/></param>
        /// <param name="publicationBrowser">The <see cref="IPublicationBrowserViewModel"/></param>
        /// <param name="treeSelectorService">The <see cref="IObjectBrowserTreeSelectorService"/></param>
        /// <param name="hubBrowserHeader">The <see cref="IHubBrowserHeaderViewModel"/></param>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel"/></param>
        /// <param name="sessionControl">The <see cref="IHubSessionControlViewModel"/></param>
        public HubDataSourceViewModel(INavigationService navigationService, IHubController hubController, IObjectBrowserViewModel objectBrowser, 
            IPublicationBrowserViewModel publicationBrowser, IObjectBrowserTreeSelectorService treeSelectorService, 
            IHubBrowserHeaderViewModel hubBrowserHeader, IDstController dstController,
            IHubSessionControlViewModel sessionControl) : base(navigationService)
        {
            this.hubController = hubController;
            this.treeSelectorService = treeSelectorService;
            this.dstController = dstController;
            this.SessionControl = sessionControl;
            this.ObjectBrowser = objectBrowser;
            this.PublicationBrowser = publicationBrowser;
            this.HubBrowserHeader = hubBrowserHeader;

            this.InitializeCommands();
        }

        /// <summary>
        /// Initializes this view model <see cref="ICommand"/>
        /// </summary>
        protected override void InitializeCommands()
        {
            base.InitializeCommands();

            var canMap = this.ObjectBrowser.CanMap.Merge(this.WhenAny(x => x.dstController.MappingDirection,
                x => x.dstController.IsSessionOpen,
                (m, s) =>
                    m.Value is MappingDirection.FromHubToDst && s.Value));

            this.ObjectBrowser.MapCommand = ReactiveCommand.Create(canMap);
            this.ObjectBrowser.MapCommand.Subscribe(_ => this.MapCommandExecute());
            
            this.ObjectBrowser.SelectedThings.CountChanged.Subscribe(_ => this.UpdateNetChangePreviewBasedOnSelection());

            this.WhenAny(x => x.hubController.OpenIteration,
                x => x.hubController.IsSessionOpen,
                (i, o) =>
                    i.Value != null && o.Value)
                .Subscribe(this.UpdateConnectButtonText);
        }

        /// <summary>
        /// Sends an update event to the Dst net change preview based on the current <see cref="IObjectBrowserViewModel.SelectedThings"/>
        /// </summary>
        private void UpdateNetChangePreviewBasedOnSelection()
        {
            CDPMessageBus.Current.SendMessage(new UpdateDstPreviewBasedOnSelectionEvent(
                this.ObjectBrowser.SelectedThings.OfType<ElementDefinitionRowViewModel>(), null, false));
        }

        /// <summary>
        /// Executes the <see cref="IObjectBrowserViewModel.MapCommand"/>
        /// </summary>
        public void MapCommandExecute()
        {
            var viewModel = AppContainer.Container.Resolve<IHubMappingConfigurationDialogViewModel>();
            
            viewModel.Elements.AddRange(this.ObjectBrowser
                .SelectedThings
                .OfType<ElementDefinitionRowViewModel>()
                .Select(x =>
                {
                    x.Thing.Clone(true);
                    return x;
                }));

            this.NavigationService.ShowDialog<HubMappingConfigurationDialog, IHubMappingConfigurationDialogViewModel>(viewModel);
            this.ObjectBrowser.SelectedThings.Clear();
        }

        /// <summary>
        /// Executes the <see cref="DataSourceViewModel.ConnectCommand"/>
        /// </summary>
        protected override void ConnectCommandExecute()
        {
            if (this.hubController.IsSessionOpen)
            {
                if ((this.dstController.DstMapResult.Any() && this.NavigationService.ShowDxDialog<HubLogoutConfirmDialog>() is true)
                    || !this.dstController.DstMapResult.Any())
                {
                    this.dstController.HubMapResult.Clear();
                    this.dstController.DstMapResult.Clear();
                    this.ObjectBrowser.Things.Clear();
                    this.hubController.Close();
                }
            }
            else
            {
                this.NavigationService.ShowDialog<Login>();
            }
        }
    }
}
