﻿// <copyright file="BuildingPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace VehicleSelector
{
    using System;
    using System.Text;
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework;
    using ColossalFramework.UI;
    using UnityEngine;
    using static Transfers;

    /// <summary>
    /// Building info panel.
    /// </summary>
    internal class BuildingPanel : UIPanel
    {
        /// <summary>
        /// Layout margin.
        /// </summary>
        protected const float Margin = 5f;

        // Layout constants - private.
        private const float TitleHeight = 40f;
        private const float NameLabelY = TitleHeight + Margin;
        private const float NameLabelHeight = 30f;
        private const float AreaLabelHeight = 20f;
        private const float AreaLabel1Y = TitleHeight + NameLabelHeight;
        private const float AreaLabel2Y = AreaLabel1Y + AreaLabelHeight;
        private const float ListY = AreaLabel2Y + AreaLabelHeight + Margin;
        private const float VehicleSelectionHeight = VehicleSelection.PanelHeight + Margin;
        private const float NoPanelHeight = ListY + Margin;
        private const float IconButtonSize = 40f;
        private const float IconButtonY = ListY - IconButtonSize - Margin;
        private const float PasteButtonX = PanelWidth - IconButtonSize - Margin;
        private const float CopyButtonX = PasteButtonX - IconButtonSize - Margin;
        private const float CopyBuildingButtonX = CopyButtonX - IconButtonSize - Margin;
        private const float CopyDistrictButtonX = CopyBuildingButtonX - IconButtonSize - Margin;
        private const float PanelWidth = VehicleSelection.PanelWidth + Margin + Margin;

        // Panel components.
        private UILabel _buildingLabel;
        private UILabel _areaLabel1;
        private UILabel _areaLabel2;
        private UIButton _copyButton;
        private UIButton _pasteButton;
        private UIButton _copyBuildingButton;
        private UIButton _copyDistrictButton;

        // Sub-panels.
        private TransferStruct[] _transfers = new TransferStruct[MaxTransfers];
        private VehicleSelection[] _vehicleSelections = new VehicleSelection[MaxTransfers];

        // Status flag.
        private bool _panelReady = false;

        // Current selections.
        private ushort _currentBuilding;
        private int _numSelections;
        private BuildingInfo _thisBuildingInfo;
        private byte _currentDistrict;
        private byte _currentPark;

        // Event handling.
        private bool _copyProcessing = false;
        private bool _pasteProcessing = false;

        /// <summary>
        /// Gets the current building ID.
        /// </summary>
        internal ushort CurrentBuilding => _currentBuilding;

        /// <summary>
        /// Gets or sets a value indicating whether this is an incoming (true) or outgoing (false) transfer.
        /// </summary>
        internal bool IsIncoming { get; set; }

        /// <summary>
        /// Gets or sets the current transfer reason.
        /// </summary>
        internal TransferManager.TransferReason TransferReason { get; set; }

        /// <summary>
        /// Called by Unity when the object is created.
        /// Used to perform setup.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            try
            {
                // Basic setup.
                autoLayout = false;
                backgroundSprite = "UnlockingPanel2";
                isVisible = true;
                canFocus = true;
                isInteractive = true;
                width = PanelWidth;
                height = NoPanelHeight;

                // Default position - centre in screen.
                relativePosition = new Vector2(Mathf.Floor((GetUIView().fixedWidth - PanelWidth) / 2), (GetUIView().fixedHeight - NoPanelHeight) / 2);

                // Title label.
                UILabel titleLabel = UILabels.AddLabel(this, 0f, 10f, Translations.Translate("MOD_NAME"), PanelWidth, 1.2f);
                titleLabel.textAlignment = UIHorizontalAlignment.Center;

                // Building label.
                _buildingLabel = UILabels.AddLabel(this, 0f, NameLabelY, string.Empty, PanelWidth);
                _buildingLabel.textAlignment = UIHorizontalAlignment.Center;

                // Drag handle.
                UIDragHandle dragHandle = this.AddUIComponent<UIDragHandle>();
                dragHandle.relativePosition = Vector3.zero;
                dragHandle.width = PanelWidth - 35f;
                dragHandle.height = TitleHeight;

                // Close button.
                UIButton closeButton = AddUIComponent<UIButton>();
                closeButton.relativePosition = new Vector2(width - 35f, 2f);
                closeButton.normalBgSprite = "buttonclose";
                closeButton.hoveredBgSprite = "buttonclosehover";
                closeButton.pressedBgSprite = "buttonclosepressed";

                // Close button event handler.
                closeButton.eventClick += (component, clickEvent) =>
                {
                    BuildingPanelManager.Close();
                };

                // Area labels.
                _areaLabel1 = UILabels.AddLabel(this, 0f, AreaLabel1Y, string.Empty, PanelWidth, 0.9f);
                _areaLabel1.textAlignment = UIHorizontalAlignment.Center;
                _areaLabel2 = UILabels.AddLabel(this, 0f, AreaLabel2Y, string.Empty, PanelWidth, 0.9f);
                _areaLabel2.textAlignment = UIHorizontalAlignment.Center;

                // Zoom to building button.
                UIButton zoomButton = AddZoomButton(this, Margin, Margin, 30f, "ZOOM_BUILDING");
                zoomButton.eventClicked += (c, p) => ZoomToBuilding(_currentBuilding);

                // Copy/paste buttons.
                _copyButton = UIButtons.AddIconButton(this, CopyButtonX, IconButtonY, IconButtonSize, UITextures.LoadQuadSpriteAtlas("VS-Copy"), Translations.Translate("COPY_TIP"));
                _copyButton.eventClicked += (c, p) => CopyPaste.Instance.Copy(_currentBuilding);
                _pasteButton = UIButtons.AddIconButton(this, PasteButtonX, IconButtonY, IconButtonSize, UITextures.LoadQuadSpriteAtlas("VS-Paste"), Translations.Translate("PASTE_TIP"));
                _pasteButton.eventClicked += (c, p) => Paste();

                // Copy to buttons.
                _copyBuildingButton = UIButtons.AddIconButton(
                    this,
                    CopyBuildingButtonX,
                    IconButtonY,
                    IconButtonSize,
                    UITextures.LoadQuadSpriteAtlas("VS-CopyBuilding"),
                    Translations.Translate("COPY_BUILDING_TIP"));
                _copyBuildingButton.eventClicked += (c, p) => CopyPaste.Instance.CopyToBuildings(_currentBuilding, 0, 0);
                _copyDistrictButton = UIButtons.AddIconButton(
                    this,
                    CopyDistrictButtonX,
                    IconButtonY,
                    IconButtonSize,
                    UITextures.LoadQuadSpriteAtlas("VS-CopyDistrict"),
                    Translations.Translate("COPY_DISTRICT_TIP"));
                _copyDistrictButton.eventClicked += (c, p) => CopyPaste.Instance.CopyToBuildings(_currentBuilding, _currentDistrict, _currentPark);

                // Add vehicle panels.
                for (int i = 0; i < MaxTransfers; ++i)
                {
                    _vehicleSelections[i] = AddUIComponent<VehicleSelection>();
                    _vehicleSelections[i].ParentPanel = this;
                    _vehicleSelections[i].relativePosition = new Vector2(Margin, ListY + (i * VehicleSelectionHeight));
                }

                // Enable events.
                _panelReady = true;
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up building panel");
            }
        }

        /// <summary>
        /// Called by Unity every update.
        /// Used to check for copy/paste keypress.
        /// </summary>
        public override void Update()
        {
            // Don't do anything if not set up yet.
            if (!_panelReady)
            {
                return;
            }

            // Copy key processing - use event flag to avoid repeated triggering.
            if (ModSettings.KeyCopy.IsPressed())
            {
                if (!_copyProcessing)
                {
                    CopyPaste.Instance.Copy(CurrentBuilding);
                    _copyProcessing = true;

                    // Update paste button state.
                    _pasteButton.isEnabled = CopyPaste.Instance.IsValidTarget(CurrentBuilding);
                }
            }
            else
            {
                // Key no longer down - resume processing of events.
                _copyProcessing = false;
            }

            // Paste key processing - use event flag to avoid repeated triggering.
            if (ModSettings.KeyPaste.IsPressed())
            {
                if (!_pasteProcessing)
                {
                    Paste();
                    _pasteProcessing = true;
                }
            }
            else
            {
                // Key no longer down - resume processing of events.
                _pasteProcessing = false;
            }

            base.Update();
        }

        /// <summary>
        /// Adds an zoom icon button.
        /// </summary>
        /// <param name="parent">Parent UIComponent.</param>
        /// <param name="xPos">Relative X position.</param>
        /// <param name="yPos">Relative Y position.</param>
        /// <param name="size">Button size.</param>
        /// <param name="tooltipKey">Tooltip translation key.</param>
        /// <returns>New UIButton.</returns>
        internal static UIButton AddZoomButton(UIComponent parent, float xPos, float yPos, float size, string tooltipKey)
        {
            UIButton newButton = parent.AddUIComponent<UIButton>();

            // Size and position.
            newButton.relativePosition = new Vector2(xPos, yPos);
            newButton.height = size;
            newButton.width = size;

            // Appearance.
            newButton.atlas = UITextures.InGameAtlas;
            newButton.normalFgSprite = "LineDetailButtonHovered";
            newButton.focusedFgSprite = "LineDetailButtonFocused";
            newButton.hoveredFgSprite = "LineDetailButton";
            newButton.disabledFgSprite = "LineDetailButtonDisabled";
            newButton.pressedFgSprite = "LineDetailButtonPressed";

            // Tooltip.
            newButton.tooltip = Translations.Translate(tooltipKey);

            return newButton;
        }

        /// <summary>
        /// Zooms to the specified building.
        /// </summary>
        /// <param name="buildingID">Target building ID.</param>
        internal static void ZoomToBuilding(ushort buildingID)
        {
            // Go to target building if available.
            if (buildingID != 0)
            {
                // Clear existing target fist to force a re-zoom-in if required.
                ToolsModifierControl.cameraController.ClearTarget();

                InstanceID instance = default;
                instance.Building = buildingID;
                ToolsModifierControl.cameraController.SetTarget(instance, Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].m_position, zoomIn: true);
            }
        }

        /// <summary>
        /// Sets/changes the currently selected building.
        /// </summary>
        /// <param name="buildingID">New building ID.</param>
        internal virtual void SetTarget(ushort buildingID)
        {
            // Local references.
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            DistrictManager districtManager = Singleton<DistrictManager>.instance;

            // Update selected building ID.
            _currentBuilding = buildingID;
            _thisBuildingInfo = buildingManager.m_buildings.m_buffer[_currentBuilding].Info;

            // Maximum number of panels.
            _numSelections = BuildingEligibility(buildingID, _thisBuildingInfo, _transfers);

            // Set up used panels.
            int i;
            for (i = 0; i < _numSelections; ++i)
            {
                _vehicleSelections[i].SetTarget(buildingID, _transfers[i].Title, _transfers[i].Reason);
                _vehicleSelections[i].Show();
            }

            // Hide remaining panels.
            while (i < MaxTransfers)
            {
                _vehicleSelections[i++].Hide();
            }

            // Set panel height.
            height = NoPanelHeight + (_numSelections * VehicleSelectionHeight);

            // Set name.
            _buildingLabel.text = buildingManager.GetBuildingName(_currentBuilding, InstanceID.Empty);

            // District text.
            StringBuilder districtText = new StringBuilder();

            // District area.
            _currentDistrict = districtManager.GetDistrict(buildingManager.m_buildings.m_buffer[_currentBuilding].m_position);
            if (_currentDistrict != 0)
            {
                districtText.Append(districtManager.GetDistrictName(_currentDistrict));
            }

            // Park area.
            _currentPark = districtManager.GetPark(buildingManager.m_buildings.m_buffer[_currentBuilding].m_position);
            if (_currentPark != 0)
            {
                // Add comma between district and park names if we have both.
                if (_currentDistrict != 0)
                {
                    districtText.Append(", ");
                }

                districtText.Append(districtManager.GetParkName(_currentPark));
            }

            // If no current district or park, then display no area message.
            if (_currentDistrict == 0 && _currentPark == 0)
            {
                _areaLabel1.text = Translations.Translate("NO_DISTRICT");
                _areaLabel2.Hide();
            }
            else
            {
                // Current district and/or park - display generated text.
                if (_currentDistrict != 0)
                {
                    // District label.
                    _areaLabel1.text = districtManager.GetDistrictName(_currentDistrict);

                    // Is there also a park area?
                    if (_currentPark != 0)
                    {
                        // Yes - set second label text and show.
                        _areaLabel2.text = districtManager.GetParkName(_currentPark);
                        _areaLabel2.Show();
                    }
                    else
                    {
                        // Just the district - hide second area label.
                        _areaLabel2.Hide();
                    }
                }
                else if (_currentPark != 0)
                {
                    // No district, but a park - set first area label text and hide the second label.
                    _areaLabel1.text = districtManager.GetParkName(_currentPark);
                    _areaLabel2.Hide();
                }
            }

            // Make sure we're fully visible on-screen.
            if (absolutePosition.y + height > Screen.height - 120)
            {
                absolutePosition = new Vector2(absolutePosition.x, Screen.height - 120 - height);
            }

            if (absolutePosition.x + width > Screen.width - 20)
            {
                absolutePosition = new Vector2(Screen.width - 20 - width, absolutePosition.y);
            }

            if (absolutePosition.y < 20f)
            {
                absolutePosition = new Vector2(absolutePosition.x, 20f);
            }

            if (absolutePosition.x < 20f)
            {
                absolutePosition = new Vector2(20f, absolutePosition.y);
            }

            // Update button states.
            _pasteButton.isEnabled = CopyPaste.Instance.IsValidTarget(CurrentBuilding);
            _copyDistrictButton.isEnabled = _currentDistrict != 0 | _currentPark != 0;

            // Make sure we're visible if we're not already.
            Show();
        }

        /// <summary>
        /// Paste data action.
        /// </summary>
        private void Paste()
        {
            // Paste data.
            CopyPaste.Instance.Paste(CurrentBuilding);

            // Update lists.
            foreach (VehicleSelection vehicleSelection in _vehicleSelections)
            {
                vehicleSelection.Refresh();
            }
        }
    }
}