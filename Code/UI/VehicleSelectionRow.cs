﻿// <copyright file="VehicleSelectionRow.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace VehicleSelector
{
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// UIList row item for vehicle prefabs.
    /// </summary>
    public class VehicleSelectionRow : UIListRow
    {
        /// <summary>
        /// Row height.
        /// </summary>
        public const float VehicleRowHeight = 40f;

        // Layout constants - private.
        private const float VehicleSpriteSize = 40f;

        // Vehicle name label.
        private UILabel vehicleNameLabel;

        // Preview image.
        private UISprite vehicleSprite;

        /// <summary>
        /// Vehicle prefab.
        /// </summary>
        private VehicleInfo _info;

        /// <summary>
        /// Gets the height for this row.
        /// </summary>
        public override float RowHeight => VehicleRowHeight;

        /// <summary>
        /// Generates and displays a row.
        /// </summary>
        /// <param name="data">Object data to display.</param>
        /// <param name="rowIndex">Row index number (for background banding).</param>
        public override void Display(object data, int rowIndex)
        {
            // Perform initial setup for new rows.
            if (vehicleNameLabel == null)
            {
                // Add object name label.
                vehicleNameLabel = AddLabel(VehicleSpriteSize + Margin, width - Margin - VehicleSpriteSize - Margin, wordWrap: true);

                // Add preview sprite image.
                vehicleSprite = AddUIComponent<UISprite>();
                vehicleSprite.height = VehicleSpriteSize;
                vehicleSprite.width = VehicleSpriteSize;
                vehicleSprite.relativePosition = Vector2.zero;
            }

            // Get building ID and set name label.
            if (data is VehicleItem thisItem)
            {
                _info = thisItem.Info;
                vehicleNameLabel.text = thisItem.Name;

                vehicleSprite.atlas = _info?.m_Atlas;
                vehicleSprite.spriteName = _info?.m_Thumbnail;
            }
            else
            {
                // Just in case (no valid vehicle record).
                vehicleNameLabel.text = string.Empty;
            }

            // Set initial background as deselected state.
            Deselect(rowIndex);
        }
    }
}