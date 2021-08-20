// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DifferenceEvent.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2021 RHEA System S.A.
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

namespace DEHPEcosimPro.Events
{
    using CDP4Common.CommonData;

    /// <summary>
    /// TODO
    /// </summary>
    /// <typeparam name="TThing"></typeparam>
    public class DifferenceEvent<TThing> where TThing : Thing
    {
        /// <summary>
        /// 
        /// </summary>
        public bool HasTheselectionChanged { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public TThing Thing { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="HasTheselectionChanged"></param>
        /// <param name="Thing"></param>
        public DifferenceEvent(bool HasTheselectionChanged, TThing Thing)
        {
            this.HasTheselectionChanged = HasTheselectionChanged;
            this.Thing = Thing;

        }
    }
}
