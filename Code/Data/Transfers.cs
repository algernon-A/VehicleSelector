// <copyright file="Transfers.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace VehicleSelector
{
    using AlgernonCommons.Translation;
    using ColossalFramework;

    /// <summary>
    /// Transfer data management.
    /// </summary>
    internal static class Transfers
    {
        /// <summary>
        /// Maximum number of transfer types supported per building.
        /// </summary>
        internal const int MaxTransfers = 3;

        /// <summary>
        /// Checks if the given building has supported transfer types.
        /// </summary>
        /// <param name="buildingID">ID of building to check.</param>
        /// <returns>True if any transfers are supported for this building, false if none.</returns>
        internal static bool BuildingEligibility(ushort buildingID) => BuildingEligibility(buildingID, Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info, new TransferStruct[MaxTransfers]) > 0;

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
                        // Ambulances.
                        transfers[0].Reason = TransferManager.TransferReason.Sick;
                        transfers[0].Title = Translations.Translate("AMBULANCE");
                    }
                    else if (buildingInfo.m_buildingAI is HelicopterDepotAI)
                    {
                        // Medical helicopters.
                        transfers[0].Reason = TransferManager.TransferReason.Sick2;
                        transfers[0].Title = Translations.Translate("HELI_MED");
                    }
                    else if (buildingInfo.m_buildingAI is CemeteryAI cemeteryAI)
                    {
                        // Deathcare.
                        transfers[0].Reason = TransferManager.TransferReason.Dead;
                        transfers[0].Title = Translations.Translate("HEARSE");

                        // Outgoing transfers - cemetaries only.
                        if (cemeteryAI.m_graveCount > 0)
                        {
                            transfers[1].Reason = TransferManager.TransferReason.DeadMove;
                            transfers[1].Title = Translations.Translate("DEAD_TRANSFER");

                            return 2;
                        }

                        return 1;
                    }
                    else
                    {
                        // Any other healthcare buildings (e.g. SaunaAI) aren't supported.
                        return 0;
                    }

                    return 1;

                // Fire.
                case ItemClass.Service.FireDepartment:
                    if (buildingInfo.m_buildingAI is HelicopterDepotAI)
                    {
                        // Fire helicopters.
                        transfers[0].Title = Translations.Translate("HELI_FIRE");
                        transfers[0].Reason = TransferManager.TransferReason.Fire2;
                    }
                    else
                    {
                        // Normal firetrucks.
                        transfers[0].Title = Translations.Translate("FIRETRUCK");
                        transfers[0].Reason = TransferManager.TransferReason.Fire;
                    }

                    return 1;

                case ItemClass.Service.Water:
                    // Water pumping.
                    if (buildingInfo.m_buildingAI is WaterFacilityAI waterFacilityAI && buildingInfo.m_class.m_level == ItemClass.Level.Level1 && waterFacilityAI.m_pumpingVehicles > 0)
                    {
                        transfers[0].Title = Translations.Translate("WATERPUMP");
                        transfers[0].Reason = TransferManager.TransferReason.FloodWater;
                        return 1;
                    }

                    // No other cases are supported.
                    return 0;

                case ItemClass.Service.Disaster:
                    // Disaster response - trucks and helicopters.
                    if (buildingInfo.m_buildingAI is DisasterResponseBuildingAI)
                    {
                        transfers[0].Title = Translations.Translate("DISASTER");
                        transfers[0].Reason = TransferManager.TransferReason.Collapsed;
                        transfers[1].Title = Translations.Translate("HELI_DISASTER");
                        transfers[1].Reason = TransferManager.TransferReason.Collapsed2;
                        return 2;
                    }

                    // No other cases are supported.
                    return 0;

                case ItemClass.Service.PoliceDepartment:
                    Building.Flags buildingFlags = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].m_flags;

                    // Police helicopter depot.
                    if (buildingInfo.m_buildingAI is HelicopterDepotAI)
                    {
                        transfers[0].Title = Translations.Translate("HELI_POLICE");
                        transfers[0].Reason = TransferManager.TransferReason.Crime;

                        // Prison Helicopter Mod.
                        if ((buildingFlags & Building.Flags.Downgrading) != 0)
                        {
                            transfers[1].Title = Translations.Translate("HELI_PRISON");
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
                            transfers[0].Title = Translations.Translate("PRISONVAN");
                            transfers[0].Reason = TransferManager.TransferReason.CriminalMove;

                            return 1;
                        }
                        else
                        {
                            // Normal police station.
                            // Police service.
                            transfers[0].Title = Translations.Translate("POLICECAR");
                            transfers[0].Reason = TransferManager.TransferReason.Crime;

                            // Prison Helicopter Mod.
                            if (buildingInfo.m_buildingAI.GetType().Name.Equals("PrisonCopterPoliceStationAI"))
                            {
                                // Big (central) police station.
                                if ((buildingFlags & Building.Flags.Downgrading) != 0)
                                {
                                    // Collect prisoners from smaller stations by sending a prison van.
                                    transfers[1].Title = Translations.Translate("PRISONVAN");
                                    transfers[1].Reason = (TransferManager.TransferReason)120;
                                    return 2;
                                }
                            }

                            return 1;
                        }
                    }

                case ItemClass.Service.Industrial:
                    // Zoned industry.
                    switch (buildingInfo.m_class.m_subService)
                    {
                        case ItemClass.SubService.IndustrialForestry:
                            if (buildingInfo.m_class.m_level == ItemClass.Level.Level2)
                            {
                                // Extractors.
                                transfers[0].Title = Translations.Translate("LOGS");
                                transfers[0].Reason = TransferManager.TransferReason.Logs;
                            }
                            else
                            {
                                // Processors.
                                transfers[0].Title = Translations.Translate("LUMBER");
                                transfers[0].Reason = TransferManager.TransferReason.Lumber;
                            }

                            break;

                        case ItemClass.SubService.IndustrialFarming:
                            if (buildingInfo.m_class.m_level == ItemClass.Level.Level2)
                            {
                                // Extractors.
                                transfers[0].Title = Translations.Translate("GRAIN");
                                transfers[0].Reason = TransferManager.TransferReason.Grain;
                            }
                            else
                            {
                                // Processors.
                                transfers[0].Title = Translations.Translate("FOOD");
                                transfers[0].Reason = TransferManager.TransferReason.Food;
                            }

                            break;

                        case ItemClass.SubService.IndustrialOil:
                            if (buildingInfo.m_class.m_level == ItemClass.Level.Level2)
                            {
                                // Extractors.
                                transfers[0].Title = Translations.Translate("OIL");
                                transfers[0].Reason = TransferManager.TransferReason.Oil;
                            }
                            else
                            {
                                // Processors.
                                transfers[0].Title = Translations.Translate("PETROL");
                                transfers[0].Reason = TransferManager.TransferReason.Petrol;
                            }

                            break;

                        case ItemClass.SubService.IndustrialOre:
                            if (buildingInfo.m_class.m_level == ItemClass.Level.Level2)
                            {
                                // Extractors.
                                transfers[0].Title = Translations.Translate("ORE");
                                transfers[0].Reason = TransferManager.TransferReason.Ore;
                            }
                            else
                            {
                                // Processors.
                                transfers[0].Title = Translations.Translate("COAL");
                                transfers[0].Reason = TransferManager.TransferReason.Coal;
                            }

                            break;

                        default:
                            transfers[0].Title = Translations.Translate("CARGO_TRUCK");
                            transfers[0].Reason = TransferManager.TransferReason.Goods;
                            break;
                    }

                    return 1;

                case ItemClass.Service.PlayerIndustry:
                    // Industries DLC.
                    if (buildingInfo.m_buildingAI is ExtractingFacilityAI extractingAI)
                    {
                        // Extractors.
                        transfers[0].Title = Translations.Translate("CARGO_TRUCK");
                        transfers[0].Reason = extractingAI.m_outputResource;
                        return 1;
                    }
                    else if (buildingInfo.m_buildingAI is ProcessingFacilityAI processingAI && buildingInfo.m_class.m_level < ItemClass.Level.Level5)
                    {
                        // Processors.
                        transfers[0].Title = Translations.Translate("CARGO_TRUCK");
                        transfers[0].Reason = processingAI.m_outputResource;
                        return 1;
                    }
                    else if (buildingInfo.m_buildingAI is UniqueFactoryAI)
                    {
                        // Unique factories.
                        transfers[0].Title = Translations.Translate("CARGO_TRUCK");
                        transfers[0].Reason = TransferManager.TransferReason.LuxuryProducts;
                        return 1;
                    }
                    else if (buildingInfo.m_buildingAI is WarehouseAI warehouseAI)
                    {
                        // Warehouses.
                        transfers[0].Title = Translations.Translate("CARGO_TRUCK");
                        transfers[0].Reason = warehouseAI.GetActualTransferReason(buildingID, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID]);
                        return 1;
                    }

                    return 0;

                case ItemClass.Service.Road:
                    if (buildingInfo.m_buildingAI is MaintenanceDepotAI)
                    {
                        // Road maintenance.
                        transfers[0].Title = Translations.Translate("ROAD_MAINT");
                    }
                    else if (buildingInfo.m_buildingAI is SnowDumpAI)
                    {
                        // Snow dumps.
                        transfers[0].Title = Translations.Translate("ROAD_SNOW");
                    }
                    else
                    {
                        // No other cases are supported.
                        return 0;
                    }

                    transfers[0].Reason = TransferManager.TransferReason.None;
                    return 1;

                case ItemClass.Service.Beautification:
                    if (buildingInfo.m_buildingAI is MaintenanceDepotAI)
                    {
                        // Park maintenance.
                        transfers[0].Title = Translations.Translate("PARK_MAINT");
                        transfers[0].Reason = TransferManager.TransferReason.None;
                        return 1;
                    }

                    // No other cases are supported.
                    return 0;

                case ItemClass.Service.PublicTransport:
                    if (buildingInfo.m_buildingAI is PostOfficeAI postOfficeAI)
                    {
                        // Post office vs. mail sorting facility - post offices have vans.
                        if (postOfficeAI.m_postVanCount > 0)
                        {
                            // Post office.
                            transfers[0].Title = Translations.Translate("MAIL_COLLECT");
                            transfers[0].Reason = TransferManager.TransferReason.Mail;

                            // Post offices send unsorted mail via their trucks.
                            transfers[1].Title = Translations.Translate("MAIL_UNSORTED");
                            transfers[1].Reason = TransferManager.TransferReason.UnsortedMail;

                            // Post offices pick up sorted mail via their trucks.
                            transfers[2].Title = Translations.Translate("MAIL_SORTED");
                            transfers[2].Reason = TransferManager.TransferReason.SortedMail;

                            return 3;
                        }

                        // Mail sorting facility.
                        transfers[0].Title = Translations.Translate("MAIL_SORTED");
                        transfers[0].Reason = TransferManager.TransferReason.SortedMail;

                        transfers[1].Title = Translations.Translate("MAIL_EXPORT");
                        transfers[1].Reason = TransferManager.TransferReason.OutgoingMail;
                        return 2;
                    }
                    else if (buildingInfo.m_class.m_subService == ItemClass.SubService.PublicTransportTaxi)
                    {
                        // Taxi depots.
                        transfers[0].Title = Translations.Translate("TAXI");
                        transfers[0].Reason = TransferManager.TransferReason.Taxi;
                        return 1;
                    }
                    else if (buildingInfo.m_class.m_subService == ItemClass.SubService.PublicTransportTrain)
                    {
                        if (buildingInfo.m_class.m_level == ItemClass.Level.Level1)
                        {
                            // Intercity passenger trains.
                            transfers[0].Title = Translations.Translate("TRAIN_PASSENGER");
                            transfers[0].Reason = TransferManager.TransferReason.None;
                            return 1;
                        }

                        if (buildingInfo.m_class.m_level == ItemClass.Level.Level4)
                        {
                            // Cargo train terminals.
                            transfers[0].Title = Translations.Translate("TRAIN_CARGO");
                            transfers[0].Reason = TransferManager.TransferReason.None;
                            return 1;
                        }

                        // Unsupported train type.
                        return 0;
                    }
                    else if (buildingInfo.m_buildingAI is AirportGateAI)
                    {
                        // Airport passenger gate.
                        transfers[0].Title = Translations.Translate("AIR_PASSENGER");
                        transfers[0].Reason = TransferManager.TransferReason.None;
                        return 1;
                    }
                    else if (buildingInfo.m_buildingAI is AirportCargoGateAI)
                    {
                        // Airport passenger gate.
                        transfers[0].Title = Translations.Translate("AIR_CARGO");
                        transfers[0].Reason = TransferManager.TransferReason.None;
                        return 1;
                    }
                    else if (buildingInfo.m_class.m_subService == ItemClass.SubService.PublicTransportShip)
                    {
                        if (buildingInfo.m_class.m_level == ItemClass.Level.Level1)
                        {
                            // Passenger harbours.
                            transfers[0].Title = Translations.Translate("SHIP_PASSENGER");
                            transfers[0].Reason = TransferManager.TransferReason.None;
                            return 1;
                        }
                        else if (buildingInfo.m_class.m_level == ItemClass.Level.Level4)
                        {
                            // Cargo harbours.
                            transfers[0].Title = Translations.Translate("SHIP_CARGO");
                            transfers[0].Reason = TransferManager.TransferReason.None;
                            return 1;
                        }
                    }
                    else if (buildingInfo.m_class.m_subService == ItemClass.SubService.PublicTransportBus)
                    {
                        bool isIntercityBus = buildingInfo.m_class.m_level == ItemClass.Level.Level3;

                        // Check for secondary transport line info (bus-intercity bus hub).
                        if (!isIntercityBus)
                        {
                            ItemClass secondaryClass = (buildingInfo.m_buildingAI as TransportStationAI)?.m_secondaryTransportInfo.m_class;
                            if (secondaryClass != null)
                            {
                                isIntercityBus = secondaryClass.m_subService == ItemClass.SubService.PublicTransportBus && secondaryClass.m_level == ItemClass.Level.Level3;
                            }
                        }

                        if (isIntercityBus)
                        {
                            // Intercity buses.
                            transfers[0].Title = Translations.Translate("BUS_INTERCITY");
                            transfers[0].Reason = TransferManager.TransferReason.None;
                            return 1;
                        }

                        // Unsupported bus type.
                        return 0;
                    }
                    else if (buildingInfo.m_buildingAI is CableCarStationAI cableCarStationAI)
                    {
                        // Cable cars.
                        transfers[0].Title = Translations.Translate("CABLE_CAR");
                        transfers[0].Reason = cableCarStationAI.m_transportInfo.m_vehicleReason;
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
                            transfers[0].Title = Translations.Translate("GARBAGE_COLLECTION");
                            transfers[0].Reason = TransferManager.TransferReason.Garbage;

                            return 1;
                        }

                        // Recycling Center.
                        else if (buildingInfo.GetClassLevel() == ItemClass.Level.Level2 && landfillAI.m_materialProduction != 0)
                        {
                            // Garbage Collection.
                            transfers[0].Title = Translations.Translate("GARBAGE_COLLECTION");
                            transfers[0].Reason = TransferManager.TransferReason.Garbage;

                            return 1;
                        }

                        // Landfill Site.
                        else if (buildingInfo.GetClassLevel() == ItemClass.Level.Level1 && landfillAI.m_electricityProduction == 0)
                        {
                            // Garbage collection.
                            transfers[0].Title = Translations.Translate("GARBAGE_COLLECTION");
                            transfers[0].Reason = TransferManager.TransferReason.Garbage;

                            // Garbage move (emptying landfills) out.
                            transfers[1].Title = Translations.Translate("GARBAGE_TRANSFER");
                            transfers[1].Reason = TransferManager.TransferReason.GarbageMove;

                            return 2;
                        }

                        // Waste Transfer Facility.
                        else if (buildingInfo.GetClassLevel() == ItemClass.Level.Level3 && landfillAI.m_electricityProduction == 0)
                        {
                            // Garbage collection.
                            transfers[0].Title = Translations.Translate("GARBAGE_COLLECTION");
                            transfers[0].Reason = TransferManager.TransferReason.Garbage;

                            return 1;
                        }

                        // Waste Processing Complex.
                        else if (buildingInfo.GetClassLevel() == ItemClass.Level.Level4)
                        {
                            // Garbage Transfer for proccessing from Waste Transfer Facility and Landfill Site.
                            transfers[0].Title = Translations.Translate("GARBAGE_TRANSFER");
                            transfers[0].Reason = TransferManager.TransferReason.GarbageTransfer;

                            return 1;
                        }
                    }

                    // Undefined service.
                    return 0;

                case ItemClass.Service.Fishing:
                    if (buildingInfo.m_buildingAI is FishingHarborAI)
                    {
                        // Fishing harbours - cargo trucks and fishing boats.
                        transfers[0].Title = Translations.Translate("FISH_TRUCK");
                        transfers[0].Reason = TransferManager.TransferReason.Fish;

                        // Fishing boats.
                        transfers[1].Title = Translations.Translate("FISH_BOAT");
                        transfers[1].Reason = TransferManager.TransferReason.None;

                        return 2;
                    }

                    if (buildingInfo.m_buildingAI is FishFarmAI)
                    {
                        // Seaweed farms etc. - no boats.
                        transfers[0].Title = Translations.Translate("FISH_TRUCK");
                        transfers[0].Reason = TransferManager.TransferReason.Fish;

                        return 1;
                    }

                    if (buildingInfo.m_buildingAI is ProcessingFacilityAI)
                    {
                        // Fish factory.
                        transfers[0].Title = Translations.Translate("CARGO_TRUCK");
                        transfers[0].Reason = TransferManager.TransferReason.Goods;

                        return 1;
                    }

                    // Unsupported case.
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
            public string Title;

            /// <summary>
            /// Transfer reason.
            /// </summary>
            public TransferManager.TransferReason Reason;
        }
    }
}
