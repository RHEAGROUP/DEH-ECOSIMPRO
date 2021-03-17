// --------------------------------------------------------------------------------------------------------------------
// <copyright file="App.xaml.cs" company="RHEA System S.A.">
//    Copyright (c) 2015-2019 RHEA System S.A.
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

namespace DEHPEcosimPro
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Threading;

    using Autofac;

    using DEHPCommon;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.Services.NavigationService;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;
    using DEHPCommon.UserPreferenceHandler.UserPreferenceService;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Services.OpcConnector;
    using DEHPEcosimPro.Services.OpcConnector.Interfaces;
    using DEHPEcosimPro.Settings;
    using DEHPEcosimPro.ViewModel;
    using DEHPEcosimPro.ViewModel.Dialogs;
    using DEHPEcosimPro.ViewModel.Dialogs.Interfaces;
    using DEHPEcosimPro.ViewModel.Interfaces;
    using DEHPEcosimPro.ViewModel.NetChangePreview;
    using DEHPEcosimPro.Views;

    using DevExpress.Xpf.Core;

    using NLog;

    using DXSplashScreenViewModel = DevExpress.Mvvm.DXSplashScreenViewModel;
    using SplashScreen = DEHPCommon.UserInterfaces.Views.SplashScreen;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class App
    {
        /// <summary>
        /// The <see cref="NLog"/> logger
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        
        /// <summary>
        /// Initializes a new <see cref="App"/>
        /// </summary>
        /// <param name="containerBuilder">An optional <see cref="Container"/></param>
        public App(ContainerBuilder containerBuilder = null)
        {
            this.LogAppStart();
            this.Exit += this.OnExit;
            AppDomain.CurrentDomain.UnhandledException += this.CurrentDomainUnhandledException;
            var splashScreenViewModel = new DXSplashScreenViewModel() { Title = "DEHP-EcosimPro Adapter", Logo = new Uri("pack://application:,,,/Resources/logo.png") };
            SplashScreenManager.Create(() => new SplashScreen(), splashScreenViewModel).ShowOnStartup();
            containerBuilder ??= new ContainerBuilder();
            RegisterTypes(containerBuilder);
            RegisterViewModels(containerBuilder);
            AppContainer.BuildContainer(containerBuilder);
        }

        private void LogAppStart()
        {
            this.logger.Info("-----------------------------------------------------------------------------------------");
            this.logger.Info($"Starting EcosimPro Adapter {Assembly.GetExecutingAssembly().GetName().Version}");
            this.logger.Info("-----------------------------------------------------------------------------------------");
        }

        /// <summary>
        /// Handles dispatcher unhandled exception
        /// </summary>
        /// <param name="sender">The <see cref="object"/> sender</param>
        /// <param name="e">The <see cref="UnhandledExceptionEventArgs"/></param>
        public void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            this.logger.Error(e.Exception);
            e.Handled = true;
        }

        /// <summary>
        /// Warn when an exception is thrown and log it 
        /// </summary>
        /// <param name="sender">The <see cref="object"/> sender</param>
        /// <param name="e">The <see cref="UnhandledExceptionEventArgs"/></param>
        private void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var errorMessage = $"{sender} has thrown {e.ExceptionObject.GetType()} \n\r {(e.ExceptionObject as Exception)?.Message}";
            MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            this.logger.Error(e.ExceptionObject);
        }

        /// <summary>
        /// Occurs when the app closes, it makes sure any opc connection are properly closed
        /// </summary>
        /// <param name="sender">The <see cref="object"/> sender</param>
        /// <param name="e">The <see cref="ExitEventArgs"/></param>
        private void OnExit(object sender, ExitEventArgs e)
        {
            var opcClientHandler = AppContainer.Container.Resolve<IOpcSessionHandler>();
            opcClientHandler?.CloseSession();
        }

        /// <summary>
        /// Occurs when <see cref="Application"/> starts, starts a new <see cref="ILifetimeScope"/> and open the <see cref="Application.MainWindow"/>
        /// </summary>
        /// <param name="e">The <see cref="StartupEventArgs"/></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            using (var scope = AppContainer.Container.BeginLifetimeScope())
            {
                scope.Resolve<INavigationService>().Show<MainWindow>();
            }

            base.OnStartup(e);
        }

        /// <summary>
        /// Registers the types that can be resolved by the <see cref="IContainer"/>
        /// </summary>
        /// <param name="containerBuilder">The <see cref="ContainerBuilder"/></param>
        private static void RegisterTypes(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<OpcClientService>().As<IOpcClientService>().SingleInstance();
            containerBuilder.RegisterType<OpcSessionHandler>().As<IOpcSessionHandler>().SingleInstance();
            containerBuilder.RegisterType<OpcSessionReconnectHandler>().As<IOpcSessionReconnectHandler>().SingleInstance();
            containerBuilder.RegisterType<OpcClientService>().As<IOpcClientService>().SingleInstance();
            containerBuilder.RegisterType<DstController.DstController>().As<IDstController>().SingleInstance();
            containerBuilder.RegisterType<UserPreferenceService<AppSettings>>().As<IUserPreferenceService<AppSettings>>().SingleInstance();
            containerBuilder.RegisterType<MappingEngine>().As<IMappingEngine>().WithParameter(MappingEngine.ParameterName, Assembly.GetExecutingAssembly());
        }

        /// <summary>
        /// Registers all the view model so the depencies can be injected
        /// </summary>
        /// <param name="containerBuilder">The <see cref="ContainerBuilder"/></param>
        private static void RegisterViewModels(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<MainWindowViewModel>().As<IMainWindowViewModel>().SingleInstance();
            containerBuilder.RegisterType<HubDataSourceViewModel>().As<IHubDataSourceViewModel>().SingleInstance();
            containerBuilder.RegisterType<DstBrowserHeaderViewModel>().As<IDstBrowserHeaderViewModel>().SingleInstance();
            containerBuilder.RegisterType<DstDataSourceViewModel>().As<IDstDataSourceViewModel>().SingleInstance();
            containerBuilder.RegisterType<DstLoginViewModel>().As<IDstLoginViewModel>();
            containerBuilder.RegisterType<DstVariablesControlViewModel>().As<IDstVariablesControlViewModel>().SingleInstance();
            containerBuilder.RegisterType<DstMappingConfigurationDialogViewModel>().As<IDstMappingConfigurationDialogViewModel>();
            containerBuilder.RegisterType<HubMappingConfigurationDialogViewModel>().As<IHubMappingConfigurationDialogViewModel>();
            containerBuilder.RegisterType<EcosimProTransferControlViewModel>().As<ITransferControlViewModel>().SingleInstance();
            containerBuilder.RegisterType<HubNetChangePreviewViewModel>().As<IHubNetChangePreviewViewModel>().SingleInstance();
            containerBuilder.RegisterType<DstNetChangePreviewViewModel>().As<IDstNetChangePreviewViewModel>().SingleInstance();
            containerBuilder.RegisterType<MappingViewModel>().As<IMappingViewModel>().SingleInstance();
        }
    }
}
