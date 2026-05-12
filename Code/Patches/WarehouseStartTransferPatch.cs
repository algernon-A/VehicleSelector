// <copyright file="WarehouseStartTransferPatch.cs" company="algernon (K. Algernon A. Sheppard)">
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
    using ColossalFramework.Math;
    using HarmonyLib;

    /// <summary>
    /// Harmony transpiler to various StartTransfer methods to implement vehicle selection.
    /// </summary>
    [HarmonyPatch]
    [HarmonyBefore("NoBigTruck")]
    public static class WarehouseStartTransferPatch
    {
        private static NBTDelegate s_NBTGetTransferVehicleServiceDelegate;

        /// <summary>
        /// Delegate to NoBigTruck mod's custom GetTransferVehicleService method.
        /// </summary>
        /// <param name="material">Vehicle's transfer reason.</param>
        /// <param name="level">Vehicle level.</param>
        /// <param name="randomizer">Randomizer reference.</param>
        /// <param name="targetBuildingId">Destination cargo station.</param>
        /// <param name="sourceBuildingId">Source cargo station.</param>
        /// <returns>Selected VehicleInfo for spawning.</returns>
        internal delegate VehicleInfo NBTDelegate(TransferManager.TransferReason material, ItemClass.Level level, ref Randomizer randomizer, ushort targetBuildingId, ushort sourceBuildingId);

        /// <summary>
        /// Target methods.
        /// </summary>
        /// <returns>List of methods to transpile.</returns>
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(WarehouseAI), nameof(WarehouseAI.StartTransfer));
            yield return AccessTools.Method(typeof(ExtractingFacilityAI), nameof(ExtractingFacilityAI.StartTransfer));
            yield return AccessTools.Method(typeof(ProcessingFacilityAI), nameof(ProcessingFacilityAI.StartTransfer));
        }

        /// <summary>
        /// Harmony transpiler for building StartTransfer methods, replacing existing calls to WarehousAI.GetTransferVehicleService with a call to our custom replacement instead.
        /// </summary>
        /// <param name="instructions">Original ILCode.</param>
        /// <param name="original">Method being transpiled.</param>
        /// <returns>New ILCode.</returns>
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            Logging.Message("transpiling ", original.DeclaringType, ":", original.Name);

            // Reflection to get original and inserted methods for calls.
            MethodInfo getTransferVehicle = AccessTools.Method(typeof(WarehouseAI), nameof(WarehouseAI.GetTransferVehicleService));
            MethodInfo chooseVehicle = AccessTools.Method(typeof(WarehouseStartTransferPatch), nameof(ChooseVehicle));

            // Instruction enumerator.
            IEnumerator<CodeInstruction> instructionsEnumerator = instructions.GetEnumerator();

            // Iterate through each instruction in original code.
            while (instructionsEnumerator.MoveNext())
            {
                // Get next instruction.
                CodeInstruction instruction = instructionsEnumerator.Current;

                // If this instruction calls the GetRandomVehicle method, then replace it with a call to our custom method.
                if (instruction.opcode == OpCodes.Call && instruction.Calls(getTransferVehicle))
                {
                    // Add buildingID and material params to call.
                    yield return new CodeInstruction(OpCodes.Ldarg_1);

                    // If this is an WarehouseAI, then we need to add the sourceBuildingId parameter for No Big Truck mod compatibility.
                    if (original.DeclaringType.Name == "WarehouseAI")
                    {
                        // Add sourceBuildingId parameter to call for No Big Truck mod.
                        yield return new CodeInstruction(OpCodes.Ldarga_S, 4);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TransferManager.TransferOffer), nameof(TransferManager.TransferOffer.Building)));
                    }
                    else
                    {
                        yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    }

                    instruction = new CodeInstruction(OpCodes.Call, chooseVehicle);
                    Logging.Message("transpiled");
                }

                // Output this instruction.
                yield return instruction;
            }
        }

        /// <summary>
        /// Chooses a vehicle for a transfer from our custom lists (reverting to game code if no custom list exists for this building and transfer).
        /// </summary>
        /// <param name="material">Transfer material.</param>
        /// <param name="level">Vehicle level.</param>
        /// <param name="randomizer">Randomizer reference.</param>
        /// <param name="buildingID">Building ID of owning building.</param>
        /// <param name="sourceBuildingId">Source building ID.</param>
        /// <returns>Vehicle prefab to spawn.</returns>
        public static VehicleInfo ChooseVehicle(TransferManager.TransferReason material, ItemClass.Level level, ref Randomizer randomizer, ushort buildingID, ushort sourceBuildingId)
        {
            // Get any custom vehicle list for this building.
            List<VehicleInfo> vehicleList = VehicleControl.GetVehicles(buildingID, material);
            if (vehicleList == null)
            {
                // Insert check for No Big Truck mod.
                if (s_NBTGetTransferVehicleServiceDelegate != null && sourceBuildingId != default)
                {
                    return s_NBTGetTransferVehicleServiceDelegate.Invoke(material, level, ref randomizer, buildingID, sourceBuildingId);
                }

                // No custom vehicle selection - use game method.
                return WarehouseAI.GetTransferVehicleService(material, level, ref randomizer);
            }

            // Custom vehicle selection found - randomly choose one.
            int i = randomizer.Int32((uint)vehicleList.Count);
            {
                return vehicleList[i];
            }
        }

        /// <summary>
        /// Checks for the No Big Truck mod, if one is found, creates the delegate to its custom method for WarehouseAI.GetTransferVehicleService.
        /// </summary>
        internal static void CheckMods()
        {
            CheckNBTMod();
        }

        /// <summary>
        /// Checks for the No Big Truck mod, and if found, creates the delegate to its custom method for WarehouseAI.GetTransferVehicleService.
        /// </summary>
        internal static void CheckNBTMod()
        {
            try
            {
                Assembly noBigTruck = AssemblyUtils.GetEnabledAssembly("NoBigTruck");
                if (noBigTruck != null)
                {
                    s_NBTGetTransferVehicleServiceDelegate = AccessTools.MethodDelegate<NBTDelegate>(AccessTools.Method(noBigTruck.GetType("NoBigTruck.Manager"), "GetTransferVehicleService"));
                    if (s_NBTGetTransferVehicleServiceDelegate != null)
                    {
                        Logging.Message("got delegate to No Big Truck mod (NoBigTruck.Manager.GetTransferVehicleService)");
                    }
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception getting delegate from No Big Truck mod (NoBigTruck.Manager.GetTransferVehicleService)");
            }
        }
    }
}