// <copyright file="TransferDataUtils.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace VehicleSelector
{
    using AlgernonCommons.Translation;
    using ColossalFramework;

    /// <summary>
    /// Transfer data utilities.
    /// </summary>
    internal static class TransferDataUtils
    {
        /// <summary>
        /// Checks if the given building has supported transfer types.
        /// </summary>
        /// <param name="buildingID">ID of building to check.</param>
        /// <param name="transfers">Transfer structure array to populate (size 4).</param>
        /// <returns>True if any transfers are supported for this building, false if none.</returns>
        internal static bool BuildingEligibility(ushort buildingID, TransferStruct[] transfers) => BuildingEligibility(buildingID, Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info, transfers) > 0;

        /// <summary>
        /// Determines the eligible transfers (if any) for the given building.
        /// Thanks to t1a2l for doing a bunch of these.
        /// </summary>
        /// <param name="buildingID">ID of building to check.</param>
        /// <param name="buildingInfo">BuildingInfo record of building.</param>
        /// <param name="transfers">Transfer structure array to populate (size 4).</param>
        /// <returns>Number of eligible transfers.</returns>
        internal static int BuildingEligibility(ushort buildingID, BuildingInfo buildingInfo, TransferStruct[] transfers)
        {
            switch (buildingInfo.GetService())
            {
                // Healthcare.
                case ItemClass.Service.HealthCare:
                    if (buildingInfo.m_buildingAI is HospitalAI)
                    {
                        transfers[0].Reason = TransferManager.TransferReason.Sick;
                    }
                    else if (buildingInfo.m_buildingAI is HelicopterDepotAI)
                    {
                        transfers[0].Reason = TransferManager.TransferReason.Sick2;
                    }
                    else if (buildingInfo.m_buildingAI is CemeteryAI cemeteryAI)
                    {
                        // Deathcare.
                        transfers[0].Reason = TransferManager.TransferReason.Dead;
                        transfers[0].PanelTitle = Translations.Translate("TFC_GEN_SER");
                        transfers[0].IsIncoming = true;

                        // Outgoing transfers - cemetaries only.
                        if (cemeteryAI.m_graveCount > 0)
                        {
                            transfers[1].Reason = TransferManager.TransferReason.DeadMove;
                            transfers[1].PanelTitle = Translations.Translate("TFC_TFR_OUT");
                            transfers[1].IsIncoming = false;

                            return 2;
                        }

                        return 1;
                    }
                    else
                    {
                        // Any other healthcare buildings (e.g. SaunaAI) aren't supported.
                        return 0;
                    }

                    transfers[0].PanelTitle = Translations.Translate("TFC_GEN_SER");
                    transfers[0].IsIncoming = true;

                    return 1;

                // Fire.
                case ItemClass.Service.FireDepartment:
                    transfers[0].PanelTitle = Translations.Translate("TFC_FIR_SER");
                    transfers[0].Reason = buildingInfo.m_buildingAI is HelicopterDepotAI ? TransferManager.TransferReason.Fire2 : TransferManager.TransferReason.Fire;
                    transfers[0].IsIncoming = true;
                    return 1;

                case ItemClass.Service.Water:
                    // Water pumping.
                    if (buildingInfo.m_buildingAI is WaterFacilityAI waterFacilityAI && buildingInfo.m_class.m_level == ItemClass.Level.Level1 && waterFacilityAI.m_pumpingVehicles > 0)
                    {
                        transfers[0].PanelTitle = Translations.Translate("TFC_GEN_SER");
                        transfers[0].IsIncoming = true;
                        transfers[0].Reason = TransferManager.TransferReason.FloodWater;
                        return 1;
                    }

                    return 0;

                case ItemClass.Service.Disaster:
                    // Disaster response - trucks and helicopters.
                    if (buildingInfo.m_buildingAI is DisasterResponseBuildingAI)
                    {
                        transfers[0].PanelTitle = Translations.Translate("TFC_DIS_TRU");
                        transfers[0].IsIncoming = true;
                        transfers[0].Reason = TransferManager.TransferReason.Collapsed;
                        transfers[1].PanelTitle = Translations.Translate("TFC_DIS_HEL");
                        transfers[1].IsIncoming = true;
                        transfers[1].Reason = TransferManager.TransferReason.Collapsed2;
                        return 2;
                    }

                    return 0;

                case ItemClass.Service.PoliceDepartment:
                    Building.Flags buildingFlags = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].m_flags;

                    // Police helicopter depot.
                    if (buildingInfo.m_buildingAI is HelicopterDepotAI)
                    {
                        transfers[0].PanelTitle = Translations.Translate("TFC_GEN_SER");
                        transfers[0].IsIncoming = true;
                        transfers[0].Reason = TransferManager.TransferReason.Crime;

                        // Prison Helicopter Mod.
                        if ((buildingFlags & Building.Flags.Downgrading) != 0)
                        {
                            transfers[1].PanelTitle = Translations.Translate("TFC_POL_PHI");
                            transfers[1].IsIncoming = true;
                            transfers[1].Reason = (TransferManager.TransferReason)121;
                            return 2;
                        }

                        return 1;
                    }
                    else
                    {
                        // Prisons.
                        if (buildingInfo.m_class.m_level >= ItemClass.Level.Level4)
                        {
                            transfers[0].PanelTitle = Translations.Translate("TFC_POL_PMI");
                            transfers[0].IsIncoming = true;
                            transfers[0].Reason = TransferManager.TransferReason.CriminalMove;

                            return 1;
                        }
                        else
                        {
                            // Normal police station.
                            // Police service.
                            transfers[0].PanelTitle = Translations.Translate("TFC_GEN_SER");
                            transfers[0].IsIncoming = true;
                            transfers[0].Reason = TransferManager.TransferReason.Crime;

                            // Prison Helicopter Mod.
                            if (buildingInfo.m_buildingAI.GetType().Name.Equals("PrisonCopterPoliceStationAI"))
                            {
                                // Big (central) police station.
                                if ((buildingFlags & Building.Flags.Downgrading) != 0)
                                {
                                    // Collect prisoners from smaller stations by sending a prison van.
                                    transfers[1].PanelTitle = Translations.Translate("TFC_POL_PMI");
                                    transfers[1].IsIncoming = true;
                                    transfers[1].Reason = (TransferManager.TransferReason)120;
                                    return 2;
                                }
                            }

                            return 1;
                        }
                    }

                case ItemClass.Service.Industrial:
                    transfers[0].PanelTitle = Translations.Translate("TFC_GEN_SEL");
                    transfers[0].IsIncoming = false;
                    transfers[0].Reason = TransferManager.TransferReason.None;
                    return 1;

                case ItemClass.Service.PlayerIndustry:
                    // Industries DLC.
                    if (buildingInfo.m_buildingAI is ExtractingFacilityAI extractingAI)
                    {
                        // Extractors.
                        transfers[0].PanelTitle = Translations.Translate("TFC_GEN_SEL");
                        transfers[0].IsIncoming = false;
                        transfers[0].Reason = extractingAI.m_outputResource;
                        return 1;
                    }
                    else if (buildingInfo.m_buildingAI is ProcessingFacilityAI processingAI && buildingInfo.m_class.m_level < ItemClass.Level.Level5)
                    {
                        // Processors.
                        transfers[0].PanelTitle = Translations.Translate("TFC_GEN_SEL");
                        transfers[0].IsIncoming = false;
                        transfers[0].Reason = processingAI.m_outputResource;
                        return 1;
                    }
                    else if (buildingInfo.m_buildingAI is UniqueFactoryAI)
                    {
                        // Unique factories.
                        transfers[0].PanelTitle = Translations.Translate("TFC_GEN_SEL");
                        transfers[0].IsIncoming = false;
                        transfers[0].Reason = TransferManager.TransferReason.LuxuryProducts;
                        return 1;
                    }
                    else if (buildingInfo.m_buildingAI is WarehouseAI)
                    {
                        // Warehouses.
                        transfers[0].PanelTitle = Translations.Translate("TFC_GEN_SEL");
                        transfers[0].IsIncoming = false;
                        transfers[0].Reason = TransferManager.TransferReason.None;
                        return 1;
                    }

                    return 0;

                case ItemClass.Service.Road:
                case ItemClass.Service.Beautification:
                    // Maintenance depots and snow dumps only, and only incoming.
                    if (buildingInfo.m_buildingAI is MaintenanceDepotAI || buildingInfo.m_buildingAI is SnowDumpAI)
                    {
                        transfers[0].PanelTitle = Translations.Translate("TFC_GEN_SER");
                        transfers[0].IsIncoming = true;
                        transfers[0].Reason = TransferManager.TransferReason.None;
                        return 1;
                    }

                    return 0;

                case ItemClass.Service.PublicTransport:
                    if (buildingInfo.m_buildingAI is PostOfficeAI postOfficeAI)
                    {
                        // Post office vs. mail sorting facility - post offices have vans.
                        if (postOfficeAI.m_postVanCount > 0)
                        {
                            // Post office.
                            transfers[0].PanelTitle = Translations.Translate("TFC_MAI_IML");
                            transfers[0].IsIncoming = true;
                            transfers[0].Reason = TransferManager.TransferReason.Mail;

                            // Post offices send unsorted mail via their trucks.
                            transfers[1].PanelTitle = Translations.Translate("TFC_MAI_OUN");
                            transfers[1].IsIncoming = false;
                            transfers[1].Reason = TransferManager.TransferReason.UnsortedMail;

                            // Post offices pick up sorted mail via their trucks.
                            transfers[2].PanelTitle = Translations.Translate("TFC_MAI_IST");
                            transfers[2].IsIncoming = true;
                            transfers[2].Reason = TransferManager.TransferReason.SortedMail;

                            return 3;
                        }

                        // Mail sorting facility.
                        transfers[0].PanelTitle = Translations.Translate("TFC_MAI_OST");
                        transfers[0].IsIncoming = false;
                        transfers[0].Reason = TransferManager.TransferReason.SortedMail;

                        transfers[1].PanelTitle = Translations.Translate("TFC_MAI_OGM");
                        transfers[1].IsIncoming = false;
                        transfers[1].Reason = TransferManager.TransferReason.OutgoingMail;
                        return 2;
                    }
                    else if (buildingInfo.m_class.m_subService == ItemClass.SubService.PublicTransportTaxi)
                    {
                        // Taxi depots.
                        transfers[0].PanelTitle = Translations.Translate("TFC_GEN_SER");
                        transfers[0].IsIncoming = false;
                        transfers[0].Reason = TransferManager.TransferReason.Taxi;
                        return 1;
                    }

                    // Unsupported public transport type.
                    return 0;

                case ItemClass.Service.Garbage:
                    if (buildingInfo.m_buildingAI is LandfillSiteAI landfillAI)
                    {
                        // Incineration Plant.
                        if (buildingInfo.GetClassLevel() == ItemClass.Level.Level1 && landfillAI.m_electricityProduction != 0)
                        {
                            // Garbage Collection.
                            transfers[0].PanelTitle = Translations.Translate("TFC_GAR_ICO");
                            transfers[0].IsIncoming = true;
                            transfers[0].Reason = TransferManager.TransferReason.Garbage;

                            return 1;
                        }

                        // Recycling Center.
                        else if (buildingInfo.GetClassLevel() == ItemClass.Level.Level2 && landfillAI.m_materialProduction != 0)
                        {
                            // Garbage Collection.
                            transfers[0].PanelTitle = Translations.Translate("TFC_GAR_ICO");
                            transfers[0].IsIncoming = true;
                            transfers[0].Reason = TransferManager.TransferReason.Garbage;

                            return 1;
                        }

                        // Landfill Site.
                        else if (buildingInfo.GetClassLevel() == ItemClass.Level.Level1 && landfillAI.m_electricityProduction == 0)
                        {
                            // Garbage collection.
                            transfers[0].PanelTitle = Translations.Translate("TFC_GAR_ICO");
                            transfers[0].IsIncoming = true;
                            transfers[0].Reason = TransferManager.TransferReason.Garbage;

                            // Garbage move (emptying landfills) out.
                            transfers[1].PanelTitle = Translations.Translate("TFC_TFR_OUT");
                            transfers[1].IsIncoming = false;
                            transfers[1].Reason = TransferManager.TransferReason.GarbageMove;

                            return 2;
                        }

                        // Waste Transfer Facility.
                        else if (buildingInfo.GetClassLevel() == ItemClass.Level.Level3 && landfillAI.m_electricityProduction == 0)
                        {
                            // Garbage collection.
                            transfers[0].PanelTitle = Translations.Translate("TFC_GAR_ICO");
                            transfers[0].IsIncoming = true;
                            transfers[0].Reason = TransferManager.TransferReason.Garbage;

                            return 1;
                        }

                        // Waste Processing Complex.
                        else if (buildingInfo.GetClassLevel() == ItemClass.Level.Level4)
                        {
                            // Garbage Transfer for proccessing from Waste Transfer Facility and Landfill Site.
                            transfers[0].PanelTitle = Translations.Translate("TFC_GAR_ITF");
                            transfers[0].IsIncoming = true;
                            transfers[0].Reason = TransferManager.TransferReason.GarbageTransfer;

                            return 1;
                        }
                    }

                    // Undefined service.
                    return 0;

                default:
                    // If not explicitly supported, then it's not supported.
                    return 0;
            }
        }

        /// <summary>
        /// Struct to hold basic transfer information.
        /// </summary>
        public struct TransferStruct
        {
            /// <summary>
            /// Title text to display for this transfer.
            /// </summary>
            public string PanelTitle;

            /// <summary>
            /// Whether or not this transfer is incoming (true) or outgoing (false).
            /// </summary>
            public bool IsIncoming;

            /// <summary>
            /// Transfer reason.
            /// </summary>
            public TransferManager.TransferReason Reason;
        }
    }
}
