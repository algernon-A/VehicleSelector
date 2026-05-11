// <copyright file="VehicleControl.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace VehicleSelector
{
    using System.Collections.Generic;
    using System.IO;
    using AlgernonCommons;

    /// <summary>
    /// Static class to control building vehicles.
    /// </summary>
    internal static class VehicleControl
    {
        // Dictionary to hold selected vehicles.
        // Key is packed ((byte)transfertype << 24) | (ushort)buildingID.
        private static readonly Dictionary<uint, List<VehicleInfo>> AssignedVehicles = new Dictionary<uint, List<VehicleInfo>>();

        /// <summary>
        /// Returns the list of selected vehicles for the given building, transfer direction, and material.
        /// </summary>
        /// <param name="buildingID">Building ID.</param>
        /// <param name="material">Transfer material.</param>
        /// <returns>List of selected vehicles (null if none).</returns>
        internal static List<VehicleInfo> GetVehicles(ushort buildingID, TransferManager.TransferReason material)
        {
            // Validity check.
            if (buildingID != 0)
            {
                // Retrieve and return any existing dictionary entry.
                if (AssignedVehicles.TryGetValue(BuildKey(buildingID, material), out List<VehicleInfo> vehicleList))
                {
                    return vehicleList;
                }

                // If no entry was found, try again using the 'none' transfer method for any default entries.
                // Fish are excluded to prevent confusion with fishing boats.
                // DummyTrain is excluded to prevent confusion with boats for cargo hubs.
                if (material != TransferManager.TransferReason.None & material != TransferManager.TransferReason.Fish & material != TransferManager.TransferReason.DummyTrain)
                {
                    // No entry found; try again using the default transfer material
                    if (AssignedVehicles.TryGetValue(BuildKey(buildingID, TransferManager.TransferReason.None), out vehicleList))
                    {
                        return vehicleList;
                    }
                }
            }

            // If we got here, no entry was found; return an empty new list.
            return null;
        }

        /// <summary>
        /// Adds a vehicle the list of selected vehicles for the given building, transfer direction, and material.
        /// </summary>
        /// <param name="buildingID">Building ID.</param>
        /// <param name="material">Transfer material.</param>
        /// <param name="vehicle">Vehicle prefab to add.</param>
        internal static void AddVehicle(ushort buildingID, TransferManager.TransferReason material, VehicleInfo vehicle)
        {
            // Safety checks.
            if (buildingID == 0 || vehicle == null)
            {
                Logging.Error("invalid parameter passed to VehicleControl.AddVehicle");
                return;
            }

            // Do we have an existing entry?
            uint key = BuildKey(buildingID, material);
            if (!AssignedVehicles.ContainsKey(key))
            {
                // No existing entry - create one.
                AssignedVehicles.Add(key, new List<VehicleInfo> { vehicle });
            }
            else
            {
                // Existing entry - add this vehicle to the list, if it isn't already there.
                if (!AssignedVehicles[key].Contains(vehicle))
                {
                    AssignedVehicles[key].Add(vehicle);
                }
            }
        }

        /// <summary>
        /// Removes a vehicle from list of selected vehicles for the given building, transfer direction, and material.
        /// </summary>
        /// <param name="buildingID">Building ID.</param>
        /// <param name="material">Transfer reason.</param>
        /// <param name="vehicle">Vehicle prefab to remove.</param>
        internal static void RemoveVehicle(ushort buildingID, TransferManager.TransferReason material, VehicleInfo vehicle)
        {
            // Safety checks.
            if (buildingID == 0 || vehicle == null)
            {
                Logging.Error("invalid parameter passed to VehicleControl.RemoveVehicle");
                return;
            }

            // Do we have an existing entry?
            uint key = BuildKey(buildingID, material);
            if (AssignedVehicles.ContainsKey(key))
            {
                // Yes - remove vehicle from list.
                AssignedVehicles[key].Remove(vehicle);

                // If no vehicles remaining in this list, remove the entire entry.
                if (AssignedVehicles[key].Count == 0)
                {
                    AssignedVehicles.Remove(key);
                }
            }
        }

        /// <summary>
        /// Pastes the given list of vehicles to the specified building and reason (overwriting any existing entries).
        /// </summary>
        /// <param name="buildingID">Building ID.</param>
        /// <param name="material">Transfer reason.</param>
        /// <param name="vehicles">Vehicle list to paste.</param>
        internal static void PasteVehicles(ushort buildingID, TransferManager.TransferReason material, List<VehicleInfo> vehicles)
        {
            // Safety checks.
            if (buildingID == 0)
            {
                Logging.Error("invalid buildingID passed to VehicleControl.ClearVehicles");
                return;
            }

            // Get entry key.
            uint key = BuildKey(buildingID, material);

            // Handle any null pastes (clear settings).
            if (vehicles == null || vehicles.Count == 0)
            {
                AssignedVehicles.Remove(key);
                return;
            }

            // Do we have an existing entry?
            if (AssignedVehicles.ContainsKey(key))
            {
                // Yes - clear list.
                AssignedVehicles[key].Clear();
            }
            else
            {
                // No - add new entry.
                AssignedVehicles.Add(key, new List<VehicleInfo>());
            }

            // Copy from list of provided vehicles (copying content, not the list reference itself).
            AssignedVehicles[key].AddRange(vehicles);
        }

        /// <summary>
        /// Gets the effective ItemClass for vehicle allocation for the given building.
        /// </summary>
        /// <param name="buildingID">Building ID.</param>
        /// <param name="buildingBuffer">Building buffer.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <param name="service">Effective service.</param>
        /// <param name="subService">Effective sub-service.</param>
        /// <param name="level">Effective level.</param>
        internal static void GetEffectiveClass(ushort buildingID, Building[] buildingBuffer, TransferManager.TransferReason transferReason, out ItemClass.Service service, out ItemClass.SubService subService, out ItemClass.Level level)
        {
            // Local references.
            BuildingInfo buildingInfo = buildingBuffer[buildingID].Info;
            ItemClass originalClass = buildingInfo.m_class;
            ItemClass.Service originalService = originalClass.m_service;

            // Return values.
            ItemClass.Service effectiveService = originalService;
            ItemClass.SubService effectiveSubService = originalClass.m_subService;
            ItemClass.Level effectiveLevel = originalClass.m_level;

            // Player industry requires some translation into private industry equivalents.
            if (originalService == ItemClass.Service.PlayerIndustry)
            {
                effectiveService = ItemClass.Service.Industrial;
                effectiveSubService = ItemClass.SubService.None;

                // Get building transfer type for those buildings with variable types.
                TransferManager.TransferReason variableTransferReason = TransferManager.TransferReason.None;
                switch (buildingInfo.m_buildingAI)
                {
                    case WarehouseAI warehouseAI:
                        variableTransferReason = warehouseAI.GetTransferReason(buildingID, ref buildingBuffer[buildingID]);
                        break;
                    case ExtractingFacilityAI extractorAI:
                        variableTransferReason = extractorAI.m_outputResource;
                        break;
                    case ProcessingFacilityAI processorAI:
                        variableTransferReason = processorAI.m_outputResource;
                        break;
                }

                // Translate into private industry equivalents - conversions are from WarehouseAI.GetTransferVehicleService.
                switch (variableTransferReason)
                {
                    // Ore.
                    case TransferManager.TransferReason.Ore:
                    case TransferManager.TransferReason.Coal:
                    case TransferManager.TransferReason.Glass:
                    case TransferManager.TransferReason.Metals:
                        effectiveSubService = ItemClass.SubService.IndustrialOre;
                        break;

                    // Forestry.
                    case TransferManager.TransferReason.Logs:
                    case TransferManager.TransferReason.Lumber:
                    case TransferManager.TransferReason.Paper:
                    case TransferManager.TransferReason.PlanedTimber:
                        effectiveSubService = ItemClass.SubService.IndustrialForestry;
                        break;

                    // Oil.
                    case TransferManager.TransferReason.Oil:
                    case TransferManager.TransferReason.Petrol:
                    case TransferManager.TransferReason.Petroleum:
                    case TransferManager.TransferReason.Plastics:
                        effectiveSubService = ItemClass.SubService.IndustrialOil;
                        break;

                    // Farming.
                    case TransferManager.TransferReason.Grain:
                    case TransferManager.TransferReason.Food:
                    case TransferManager.TransferReason.Flours:
                        effectiveSubService = ItemClass.SubService.IndustrialFarming;
                        break;

                    // Animal products have their own category.
                    case TransferManager.TransferReason.AnimalProducts:
                        effectiveService = ItemClass.Service.PlayerIndustry;
                        effectiveSubService = ItemClass.SubService.PlayerIndustryFarming;
                        break;

                    // Generic goods.
                    case TransferManager.TransferReason.Goods:
                        effectiveSubService = ItemClass.SubService.IndustrialGeneric;
                        break;

                    // Luxury products.
                    case TransferManager.TransferReason.LuxuryProducts:
                        effectiveService = ItemClass.Service.PlayerIndustry;
                        break;

                    // Fish warehousing.
                    case TransferManager.TransferReason.Fish:
                        if (buildingInfo.m_buildingAI is WarehouseAI)
                        {
                            effectiveService = ItemClass.Service.Fishing;
                        }

                        break;
                }
            }
            else if (originalClass.m_subService == ItemClass.SubService.PublicTransportPost)
            {
                // Special treatement for post offices - post vans have level 2, others level 5.
                effectiveLevel = transferReason == TransferManager.TransferReason.Mail ? ItemClass.Level.Level2 : ItemClass.Level.Level5;
            }
            else if (originalService == ItemClass.Service.Fishing)
            {
                if (transferReason == TransferManager.TransferReason.None && buildingInfo.m_buildingAI is FishingHarborAI fishingHarborAI)
                {
                    // Fishing harbors, fishing boat selection - use boat class.
                    effectiveService = fishingHarborAI.m_boatClass.m_service;
                    effectiveSubService = fishingHarborAI.m_boatClass.m_subService;
                    effectiveLevel = fishingHarborAI.m_boatClass.m_level;
                }
                else if (buildingInfo.m_buildingAI is FishFarmAI)
                {
                    // Set fish farm AI to level 1 for fish trucks.
                    effectiveLevel = ItemClass.Level.Level1;
                }
                else if (buildingInfo.m_buildingAI is ProcessingFacilityAI)
                {
                    // Fish factories.
                    effectiveService = ItemClass.Service.Industrial;
                    effectiveSubService = ItemClass.SubService.IndustrialGeneric;
                }
            }
            else if (originalClass.m_subService == ItemClass.SubService.PublicTransportBus && originalClass.m_level == ItemClass.Level.Level1)
            {
                // Bus station - check for secondary transport info.
                ItemClass secondaryClass = (buildingInfo.m_buildingAI as TransportStationAI)?.m_secondaryTransportInfo?.m_class;
                if (secondaryClass != null && secondaryClass.m_subService == ItemClass.SubService.PublicTransportBus)
                {
                    // Secondary info found - use that instead.
                    effectiveLevel = secondaryClass.m_level;
                }
            }
            else if (originalClass.m_subService == ItemClass.SubService.PublicTransportShip & transferReason == TransferManager.TransferReason.DummyTrain)
            {
                // Trains from cargo hubs.
                effectiveSubService = ItemClass.SubService.PublicTransportTrain;
                effectiveLevel = ItemClass.Level.Level4;
            }
            else if (originalService == ItemClass.Service.PoliceDepartment && originalClass.m_subService != ItemClass.SubService.PoliceDepartmentBank && (transferReason == (TransferManager.TransferReason)223 || transferReason == (TransferManager.TransferReason)224))
            {
                effectiveLevel = ItemClass.Level.Level4;
            }

            // Populate return values.
            service = effectiveService;
            subService = effectiveSubService;
            level = effectiveLevel;
        }

        /// <summary>
        /// Removes all references to a given building from the vehicle dictionary.
        /// </summary>
        /// <param name="buildingID">BuildingID to remove.</param>
        internal static void ReleaseBuilding(ushort buildingID)
        {
            // Iterate through each key in dictionary, finding any entries corresponding to the given building ID.
            List<uint> removeList = new List<uint>();
            foreach (uint key in AssignedVehicles.Keys)
            {
                if ((key & 0x0000FFFF) == buildingID)
                {
                    removeList.Add(key);
                }
            }

            // Iterate through each entry found and remove it from the dictionary.
            foreach (uint key in removeList)
            {
                AssignedVehicles.Remove(key);
            }
        }

        /// <summary>
        /// Serializes vehicle selection data.
        /// </summary>
        /// <param name="writer">Binary writer instance to serialize to.</param>
        internal static void Serialize(BinaryWriter writer)
        {
            // Write length of dictionary.
            Logging.Message("serializing vehicle data with ", AssignedVehicles.Count, " entries");
            writer.Write(AssignedVehicles.Count);

            // Serialise each building entry.
            foreach (KeyValuePair<uint, List<VehicleInfo>> entry in AssignedVehicles)
            {
                // Local reference.
                List<VehicleInfo> vehicleList = entry.Value;

                // Skip empty entries.
                if (vehicleList != null && vehicleList.Count > 0)
                {
                    // Serialize key.
                    writer.Write(entry.Key);

                    // Serialize list (vehicle names).
                    writer.Write((ushort)vehicleList.Count);
                    foreach (VehicleInfo vehicle in vehicleList)
                    {
                        // If vehicle is invalid for some reason, just write an empty string (will be ignored on deserialization).
                        writer.Write(vehicle?.name ?? string.Empty);
                    }
                }

                Logging.Message("wrote entry ", entry.Key);
            }
        }

        /// <summary>
        /// Deserializes vehicle selection data.
        /// </summary>
        /// <param name="reader">Binary reader instance to deserialize from.</param>
        internal static void Deserialize(BinaryReader reader)
        {
            // Clear dictionary.
            AssignedVehicles.Clear();

            // Iterate through each entry read.
            int numEntries = reader.ReadInt32();
            Logging.Message("deserializing vehicle data with ", numEntries, " entries");
            for (int i = 0; i < numEntries; ++i)
            {
                // Dictionary entry key.
                uint key = reader.ReadUInt32();

                // List length.
                ushort numVehicles = reader.ReadUInt16();
                if (numVehicles > 0)
                {
                    // Read list.
                    List<VehicleInfo> vehicleList = new List<VehicleInfo>();
                    for (int j = 0; j < numVehicles; ++j)
                    {
                        string vehicleName = reader.ReadString();
                        if (!string.IsNullOrEmpty(vehicleName))
                        {
                            // Attempt to find matching prefab from saved vehicle name.
                            VehicleInfo thisVehicle = PrefabCollection<VehicleInfo>.FindLoaded(vehicleName);

                            // Make sure that vehicle is laoded before we add to list.
                            if (thisVehicle != null)
                            {
                                vehicleList.Add(thisVehicle);
                            }
                            else
                            {
                                Logging.Message("couldn't find vehicle ", vehicleName);
                            }
                        }
                        else
                        {
                            Logging.Error("invalid vehicle name");
                        }
                    }

                    // If at least one vehicle was recovered, add the entry to the dictionary.
                    if (vehicleList.Count > 0)
                    {
                        // Check for duplicates.
                        if (AssignedVehicles.ContainsKey(key))
                        {
                            Logging.Message("duplicate vehicle key ", key, "; skipping");
                        }
                        else
                        {
                            AssignedVehicles.Add(key, vehicleList);
                        }
                    }
                }

                Logging.Message("read entry ", key);
            }
        }

        /// <summary>
        /// Builds the vehicle dictionary key from the provided parametes.
        /// </summary>
        /// <param name="buildingID">Building ID.</param>
        /// <param name="material">Transfer material.</param>
        /// <returns>Vehicle dictionary key.</returns>
        private static uint BuildKey(ushort buildingID, TransferManager.TransferReason material) => ((uint)material << 24) | buildingID;
    }
}