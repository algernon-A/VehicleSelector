// <copyright file="BuildingPanelManager.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace VehicleSelector
{
    using System;
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Static class to manage the mod's building info panel.
    /// </summary>
    internal static class BuildingPanelManager
    {
        // Instance references.
        private static GameObject s_gameObject;
        private static BuildingPanel s_panel;

        // InfoPanel buttons.
        private static UIButton s_privateBuildingButton;
        private static UIButton s_playerBuildingButton;

        /// <summary>
        /// Gets the active panel instance.
        /// </summary>
        internal static BuildingPanel Panel => s_panel;

        /// <summary>
        /// Creates the panel object in-game and displays it.
        /// </summary>
        /// <typeparam name="TPanel">Panel type.</typeparam>
        internal static void Create<TPanel>()
            where TPanel : BuildingPanel
        {
            try
            {
                // If no instance already set, create one.
                if (s_gameObject == null)
                {
                    // Give it a unique name for easy finding with ModTools.
                    s_gameObject = new GameObject("TCBuildingInfoPanel");
                    s_gameObject.transform.parent = UIView.GetAView().transform;

                    // Add panel and set parent transform.
                    s_panel = s_gameObject.AddComponent<TPanel>();

                    // Show panel.
                    Panel.Show();
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception creating TCBuildingInfoPanel");
            }
        }

        /// <summary>
        /// Closes the panel by destroying the object (removing any ongoing UI overhead).
        /// </summary>
        internal static void Close()
        {
            if (s_panel != null)
            {
                GameObject.Destroy(s_panel);
                GameObject.Destroy(s_gameObject);

                s_panel = null;
                s_gameObject = null;
            }
        }

        /// <summary>
        /// Sets the target to the selected building, creating the panel if necessary.
        /// </summary>
        /// <param name="buildingID">New building ID.</param>
        internal static void SetTarget(ushort buildingID)
        {
            // If no existing panel, create it.
            if (Panel == null)
            {
                Create<BuildingPanel>();
            }

            // Set the target.
            Panel.SetTarget(buildingID);
        }

        /// <summary>
        /// Adds the building buttons to game building info panels.
        /// </summary>
        internal static void AddInfoPanelButtons()
        {
            try
            {
                s_privateBuildingButton = AddInfoPanelButton(UIView.library.Get<ZonedBuildingWorldInfoPanel>(typeof(ZonedBuildingWorldInfoPanel).Name));
                s_playerBuildingButton = AddInfoPanelButton(UIView.library.Get<CityServiceWorldInfoPanel>(typeof(CityServiceWorldInfoPanel).Name));
                AddInfoPanelButton(UIView.library.Get<WarehouseWorldInfoPanel>(typeof(WarehouseWorldInfoPanel).Name));
                AddInfoPanelButton(UIView.library.Get<UniqueFactoryWorldInfoPanel>(typeof(UniqueFactoryWorldInfoPanel).Name));
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception adding building info panel buttons");
            }
        }

        /// <summary>
        /// Handles building info world target building changes.
        /// </summary>
        internal static void TargetChanged()
        {
            // Don't do anything if panel isn't open.
            if (s_panel == null)
            {
                return;
            }

            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            bool supportedBuilding = Transfers.BuildingEligibility(buildingID);
            s_privateBuildingButton.isVisible = supportedBuilding;
            s_playerBuildingButton.isVisible = supportedBuilding;

            if (supportedBuilding)
            {
                SetTarget(buildingID);
            }
            else
            {
                Close();
            }
        }

        /// <summary>
        /// Adds a Transfer Controller button to a building info panel to directly access that building's.
        /// </summary>
        /// <param name="infoPanel">Infopanel to apply the button to.</param>
        /// <returns>New UIButton.</returns>
        private static UIButton AddInfoPanelButton(BuildingWorldInfoPanel infoPanel)
        {
            const float ButtonHeight = 42f;
            const float ButtonWidth = 42f;

            // Targets.
            UIComponent parent = null;
            float relativeX = 0f;
            float relativeY = 0f;

            Logging.Message("adding info panel button  to ", infoPanel.name);

            // Player info panels have wrappers, warehouse and zoned ones don't.
            UIComponent wrapper = infoPanel.Find("Wrapper");
            if (wrapper == null)
            {
                if (infoPanel.Find("ActionPanel") is UIPanel actionPanel)
                {
                    Logging.Message("adding info panel button to warehouse");

                    // Warehouse.
                    relativeX = 47f;
                    parent = actionPanel;
                }
                else if (infoPanel.Find("Misc") is UIPanel miscPanel)
                {
                    Logging.Message("adding info panel button to unique factories");

                    // Unique factory.
                    relativeX = 18f;
                    parent = miscPanel;
                }
                else if (infoPanel.Find("MakeHistoricalPanel") is UIPanel makeHistoricalPanel)
                {
                    // Zoned building.
                    Logging.Message("adding info panel button to zoned building");
                    relativeX = 10f;
                    relativeY = makeHistoricalPanel.relativePosition.y - 47f;
                    parent = infoPanel.component;
                }
            }
            else
            {
                // City service panel.
                relativeX = 94f;
                parent = wrapper.Find("MainSectionPanel")?.Find("MainBottom")?.Find("ButtonPanels")?.Find("ActionButtons")?.Find("ActionPanelPanel")?.Find("ActionPanel");
            }

            if (parent == null)
            {
                Logging.Error("couldn't place panel button for ", infoPanel.name);
                return null;
            }

            UIButton panelButton = parent.AddUIComponent<UIButton>();

            // Basic button setup.
            panelButton.atlas = UITextures.InGameAtlas;
            panelButton.height = ButtonHeight;
            panelButton.width = ButtonWidth;
            panelButton.normalFgSprite = "IconServiceVehicle";
            panelButton.focusedFgSprite = "IconServiceVehicle";
            panelButton.hoveredFgSprite = "IconServiceVehicle";
            panelButton.pressedFgSprite = "IconServiceVehicle";
            panelButton.disabledFgSprite = "IconServiceVehicle";
            panelButton.normalBgSprite = "GenericPanelLight";
            panelButton.focusedBgSprite = "GenericPanelLight";
            panelButton.hoveredBgSprite = "GenericPanelWhite";
            panelButton.pressedBgSprite = "GenericPanelLight";
            panelButton.disabledBgSprite = "ButtonMenuDisabled";
            panelButton.color = new Color32(219, 219, 219, 255);
            panelButton.focusedColor = Color.white;
            panelButton.hoveredColor = Color.white;
            panelButton.disabledColor = new Color32(93, 84, 84, 255);
            panelButton.name = "VehicleSelectorButton";
            panelButton.tooltip = Translations.Translate("MOD_NAME");

            // Set position.
            panelButton.relativePosition = new Vector2(relativeX, relativeY);

            // Event handler.
            panelButton.eventClick += (c, p) =>
            {
                // Select current building in the building details panel and show.
                SetTarget(WorldInfoPanel.GetCurrentInstanceID().Building);

                // Manually unfocus control, otherwise it can stay focused until next UI event (looks untidy).
                c.Unfocus();
            };

            return panelButton;
        }
    }
}