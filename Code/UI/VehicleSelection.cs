// <copyright file="VehicleSelection.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace VehicleSelector
{
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Warehouse vehicle controls.
    /// </summary>
    internal class VehicleSelection : UIPanel
    {
        /// <summary>
        /// Panel height.
        /// </summary>
        internal const float PanelHeight = VehicleListY + VehicleListHeight + Margin;

        /// <summary>
        /// List height.
        /// </summary>
        internal const float VehicleListHeight = 240f;

        // Layout constants - private.
        private const float Margin = 5f;
        private const float VehicleListY = 45f;

        // Panel components.
        private readonly UILabel _titleLabel;
        private readonly UIButton _addVehicleButton;
        private readonly UIButton _removeVehicleButton;
        private readonly VehicleSelectionPanel _vehicleSelectionPanel;
        private readonly SelectedVehiclePanel _selectedVehiclePanel;

        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleSelection"/> class.
        /// </summary>
        internal VehicleSelection()
        {
            // Set size.
            height = PanelHeight;
            width = BuildingPanel.PanelWidth - Margin - Margin;

            // Appearance.
            atlas = UITextures.InGameAtlas;
            backgroundSprite = "GenericPanelLight";
            color = new Color32(160, 160, 160, 255);

            // Title.
            _titleLabel = UILabels.AddLabel(this, 0f, 10f, "Select vehicle", BuildingPanel.PanelWidth, 1f, UIHorizontalAlignment.Center);

            // 'Add vehicle' button.
            _addVehicleButton = UIButtons.AddIconButton(
                this,
                BuildingPanel.MidControlX,
                VehicleListY,
                BuildingPanel.ArrowSize,
                UITextures.LoadQuadSpriteAtlas("TC-ArrowPlus"),
                Translations.Translate("ADD_VEHICLE_TIP"));
            _addVehicleButton.isEnabled = false;
            _addVehicleButton.eventClicked += (control, clickEvent) => AddVehicle(_vehicleSelectionPanel.SelectedVehicle);

            // Remove vehicle button.
            _removeVehicleButton = UIButtons.AddIconButton(
                this,
                BuildingPanel.MidControlX,
                VehicleListY + BuildingPanel.ArrowSize,
                BuildingPanel.ArrowSize,
                UITextures.LoadQuadSpriteAtlas("TC-ArrowMinus"),
                Translations.Translate("REMOVE_VEHICLE_TIP"));
            _removeVehicleButton.isEnabled = false;
            _removeVehicleButton.eventClicked += (control, clickEvent) => RemoveVehicle();

            // Vehicle selection panels.
            _selectedVehiclePanel = this.AddUIComponent<SelectedVehiclePanel>();
            _selectedVehiclePanel.relativePosition = new Vector2(Margin, VehicleListY);
            _selectedVehiclePanel.ParentPanel = this;
            _vehicleSelectionPanel = this.AddUIComponent<VehicleSelectionPanel>();
            _vehicleSelectionPanel.ParentPanel = this;
            _vehicleSelectionPanel.relativePosition = new Vector2(BuildingPanel.RightColumnX, VehicleListY);

            // Vehicle selection list labels.
            UILabels.AddLabel(_vehicleSelectionPanel.VehicleList, 0f, -15f, Translations.Translate("AVAILABLE_VEHICLES"), BuildingPanel.SelectionWidth, 0.8f, UIHorizontalAlignment.Center);
            UILabels.AddLabel(_selectedVehiclePanel.VehicleList, 0f, -15f, Translations.Translate("SELECTED_VEHICLES"), BuildingPanel.SelectionWidth, 0.8f, UIHorizontalAlignment.Center);
        }

        /// <summary>
        /// Gets or sets the parent tab reference.
        /// </summary>
        internal BuildingPanel ParentPanel { get; set; }

        /// <summary>
        /// Gets the current transfer reason.
        /// </summary>
        internal TransferManager.TransferReason TransferReason { get; private set; }

        /// <summary>
        /// Gets othe currently selected building.
        /// </summary>
        internal ushort CurrentBuilding { get; private set; }

        /// <summary>
        /// Sets/changes the currently selected building.
        /// </summary>
        /// <param name="buildingID">New building ID.</param>
        /// <param name="title">Selection list title string.</param>
        /// <param name="reason">Transfer reason for this vehicle selection.</param>
        internal void SetTarget(ushort buildingID, string title, TransferManager.TransferReason reason)
        {
            // Ensure valid building.
            if (buildingID != 0)
            {
                CurrentBuilding = buildingID;
                TransferReason = reason;
                _titleLabel.text = title;

                Refresh();
            }
        }

        /// <summary>
        /// Refreshes list contents.
        /// </summary>
        internal void Refresh()
        {
            _selectedVehiclePanel.RefreshList();
            _vehicleSelectionPanel.RefreshList();
        }

        /// <summary>
        /// Update button states when vehicle selections are updated.
        /// </summary>
        internal void SelectionUpdated()
        {
            _addVehicleButton.isEnabled = _vehicleSelectionPanel.SelectedVehicle != null;
            _removeVehicleButton.isEnabled = _selectedVehiclePanel.SelectedVehicle != null;
        }

        /// <summary>
        /// Adds a vehicle to the list for this transfer.
        /// </summary>
        /// <param name="vehicle">Vehicle prefab to add.</param>
        private void AddVehicle(VehicleInfo vehicle)
        {
            // Add vehicle to building.
            VehicleControl.AddVehicle(CurrentBuilding, TransferReason, vehicle);

            // Update current selection.
            _selectedVehiclePanel.SelectedVehicle = vehicle;

            // Update district lists.
            _selectedVehiclePanel.RefreshList();
            _vehicleSelectionPanel.RefreshList();
        }

        /// <summary>
        /// Removes the currently selected district from the list for this building.
        /// Should be called as base after district has been updated by child class.
        /// </summary>
        private void RemoveVehicle()
        {
            // Remove selected vehicle from building.
            VehicleControl.RemoveVehicle(CurrentBuilding, TransferReason, _selectedVehiclePanel.SelectedVehicle);

            // Clear current selection.
            _selectedVehiclePanel.SelectedVehicle = null;

            // Update vehicle lists.
            _selectedVehiclePanel.RefreshList();
            _vehicleSelectionPanel.RefreshList();
        }
    }
}