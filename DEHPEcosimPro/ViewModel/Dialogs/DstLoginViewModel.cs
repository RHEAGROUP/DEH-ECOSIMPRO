// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstLoginViewModel.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2020 RHEA System S.A.
// 
//    Author: Sam Gerené, Alex Vorobiev, Alexander van Delft, Nathanael Smiechowski, Ahmed Abulwafa Ahmed.
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

namespace DEHPEcosimPro.ViewModel.Dialogs
{
    using System;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using CDP4Common.EngineeringModelData;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.UserInterfaces.Behaviors;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserPreferenceHandler.UserPreferenceService;

    using DEHPEcosimPro.Settings;
    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.ViewModel.Dialogs.Interfaces;

    using Opc.Ua;

    using ReactiveUI;

    /// <summary>
    /// The view-model for the Login that allows users to connect to a OPC UA datasource
    /// </summary>
    public class DstLoginViewModel : ReactiveObject, IDstLoginViewModel, ICloseWindowViewModel
    {
        /// <summary>
        /// The <see cref="IDstController"/> instance
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel"/> instance
        /// </summary>
        private readonly IStatusBarControlViewModel statusBarControlView;

        /// <summary>
        /// The <see cref="IUserPreferenceService{AppSettings}"/> instance
        /// </summary>
        private readonly IUserPreferenceService<AppSettings> userPreferenceService;
        
        /// <summary>
        /// Backing field for <see cref="IsBusy"/>
        /// </summary>
        private bool isBusy;

        /// <summary>
        /// Gets or sets the assert indicating whether the view is busy
        /// </summary>
        public bool IsBusy
        {
            get => this.isBusy;
            set => this.RaiseAndSetIfChanged(ref this.isBusy, value);
        }

        /// <summary>
        /// Backing field for the <see cref="UserName"/> property
        /// </summary>
        private string username;

        /// <summary>
        /// Gets or sets server username value
        /// </summary>
        public string UserName
        {
            get => this.username;
            set => this.RaiseAndSetIfChanged(ref this.username, value);
        }

        /// <summary>
        /// Backing field for the <see cref="Password"/> property
        /// </summary>
        private string password;

        /// <summary>
        /// Gets or sets server password value
        /// </summary>
        public string Password
        {
            get => this.password;
            set => this.RaiseAndSetIfChanged(ref this.password, value);
        }

        /// <summary>
        /// Backing field for the <see cref="Uri"/> property
        /// </summary>
        private string uri;

        /// <summary>
        /// Gets or sets server uri
        /// </summary>
        public string Uri
        {
            get => this.uri;
            set => this.RaiseAndSetIfChanged(ref this.uri, value);
        }

        /// <summary>
        /// Backing field for the <see cref="LoginSuccessful"/> property
        /// </summary>
        private bool loginSuccessful;

        /// <summary>
        /// Gets or sets login succesfully flag
        /// </summary>
        public bool LoginSuccessful
        {
            get => this.loginSuccessful;
            private set => this.RaiseAndSetIfChanged(ref this.loginSuccessful, value);
        }

        /// <summary>
        /// Backing field for <see cref="RequiresAuthentication"/>
        /// </summary>
        private bool requiresAuthentication;
        
        /// <summary>
        /// Gets or sets an assert whether the specified <see cref="Uri"/> endpoint requires authentication
        /// </summary>
        public bool RequiresAuthentication
        {
            get => this.requiresAuthentication;
            set => this.RaiseAndSetIfChanged(ref this.requiresAuthentication, value);
        }

        /// <summary>
        /// Gets or sets the saved server addresses
        /// </summary>
        public ReactiveList<string> SavedUris { get; private set; } = new ReactiveList<string> { ChangeTrackingEnabled = true };

        /// <summary>
        /// Gets the command responsible for adding the current <see cref="Uri"/> to <see cref="SavedUris"/>
        /// </summary>
        public ReactiveCommand<object> SaveCurrentUriCommand { get; private set; }

