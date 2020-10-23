// --------------------------------------------------------------------------------------------------------------------
// <copyright file="App.xaml.cs" company="RHEA System S.A.">
//    Copyright (c) 2015-2019 RHEA System S.A.
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

namespace DEHPEcosimPro
{
    using System.Windows;

    using Autofac;

    using DEHPCommon;
    using DEHPCommon.Services.NavigationService;

    using DEHPEcosimPro.ViewModel;
    using DEHPEcosimPro.ViewModel.Interfaces;
    using DEHPEcosimPro.Views;

    using DevExpress.Xpf.Core;

    using DXSplashScreenViewModel = DevExpress.Mvvm.DXSplashScreenViewModel;
    using SplashScreen = DEHPCommon.UserInterfaces.Views.SplashScreen;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        /// <summary>
        /// Initializes a new <see cref="App"/>
        /// </summary>
        /// <param name="containerBuilder">An optional <see cref="Container"/></param>
        public App(ContainerBuilder containerBuilder = null)
        {
            var splashScreenViewModel = new DXSplashScreenViewModel() { Title = "DEHP-EcosimPro Adapter"};
            SplashScreenManager.Create(() => new SplashScreen(), splashScreenViewModel).ShowOnStartup();
            containerBuilder ??= new ContainerBuilder();
            RegisterViewModels(containerBuilder);
            AppContainer.BuildContainer(containerBuilder);
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
        /// Registers all the view model so the depencies can be injected
        /// </summary>
        /// <param name="containerBuilder">The <see cref="ContainerBuilder"/></param>
        private static void RegisterViewModels(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<MainWindowViewModel>().As<IMainWindowViewModel>().SingleInstance();
            containerBuilder.RegisterType<DataSourceViewModel>().As<IDataSourceViewModel>();
        }
    }
}
