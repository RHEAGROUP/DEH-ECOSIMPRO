// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AppContainer.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro
{
    using Autofac;

    using DEHPEcosimPro.Services.IconCacheService;

    /// <summary>
    /// Provides the IOContainer for DI for this Application
    /// </summary>
    public static class AppContainer
    {
        /// <summary>
        /// Gets the <see cref="App"/> <see cref="IContainer"/>
        /// </summary>
        public static IContainer Container { get; set; }

        /// <summary>
        /// Builds the <see cref="Container"/>
        /// </summary>
        /// <param name="containerBuilder">An optional <see cref="Container"/></param>
        public static void BuildContainer(ContainerBuilder containerBuilder = null)
        {
            containerBuilder ??= new ContainerBuilder();
            containerBuilder.RegisterType<IconCacheService>().As<IIconCacheService>().SingleInstance();
            Container = containerBuilder.Build();
        }
    }
}
