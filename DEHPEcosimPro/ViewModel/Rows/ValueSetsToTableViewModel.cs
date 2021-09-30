// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChooseMappingColumnsViewModel.cs" company="RHEA System S.A.">
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

namespace DEHPEcosimPro.ViewModel.Rows
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    /// <summary>
    /// Takes the valueset of a parameter and transform it into tables
    /// </summary>
    public class ValueSetsToTableViewModel
    {
        /// <summary>
        /// Takes the valueset of a parameter and transform it into tables
        /// </summary>
        public ValueSetsToTableViewModel()
        {
        }

        /// <summary>
        /// Takes the valueset of a parameter and transform it into tables
        /// </summary>
        /// <param name="parameter">parameter that contain the valueset</param>
        /// <param name="SelectedOption">option of the parameter to find the valueset</param>
        /// <param name="SelectedState">state of the parameter to find the valueset</param>
        public ValueSetsToTableViewModel(ParameterOrOverrideBase parameter, Option SelectedOption, ActualFiniteState SelectedState)
        {
            if (parameter.ParameterType is SampledFunctionParameterType parameterType)
            { 
                this.ValueSet = parameter.QueryParameterBaseValueSet(SelectedOption, SelectedState);
                this.ResetDataTables();
                this.FeedDataTable(parameterType);

                this.GetListOfValueByColumns();
            }
        }

        /// <summary>
        /// Tuples that contain the name of the column and the datas of the column
        /// </summary>
        public List<(string name, List<string> list)> ListOfTuple { get; set; } = new List<(string name, List<string> list)>();

        /// <summary>
        /// Table of the data, with the columns' names, the rows and the data
        /// </summary>
        public DataTable PublishedValueTable { get; set; }

        /// <summary>
        /// Valueset of the data we want to put on the table
        /// </summary>
        protected IValueSet ValueSet { get; set; }

        /// <summary>
        /// convert the datable <see cref="PublishedValueTable"/> in tuples <see cref="ListOfTuple"/>
        /// </summary>
        private void GetListOfValueByColumns()
        {
            for (var i = 0; i < this.PublishedValueTable.Columns.Count; i++)
            {
                var nameofcolumn = this.PublishedValueTable.Columns[i].ColumnName;
                var myColumn = new List<string>();

                foreach (var row in this.PublishedValueTable.Rows.Cast<DataRow>().ToArray())
                {
                    myColumn.Add(row[i].ToString());
                }

                (string name, List<string> list) listOfValueByColumns = (nameofcolumn, myColumn);

                this.ListOfTuple.Add(listOfValueByColumns);
            }
        }

        /// <summary>
        /// Add columns and rows to the datatable <see cref="PublishedValueTable"/>
        /// </summary>
        /// <param name="parameterType"></param>
        private void FeedDataTable(SampledFunctionParameterType parameterType)
        {
            foreach (var parameterTypeAssignment in parameterType.IndependentParameterType.ToList())
            {
                var columnName = parameterTypeAssignment.ParameterType.ShortName;
                this.AddColumnToTables(columnName, typeof(object));
            }

            foreach (var parameterTypeAssignment in parameterType.DependentParameterType.ToList())
            {
                var columnName = parameterTypeAssignment.ParameterType.ShortName;
                this.AddColumnToTables(columnName, typeof(object));
            }

            var columns = parameterType.NumberOfValues;

            this.AddRowsToTables(columns);
        }

        /// <summary>
        /// Adds rows to all tables
        /// </summary>
        /// <param name="columns">The number of columns</param>
        private void AddRowsToTables(int columns)
        {
            if (this.ValueSet is ParameterValueSetBase parameterValueSetBase)
            {
                this.AddRowsToTable(parameterValueSetBase.Published, this.PublishedValueTable, columns);
            }
        }

        /// <summary>
        /// Add a column to all the tables.
        /// </summary>
        /// <param name="name">The name of the column</param>
        /// <param name="objectType">The type of cell object</param>
        private void AddColumnToTables(string name, Type objectType)
        {
            this.PublishedValueTable.Columns.Add(name, objectType);
        }

        /// <summary>
        /// Adds rows to a specific table
        /// </summary>
        /// <param name="valueArray">The value array with data</param>
        /// <param name="table">The data table to fill</param>
        /// <param name="columns">The number of columns</param>
        private void AddRowsToTable(ValueArray<string> valueArray, DataTable table, int columns)
        {
            foreach (var valueChunk in this.SplitValues(valueArray, columns))
            {
                var rowValue = table.NewRow();
                var valueCounter = 0;

                foreach (var value in valueChunk)
                {
                    rowValue[valueCounter] = value;
                    valueCounter++;
                }

                table.Rows.Add(rowValue);
            }
        }

        /// <summary>
        /// Splits the valueset into chunks based on number of independent and dependent parametertype allocations
        /// </summary>
        /// <param name="values">The entire value array</param>
        /// <param name="nSize">The size of chunks to split into</param>
        /// <returns>An IEnumerable of the lists of chunks.</returns>
        private IEnumerable<List<string>> SplitValues(ValueArray<string> values, int nSize = 30)
        {
            for (var i = 0; i < values.Count; i += nSize)
            {
                yield return values.ToList().GetRange(i, Math.Min(nSize, values.Count - i));
            }
        }

        /// <summary>
        /// Resets the data table
        /// </summary>
        private void ResetDataTables()
        {
            this.PublishedValueTable = this.ResetDataTable();
        }

        /// <summary>
        /// Resets a single data table
        /// </summary>
        /// >
        private DataTable ResetDataTable()
        {
            var table = new DataTable();
            table.Rows.Clear();
            table.Columns.Clear();

            return table;
        }
    }
}