        /// <summary>
        /// Gets the server login command
        /// </summary>
        public ReactiveCommand<Unit> LoginCommand { get; private set; }
        
        /// <summary>
        /// Gets or sets the <see cref="ICloseWindowBehavior"/> instance
        /// </summary>
        public ICloseWindowBehavior CloseWindowBehavior { get; set; }

        /// <summary>
        /// Backing field for <see cref="SelectedExternalIdentifierMap"/>
        /// </summary>
        private ExternalIdentifierMap selectedExternalIdentifierMap;

        /// <summary>
        /// Gets or sets the selected <see cref="ExternalIdentifierMap"/>
        /// </summary>
        public ExternalIdentifierMap SelectedExternalIdentifierMap
        {
            get => this.selectedExternalIdentifierMap;
            set => this.RaiseAndSetIfChanged(ref this.selectedExternalIdentifierMap, value);
        }

        /// <summary>
        /// Backing field for <see cref="ExternalIdentifierMapNewName"/>
        /// </summary>
        private string externalIdentifierMapNewName;

        /// <summary>
        /// Gets or sets the name for creating a new <see cref="ExternalIdentifierMap"/>
        /// </summary>
        public string ExternalIdentifierMapNewName
        {
            get => this.externalIdentifierMapNewName;
            set => this.RaiseAndSetIfChanged(ref this.externalIdentifierMapNewName, value);
        }

        /// <summary>
        /// Backing field for <see cref="CreateNewMappingConfigurationChecked"/>
        /// </summary>
        private bool createNewMappingConfigurationChecked;

