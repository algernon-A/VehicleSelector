// <copyright file="CityServiceWorldInfoPanelPatch.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace VehicleSelector
{
    using ColossalFramework.UI;
    using HarmonyLib;

    /// <summary>
    /// Harmony patch disable the vanilla vehicle selector.
    /// </summary>
    [HarmonyPatch(typeof(CityServiceWorldInfoPanel), "UpdateBindings")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony")]
    public static class CityServiceWorldInfoPanelPatch
    {
        /// <summary>
        /// Harmony Postfix patch to CityServiceWorldInfoPanel.UpdateBindings to disable the vanilla vehicle selector.
        /// </summary>
        /// <param name="___m_VehicleSelector">Panel m_VehicleSelector private field.</param>
        public static void Postfix(ServiceBuildingVehicleSelector ___m_VehicleSelector)
        {
            if (___m_VehicleSelector != null)
            {
                ___m_VehicleSelector.Find<UIButton>("VehicleSelector").enabled = false;
            }
        }
    }
}