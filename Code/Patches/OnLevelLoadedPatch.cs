// <copyright file="OnLevelLoadedPatch.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace VehicleSelector
{
    using ColossalFramework.UI;
    using HarmonyLib;
    using UnityEngine;

    /// <summary>
    /// Harmony Postfix patch for OnLevelLoaded.  This enables us to perform setup tasks after all loading has been completed.
    /// </summary>
    [HarmonyPatch(typeof(LoadingWrapper))]
    [HarmonyPatch("OnLevelLoaded")]
    public static class OnLevelLoadedPatch
    {
        /// <summary>
        /// Harmony postfix to perform actions require after the level has loaded.
        /// </summary>
        public static void Postfix()
        {
            // Move Customize It Extended buttons.
            UIButton cieButton = UIView.library.Get<CityServiceWorldInfoPanel>(typeof(CityServiceWorldInfoPanel).Name).Find<UIButton>("CustomizeItExtendedButton");
            if (cieButton != null)
            {
                cieButton.relativePosition += new Vector3(40f, 0f, 0f);
            }

            cieButton = UIView.library.Get<WarehouseWorldInfoPanel>(typeof(WarehouseWorldInfoPanel).Name).Find<UIButton>("CustomizeItExtendedButton");
            if (cieButton != null)
            {
                cieButton.relativePosition += new Vector3(40f, 0f, 0f);
            }
        }
    }
}