        /// <summary>
        /// Gets or sets the checked checkbox assert that selects that a new mapping configuration will be created
        /// </summary>
        public bool CreateNewMappingConfigurationChecked
        {
            get => this.createNewMappingConfigurationChecked;
            set => this.RaiseAndSetIfChanged(ref this.createNewMappingConfigurationChecked, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="ReactiveList{T}"/> of available <see cref="ExternalIdentifierMap"/>
        /// </summary>
        public ReactiveList<ExternalIdentifierMap> AvailableExternalIdentifierMap { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DstLoginViewModel"/> class.
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController"/></param>
        /// <param name="statusBarControlView">The <see cref="IStatusBarControlViewModel"/></param>
        /// <param name="userPreferenceService">The <see cref="IUserPreferenceService{AppSettings}"/></param>
        /// <param name="hubController">The <see cref="IHubController"/></param>
        public DstLoginViewModel(IDstController dstController, IStatusBarControlViewModel statusBarControlView, 
            IUserPreferenceService<AppSettings> userPreferenceService, IHubController hubController)
        {
            this.dstController = dstController;
            this.statusBarControlView = statusBarControlView;
            this.userPreferenceService = userPreferenceService;

            this.PopulateSavedUris();

            var canSaveUri = this.SavedUris.CountChanged.StartWith(0).CombineLatest(this.WhenAnyValue(vm => vm.Uri),
                (args, uri) => !string.IsNullOrWhiteSpace(uri) && !this.SavedUris.Contains(uri));

            this.SaveCurrentUriCommand = ReactiveCommand.Create(canSaveUri);
            this.SaveCurrentUriCommand.Subscribe(_ => this.ExecuteSaveCurrentUri());

            this.AvailableExternalIdentifierMap = new ReactiveList<ExternalIdentifierMap>(
                hubController.AvailableExternalIdentifierMap(this.dstController.ThisToolName));

            this.WhenAnyValue(x => x.SelectedExternalIdentifierMap).Subscribe(_ =>
            {
                if (this.SelectedExternalIdentifierMap != null)
                {
                    this.CreateNewMappingConfigurationChecked = false;
                    this.ExternalIdentifierMapNewName = null;
                }
            });
            
            this.WhenAnyValue(x => x.ExternalIdentifierMapNewName).Subscribe(_ =>
            {
                if (!string.IsNullOrWhiteSpace(this.ExternalIdentifierMapNewName))
                {
                    this.CreateNewMappingConfigurationChecked = true;
                    this.SelectedExternalIdentifierMap = null;
                }
            });

            var canLogin = this.WhenAnyValue(
                vm => vm.UserName,
                vm => vm.Password,
                vm => vm.RequiresAuthentication,
                vm => vm.Uri,
                vm => vm.SelectedExternalIdentifierMap,
                vm => vm.ExternalIdentifierMapNewName,
                (username, password, requiresAuthentication, uri, map, mapNew) =>
                    (!string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(username) || !requiresAuthentication)
                    && !string.IsNullOrWhiteSpace(uri) && (map != null || !string.IsNullOrWhiteSpace(mapNew)));

            this.LoginCommand = ReactiveCommand.CreateAsyncTask(canLogin, 
                async _ => await this.ExecuteLogin(), RxApp.MainThreadScheduler);
            
            this.LoginCommand.ThrownExceptions
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(exception =>
                {
                    this.statusBarControlView.Append($"Loggin failed: {exception.Message}", StatusBarMessageSeverity.Error);
                    this.IsBusy = false;
                });

            this.LoginCommand.ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.LoginCommandIsDoneExecuting());

            this.WhenAnyValue(x => x.CreateNewMappingConfigurationChecked).Subscribe(_ => this.UpdateExternalIdentifierSelectors());
        }

        private void LoginCommandIsDoneExecuting()
        {
            this.IsBusy = false;
            this.LoginSuccessful = this.dstController.IsSessionOpen;

            if (this.LoginSuccessful)
            {
                this.statusBarControlView.Append("Loggin successful");
                this.CloseWindowBehavior?.Close();
            }
            else
            {
                this.statusBarControlView.Append($"Loggin failed", StatusBarMessageSeverity.Info);
            }
        }

        /// <summary>
        /// Updates the respective field depending on the user selection
        /// </summary>
        private void UpdateExternalIdentifierSelectors()
        {
            if (this.CreateNewMappingConfigurationChecked)
            {
                this.SelectedExternalIdentifierMap = null;
            }
            else
            {
                this.ExternalIdentifierMapNewName = null;
            }
        }

        /// <summary>
        /// Loads the saved server addresses into the <see cref="SavedUris"/>
        /// </summary>
        private void PopulateSavedUris()
        {
            this.userPreferenceService.Read();
            this.SavedUris.Clear();
            this.SavedUris.AddRange(this.userPreferenceService.UserPreferenceSettings.SavedOpcUris);
        }

        /// <summary>
        /// Executes the <see cref="SaveCurrentUriCommand"/>
        /// </summary>
        private void ExecuteSaveCurrentUri()
        {
            this.userPreferenceService.UserPreferenceSettings.SavedOpcUris.Add(this.Uri);
            this.userPreferenceService.Save();
            this.PopulateSavedUris();
        }

        /// <summary>
        /// Executes login command
        /// </summary>
        private async Task ExecuteLogin()
        {
            this.IsBusy = true;

            this.ProcessExternalIdentifierMap();

            this.statusBarControlView.Append("Loggin in...");

            var credentials = this.RequiresAuthentication ? new UserIdentity(this.UserName, this.Password) : null;
            await this.dstController.Connect(this.Uri, true, credentials);

            this.LoginCommandIsDoneExecuting();
        }

        /// <summary>
        /// Creates a new <see cref="ExternalIdentifierMap"/> and or set the <see cref="IDstController.ExternalIdentifierMap"/>
        /// </summary>
        private void ProcessExternalIdentifierMap()
        {
            this.dstController.ExternalIdentifierMap = this.SelectedExternalIdentifierMap?.Clone(true) ??
                                                       this.dstController.CreateExternalIdentifierMap(this.ExternalIdentifierMapNewName);
        }
    }
}
