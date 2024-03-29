﻿// <copyright file="StartTransferPatches.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace VehicleSelector
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using AlgernonCommons;
    using ColossalFramework;
    using ColossalFramework.Math;
    using HarmonyLib;

    /// <summary>
    /// Harmony transpiler to various StartTransfer methods to implement vehicle selection.
    /// </summary>
    [HarmonyPatch]
    [HarmonyBefore("NoBigTruck")]
    public static class StartTransferPatches
    {
        /// <summary>
        /// Target methods.
        /// </summary>
        /// <returns>List of methods to transpile.</returns>
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return typeof(HospitalAI).GetMethod(nameof(HospitalAI.StartTransfer));
            yield return typeof(PoliceStationAI).GetMethod(nameof(PoliceStationAI.StartTransfer));
            yield return typeof(CemeteryAI).GetMethod(nameof(CemeteryAI.StartTransfer));
            yield return typeof(MaintenanceDepotAI).GetMethod(nameof(MaintenanceDepotAI.StartTransfer));
            yield return typeof(PostOfficeAI).GetMethod(nameof(PostOfficeAI.StartTransfer));
            yield return typeof(LandfillSiteAI).GetMethod(nameof(LandfillSiteAI.StartTransfer));
            yield return typeof(SnowDumpAI).GetMethod(nameof(SnowDumpAI.StartTransfer));
            yield return typeof(FireStationAI).GetMethod(nameof(FireStationAI.StartTransfer));
            yield return typeof(DisasterResponseBuildingAI).GetMethod(nameof(DisasterResponseBuildingAI.StartTransfer));
            yield return typeof(IndustrialBuildingAI).GetMethod(nameof(IndustrialBuildingAI.StartTransfer));
            yield return typeof(IndustrialExtractorAI).GetMethod(nameof(IndustrialExtractorAI.StartTransfer));
            yield return typeof(FishingHarborAI).GetMethod(nameof(FishingHarborAI.StartTransfer));
            yield return typeof(FishFarmAI).GetMethod(nameof(FishFarmAI.StartTransfer));
            yield return typeof(HelicopterDepotAI).GetMethod(nameof(HelicopterDepotAI.StartTransfer));
            yield return typeof(WaterFacilityAI).GetMethod(nameof(WaterFacilityAI.StartTransfer));
            yield return typeof(BankOfficeAI).GetMethod(nameof(BankOfficeAI.StartTransfer));

            // Prison helicopter mod, if avaialble.
            MethodInfo prisonHeliAI = Type.GetType("PrisonHelicopter.AI.PrisonCopterPoliceStationAI,PrisonHelicopter", false)?.GetMethod("StartTransfer");
            if (prisonHeliAI != null)
            {
                yield return prisonHeliAI;
            }

            MethodInfo heliDepotAI = Type.GetType("PrisonHelicopter.AI.PoliceHelicopterDepotAI,PrisonHelicopter", false)?.GetMethod("StartTransfer");
            if (heliDepotAI != null)
            {
                yield return heliDepotAI;
            }
        }

        /// <summary>
        /// Harmony transpiler for building StartTransfer methods, replacing existing calls to VehicleManager.GetRandomVehicleInfo with a call to our custom replacement instead.
        /// </summary>
        /// <param name="instructions">Original ILCode.</param>
        /// <param name="original">Method being transpiled.</param>
        /// <returns>Modified ILCode.</returns>
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            Logging.Message("transpiling ", original.DeclaringType, ":", original.Name);

            // Reflection to get original methods for calls.
            MethodInfo getRandomVehicle = AccessTools.Method(typeof(VehicleManager), nameof(VehicleManager.GetRandomVehicleInfo), new Type[] { typeof(Randomizer).MakeByRefType(), typeof(ItemClass.Service), typeof(ItemClass.SubService), typeof(ItemClass.Level) });
            MethodInfo getRandomVehicleType = AccessTools.Method(typeof(VehicleManager), nameof(VehicleManager.GetRandomVehicleInfo), new Type[] { typeof(Randomizer).MakeByRefType(), typeof(ItemClass.Service), typeof(ItemClass.SubService), typeof(ItemClass.Level), typeof(VehicleInfo.VehicleType) });
            MethodInfo getSelectedVehicle = AccessTools.Method(typeof(PlayerBuildingAI), nameof(PlayerBuildingAI.GetSelectedVehicle));

            // Reflection to get inserted methods for calls.
            MethodInfo chooseVehicle = AccessTools.Method(typeof(StartTransferPatches), nameof(ChooseVehicle));
            MethodInfo chooseVehicleType = AccessTools.Method(typeof(StartTransferPatches), nameof(ChooseVehicleType));
            MethodInfo selectVehicle = AccessTools.Method(typeof(StartTransferPatches), nameof(GetSelectedVehicle));

            // Instruction enumerator.
            IEnumerator<CodeInstruction> instructionsEnumerator = instructions.GetEnumerator();

            // Iterate through each instruction in original code.
            while (instructionsEnumerator.MoveNext())
            {
                // Get next instruction.
                CodeInstruction instruction = instructionsEnumerator.Current;

                // If this instruction calls the GetSelectedVehicle method, then replace it with a call to our custom method.
                if (instruction.Calls(getSelectedVehicle))
                {
                    // Add material param to call.
                    yield return new CodeInstruction(OpCodes.Ldarg_3);
                    instruction = new CodeInstruction(OpCodes.Call, selectVehicle);
                }

                // If this instruction calls the GetRandomVehicle method, then replace it with a call to our custom method.
                // Standard version.
                else if (instruction.Calls(getRandomVehicle))
                {
                    // Get any labels attached to original instruction.
                    List<Label> labels = instruction.labels;

                    // Add buildingID and material params to call, restoring any original labels against the insert start.
                    yield return new CodeInstruction(OpCodes.Ldarg_1) { labels = labels };
                    yield return new CodeInstruction(OpCodes.Ldarg_3);
                    instruction = new CodeInstruction(OpCodes.Call, chooseVehicle);
                    Logging.Message("transpiled");
                }

                // If this instruction calls the GetRandomVehicle method, then replace it with a call to our custom method.
                // Overload with additional VehicleType argument.
                else if (instruction.Calls(getRandomVehicleType))
                {
                    // Add buildingID and material params to call.
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_3);
                    instruction = new CodeInstruction(OpCodes.Call, chooseVehicleType);
                    Logging.Message("transpiled");
                }

                // Output this instruction.
                yield return instruction;
            }
        }

        /// <summary>
        /// Chooses a vehicle for a transfer from our custom lists (reverting to game code if no custom list exists for this building and transfer).
        /// </summary>
        /// <param name="buildingAI">Building AI instance.</param>
        /// <param name="buildingID">Building ID.</param>
        /// <param name="material">Transfer material.</param>
        /// <returns>Vehicle prefab to spawn.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Original instance reference")]
        public static VehicleInfo GetSelectedVehicle(BuildingAI buildingAI, ushort buildingID, TransferManager.TransferReason material)
        {
            // Get any custom vehicle list for this build
            List<VehicleInfo> vehicleList = VehicleControl.GetVehicles(buildingID, material);
            if (vehicleList == null)
            {
                // No custom vehicle selection - use game method.
                return null;
            }

            // Custom vehicle selection found - randomly choose one.
            int i = Singleton<SimulationManager>.instance.m_randomizer.Int32((uint)vehicleList.Count);
            {
                return vehicleList[i];
            }
        }

        /// <summary>
        /// Chooses a vehicle for a transfer from our custom lists (reverting to game code if no custom list exists for this building and transfer).
        /// </summary>
        /// <param name="vehicleManager">VehicleManager instance.</param>
        /// <param name="r">Randomizer reference.</param>
        /// <param name="service">Vehicle service.</param>
        /// <param name="subService">Vehicle subservice.</param>
        /// <param name="level">Vehicle level.</param>
        /// <param name="buildingID">Building ID of owning building.</param>
        /// <param name="material">Transfer material.</param>
        /// <returns>Vehicle prefab to spawn.</returns>
        public static VehicleInfo ChooseVehicle(VehicleManager vehicleManager, ref Randomizer r, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, ushort buildingID, TransferManager.TransferReason material)
        {
            // Get any custom vehicle list for this building.
            List<VehicleInfo> vehicleList = VehicleControl.GetVehicles(buildingID, material);
            if (vehicleList == null)
            {
                // No custom vehicle selection - use game method.
                return vehicleManager.GetRandomVehicleInfo(ref r, service, subService, level);
            }

            // Custom vehicle selection found - randomly choose one.
            int i = r.Int32((uint)vehicleList.Count);
            {
                return vehicleList[i];
            }
        }

        /// <summary>
        /// Chooses a vehicle for a transfer from our custom lists (reverting to game code if no custom list exists for this building and transfer).
        /// Special version with additional VehicleType argument.
        /// </summary>
        /// <param name="vehicleManager">VehicleManager instance.</param>
        /// <param name="r">Randomizer reference.</param>
        /// <param name="service">Vehicle service.</param>
        /// <param name="subService">Vehicle subservice.</param>
        /// <param name="level">Vehicle level.</param>
        /// <param name="type">Vehicle type.</param>
        /// <param name="buildingID">Building ID of owning building.</param>
        /// <param name="material">Transfer material.</param>
        /// <returns>Vehicle prefab to spawn.</returns>
        public static VehicleInfo ChooseVehicleType(VehicleManager vehicleManager, ref Randomizer r, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, VehicleInfo.VehicleType type, ushort buildingID, TransferManager.TransferReason material)
        {
            // Get any custom vehicle list for this build
            List<VehicleInfo> vehicleList = VehicleControl.GetVehicles(buildingID, material);
            if (vehicleList == null)
            {
                // No custom vehicle selection - use game method.
                return vehicleManager.GetRandomVehicleInfo(ref r, service, subService, level, type);
            }

            // Custom vehicle selection found - randomly choose one.
            int i = r.Int32((uint)vehicleList.Count);
            {
                return vehicleList[i];
            }
        }
    }
}