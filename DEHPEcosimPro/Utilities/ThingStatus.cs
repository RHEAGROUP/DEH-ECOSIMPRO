// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ThingStatus.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.Utilities
{
    using System.Linq;

    using CDP4Common.CommonData;

    /// <summary>
    /// A class that gives information on the status of a <see cref="Thing"/>
    /// </summary>
    public class ThingStatus
    {
        /// <summary>
        /// Initializes a new instace of the <see cref="ThingStatus"/> class
        /// </summary>
        /// <param name="thing">The <see cref="Thing"/></param>
        public ThingStatus(Thing thing)
        {
            this.Thing = thing;
            this.HasError = thing.ValidationErrors.Any();
            this.HasRelationship = thing.HasRelationship;
        }

        /// <summary>
        /// Gets the <see cref="Thing"/>
        /// </summary>
        public Thing Thing { get; }

        /// <summary>
        /// Asserts whether the <see cref="Thing"/> has errors
        /// </summary>
        public bool HasError { get; }

        /// <summary>
        /// Gets a value indicating whether the thing has associated relationships
        /// </summary>
        public bool HasRelationship { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the thing is marked as a user's favorite
        /// </summary>
        public bool IsFavorite { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the thing is marked as locked
        /// </summary>
        public bool IsLocked { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the thing is marked as hidden
        /// </summary>
        public bool IsHidden { get; set; } = false;
    }
}
