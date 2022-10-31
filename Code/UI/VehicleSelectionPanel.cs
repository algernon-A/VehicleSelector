// <copyright file="VehicleSelectionPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace VehicleSelector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AlgernonCommons;
    using AlgernonCommons.UI;
    using ColossalFramework;
    using ColossalFramework.UI;

    /// <summary>
    /// Vehicle selection panel main class.
    /// </summary>
    internal class VehicleSelectionPanel : UIPanel
    {
        /// <summary>
        /// Layout margin.
        /// </summary>
        protected const float Margin = 5f;

        // Vehicle selection list.
        private readonly UIList _vehicleList;

        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleSelectionPanel"/> class.
        /// </summary>
        internal VehicleSelectionPanel()
        {
            try
            {
                // Basic setup.
                name = "VehicleSelectionPanel";
                autoLayout = false;
                isVisible = true;
                canFocus = true;
                isInteractive = true;
                width = VehicleSelection.ListWidth;
                height = VehicleSelection.VehicleListHeight;

                // Vehicle selection list.
                _vehicleList = UIList.AddUIList<VehicleSelectionRow>(
                    this,
                    0f,
                    0f,
                    VehicleSelection.ListWidth,
                    VehicleSelection.VehicleListHeight,
                    VehicleSelectionRow.VehicleRowHeight);
                _vehicleList.EventSelectionChanged += (c, selectedItem) => SelectedVehicle = (selectedItem as VehicleItem)?.Info;
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up vehicle selection panel");
            }
        }

        /// <summary>
        /// Gets or sets the parent reference.
        /// </summary>
        internal VehicleSelection ParentPanel { get; set; }

        /// <summary>
        /// Gets the vehicle selection list.
        /// </summary>
        internal UIList VehicleList => _vehicleList;

        /// <summary>
        /// Sets the currently selected vehicle.
        /// </summary>
        protected virtual VehicleInfo SelectedVehicle { set => ParentPanel.SelectedListVehicle = value; }

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        internal void ClearSelection() => _vehicleList.SelectedIndex = -1;

        /// <summary>
        /// Refreshes the list with current information.
        /// </summary>
        internal void RefreshList()
        {
            // Clear selected index.
            _vehicleList.SelectedIndex = -1;

            // Repopulate the list.
            PopulateList();
        }

        /// <summary>
        /// Populates the list with available vehicles.
        /// </summary>
        protected virtual void PopulateList()
        {
            // Ensure valid building selection.
            ushort currentBuilding = ParentPanel.CurrentBuilding;
            if (currentBuilding == 0)
            {
                return;
            }

            // Local references.
            Building[] buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
            List<VehicleItem> items = new List<VehicleItem>();

            // Determine effective building class for vehicle matching.
            VehicleControl.GetEffectiveClass(currentBuilding, buildingBuffer, ParentPanel.TransferReason, out ItemClass.Service buildingService, out ItemClass.SubService buildingSubService, out ItemClass.Level buildingLevel);

            // Get list of already-selected vehicles.
            List<VehicleInfo> selectedList = VehicleControl.GetVehicles(currentBuilding, ParentPanel.TransferReason);

            // Iterate through all loaded vehicles.
            for (uint i = 0; i < PrefabCollection<VehicleInfo>.LoadedCount(); ++i)
            {
                if (PrefabCollection<VehicleInfo>.GetLoaded(i) is VehicleInfo vehicle)
                {
                    // Looking for service, sub-service and level match.
                    // Level match is ignored if the service is PlayerIndustry, to let general Industries DLC cargo vehicles (level 0) also transfer luxury products (level 1),
                    // Level match is also ignored for zoned industry due to builing level-up.
                    // Ignore any trailer vehicles.
                    // Ignore any procedural vehicles (e.g. fire helicopter buckets).
                    if (vehicle.m_class.m_service == buildingService &&
                        vehicle.m_class.m_subService == buildingSubService &&
                        (vehicle.m_class.m_level == buildingLevel || buildingService == ItemClass.Service.Industrial || buildingService == ItemClass.Service.PlayerIndustry) &&
                        !(vehicle.m_vehicleAI is CarTrailerAI) &&
                        !(vehicle.m_placementStyle == ItemClass.Placement.Procedural) &&
                        (selectedList == null || !selectedList.Contains(vehicle)))
                    {
                        // Special check for fishing boats, to stop fishing boats being included in fish truck lists (and vice-versa).
                        if (buildingService == ItemClass.Service.Fishing)
                        {
                            bool isFishingBoat = vehicle.m_vehicleAI is FishingBoatAI;
                            bool isFishingBoatService = ParentPanel.TransferReason == TransferManager.TransferReason.None;
                            if (isFishingBoat != isFishingBoatService)
                            {
                                continue;
                            }
                        }

                        // Check vehicle type, if applicable.
                        if (buildingBuffer[currentBuilding].Info.m_buildingAI is PlayerBuildingAI playerBuildingAI)
                        {
                            VehicleInfo.VehicleType vehicleType = playerBuildingAI.GetVehicleType();

                            // Additional check for passenger planes.
                            if (buildingSubService == ItemClass.SubService.PublicTransportPlane)
                            {
                                vehicleType = VehicleInfo.VehicleType.Plane;
                            }

                            if (vehicleType != VehicleInfo.VehicleType.None && vehicleType != vehicle.m_vehicleType)
                            {
                                continue;
                            }
                        }

                        // All filters passed - add to available list.
                        items.Add(new VehicleItem(vehicle));
                    }
                }
            }

            // Set display list items, without changing the display.
            _vehicleList.Data = new FastList<object>
            {
                m_buffer = items.OrderBy(x => x.Name).ToArray(),
                m_size = items.Count,
            };
        }
    }
}