// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindowViewModel.cs" company="RHEA System S.A.">
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
    using DEHPEcosimPro.ViewModel.Interfaces;

    using ReactiveUI;

    /// <summary>
    /// <see cref="MainWindowViewModel"/> is the view model for <see cref="Views.MainWindow"/>
    /// </summary>
    public class MainWindowViewModel : ReactiveObject, IMainWindowViewModel
    {
        /// <summary>
        /// Gets the view model that represents the 10-25 data source
        /// </summary>
        public IDataSourceViewModel Object1025 { get; private set; }

        /// <summary>
        /// Gets the view model that represents the EcosimPro data source
        /// </summary>
        public IDataSourceViewModel ObjectEcosimPro { get; private set; }

        /// <summary>
        /// Initializes a new <see cref="MainWindowViewModel"/>
        /// </summary>
        public MainWindowViewModel(IDataSourceViewModel object1025ViewModel, IDataSourceViewModel objectEcosimProViewModel)
        {
            this.InitializeCommands();
            this.Object1025 = object1025ViewModel;
            this.ObjectEcosimPro = objectEcosimProViewModel;
        }

        /// <summary>
        /// Initializes the <see cref="ReactiveCommand{T}"/>
        /// </summary>
        private void InitializeCommands()
        {
        }
    }
}
