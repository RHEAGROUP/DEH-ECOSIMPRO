// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TreeListSortingBehavior.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2022 RHEA System S.A.
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

namespace DEHPEcosimPro.Behaviors
{
    using System;

    using DevExpress.Mvvm.UI.Interactivity;
    using DevExpress.Xpf.Grid;
    using DevExpress.Xpf.Grid.TreeList;

    public class TreeListSortingBehavior: Behavior<TreeListView>
    {
        /// <summary>
        /// Register event handlers
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.CustomColumnSort += this.AssociatedObject_CustomColumnSort;
        }

        /// <summary>
        /// Unregister event handlers
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.AssociatedObject.CustomColumnSort -= this.AssociatedObject_CustomColumnSort;
        }

        /// <summary>
        /// Handle the sort behaviour as we have to compare non-specific object types
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The argument</param>
        public void AssociatedObject_CustomColumnSort(object sender, TreeListCustomColumnSortEventArgs e)
        {
            IComparable value1 = e.Value1 as IComparable;
            IComparable value2 = e.Value2 as IComparable;
            e.Handled = true;

            if (value1 == null)
            {
                e.Result = value2 == null ? 0 : -1;
                return;
            }

            if (value2 == null)
            {
                e.Result =  1;
                return;
            }

            Type type1 = e.Value1.GetType();
            Type type2 = e.Value2.GetType();

            if (type1 != type2)
            {
                try
                {
                    decimal value1InDecimal = Convert.ToDecimal(e.Value1);
                    decimal value2InDecimal = Convert.ToDecimal(e.Value2);
                    e.Result = value1InDecimal.CompareTo(value2InDecimal);
                    return;
                }
                catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
                {
                    int hashCompare = type1.GetHashCode().CompareTo(type2.GetHashCode());
                    e.Result = hashCompare != 0 ? hashCompare : string.CompareOrdinal(type1.Name, type2.Name);
                    return;
                }
            }

            e.Result = value1.CompareTo(value2);
        }
    }
}
