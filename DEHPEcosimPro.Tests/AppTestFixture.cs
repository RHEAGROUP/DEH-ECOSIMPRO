﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AppTestFixture.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Tests
{
    using System.Collections;
    using System.Collections.Generic;

    using Autofac;

    using DEHPCommon;

    using NUnit.Framework;

    [TestFixture]
    public class AppTestFixture
    {
        [Test]
        public void VerifyContainerIsBuilt()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<List<byte>>().As<IList>();
            Assert.IsNotNull(new App(containerBuilder));
            Assert.IsNotNull(AppContainer.Container.Resolve<IList>());
        }
    }
}
