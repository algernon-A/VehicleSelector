// <copyright file="SelectedVehiclePanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace VehicleSelector
{
    using System.Collections.Generic;
    using System.Linq;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Selected vehicle panel.
    /// </summary>
    internal class SelectedVehiclePanel : VehicleSelectionPanel
    {
        // Panel to display when no item is selected.
        private readonly UIPanel _randomPanel;
        private readonly UILabel _randomLabel;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectedVehiclePanel"/> class.
        /// </summary>
        internal SelectedVehiclePanel()
        {
            // Panel setup.
            _randomPanel = VehicleList.AddUIComponent<UIPanel>();
            _randomPanel.width = VehicleList.width;
            _randomPanel.height = VehicleList.height;
            _randomPanel.relativePosition = new Vector2(0f, 0f);

            // Random sprite.
            UISprite randomSprite = _randomPanel.AddUIComponent<UISprite>();
            randomSprite.atlas = UITextures.InGameAtlas;
            randomSprite.spriteName = "Random";

            // Label.
            _randomLabel = UILabels.AddLabel(_randomPanel, 0f, 0f, Translations.Translate("ANY_VEHICLE"), VehicleList.width, 0.8f);

            // Size is 56x33, so offset -8 from left and 3.5 from top to match normal row sizing.
            randomSprite.size = new Vector2(56f, 33f);
            randomSprite.relativePosition = new Vector2(-8, (40f - randomSprite.height) / 2f);
            _randomLabel.relativePosition = new Vector2(48f, (randomSprite.height - _randomLabel.height) / 2f);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the Transport Lines Manager mod is active.
        /// </summary>
        internal static bool TLMActive { get; set; } = false;

        /// <summary>
        /// Sets the currently selected vehicle.
        /// </summary>
        protected override VehicleInfo SelectedVehicle { set => ParentPanel.SelectedBuildingVehicle = value; }

        /// <summary>
        /// Populates the list.
        /// </summary>
        protected override void PopulateList()
        {
            List<VehicleItem> items = new List<VehicleItem>();
            List<VehicleInfo> buildingVehicles = VehicleControl.GetVehicles(ParentPanel.CurrentBuilding, ParentPanel.TransferReason);
            if (buildingVehicles != null)
            {
                foreach (VehicleInfo vehicle in buildingVehicles)
                {
                    items.Add(new VehicleItem(vehicle));
                }
            }

            // If list is empty, show random item panel (and hide otherwise).
            if (items.Count == 0)
            {
                _randomPanel.Show();

                // Check for TLM override.
                _randomLabel.text = Translations.Translate(TLMActive && Singleton<BuildingManager>.instance.m_buildings.m_buffer[ParentPanel.ParentPanel.CurrentBuilding].Info.m_buildingAI is TransportStationAI ? "TLM_VEHICLE" : "ANY_VEHICLE");
            }
            else
            {
                _randomPanel.Hide();
            }

            // Set display list items, without changing the display.
            VehicleList.Data = new FastList<object>
            {
                m_buffer = items.OrderBy(x => x.Name).ToArray(),
                m_size = items.Count,
            };
        }
    }
}