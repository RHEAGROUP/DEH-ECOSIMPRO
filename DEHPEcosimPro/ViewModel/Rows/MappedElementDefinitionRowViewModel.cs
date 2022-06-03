// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MappedElementDefinitionRowViewModel.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2021 RHEA System S.A.
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

namespace DEHPEcosimPro.ViewModel.Rows
{
    using System;

    using CDP4Common.EngineeringModelData;

    using ReactiveUI;

    /// <summary>
    /// Represents a single value from the Hub source and a <see cref="SelectedVariable"/> to update with this value
    /// </summary>
    public class MappedElementDefinitionRowViewModel : ReactiveObject, IDisposable
    {
        /// <summary>
        /// The <see cref="IDisposable"/> for the Subscription
        /// </summary>
        private readonly IDisposable disposable;

        /// <summary>
        /// Backing field for <see cref="SelectedParameter"/>
        /// </summary>
        private ParameterOrOverrideBase selectedParameter;

        /// <summary>
        /// Gets or sets the source <see cref="Parameter"/>
        /// </summary>
        public ParameterOrOverrideBase SelectedParameter
        {
            get => this.selectedParameter;
            set => this.RaiseAndSetIfChanged(ref this.selectedParameter, value);
        }

        /// <summary>
        /// Backing field for <see cref="SelectedValue"/>
        /// </summary>
        private ValueSetValueRowViewModel selectedValue;

        /// <summary>
        /// Gets or sets the value to be transfered to the <see cref="SelectedVariable"/>
        /// </summary>
        public ValueSetValueRowViewModel SelectedValue
        {
            get => this.selectedValue;
            set => this.RaiseAndSetIfChanged(ref this.selectedValue, value);
        }

        /// <summary>
        /// Backing field for <see cref="SelectedVariable"/>
        /// </summary>
        private VariableBaseRowViewModel selectedVariable;

        /// <summary>
        /// Gets or sets the <see cref="VariableRowViewModel"/> holding the destination <see cref="Opc.Ua.ReferenceDescription"/>
        /// </summary>
        public VariableBaseRowViewModel SelectedVariable
        {
            get => this.selectedVariable;
            set => this.RaiseAndSetIfChanged(ref this.selectedVariable, value);
        }

        /// <summary>
        /// Backing field fopr <see cref="IsValid"/>
        /// </summary>
        private bool isValid;

        /// <summary>
        /// Gets a value indicating whether this <see cref="MappedElementDefinitionRowViewModel"/> is ready to be mapped
        /// </summary>
        public bool IsValid
        {
            get => this.isValid;
            set => this.RaiseAndSetIfChanged(ref this.isValid, value);
        }
        
        /// <summary>
        /// Gets or sets the mapping configuration
        /// </summary>
        public IdCorrespondence MappingConfiguration { get; set; }

        /// <summary>
        /// Initializes a new <see cref="MappedElementDefinitionRowViewModel"/>
        /// </summary>
        public MappedElementDefinitionRowViewModel()
        {
            this.disposable = this.WhenAnyValue(x => x.SelectedVariable,
                    x => x.SelectedValue)
                .Subscribe(_ => this.VerifyValidity());
        }

        /// <summary>
        /// Initializes a new <see cref="MappedElementDefinitionRowViewModel"/>
        /// </summary>
        /// <param name="valueSet">The <see cref="IValueSet"/></param>
        /// <param name="valueIndex">The value index</param>
        /// <param name="switchKind">The <see cref="ParameterSwitchKind"/></param>
        public MappedElementDefinitionRowViewModel(IValueSet valueSet, int valueIndex, ParameterSwitchKind switchKind) : this()
        {
            this.SelectedParameter = (valueSet as ParameterValueSetBase)?.GetContainerOfType<ParameterOrOverrideBase>();
            this.SelectedValue = new ValueSetValueRowViewModel(valueSet, valueIndex, switchKind);
        }

        /// <summary>
        /// Verify validity of the this <see cref="MappedElementDefinitionRowViewModel"/>
        /// </summary>
        public void VerifyValidity()
        {
            this.IsValid = this.SelectedValue != null && 
                           this.SelectedValue.Value != "-" && 
                           this.SelectedVariable != null;
        }

        /// <summary>
        /// Dispose the <see cref="MappedElementDefinitionRowViewModel" />
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Dispose this <see cref="MappedElementDefinitionRowViewModel" />
        /// </summary>
        /// <param name="disposing">A value indicating if it sould dispose or not</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.SelectedVariable?.Dispose();
                this.disposable.Dispose();
            }
        }
    }
}
