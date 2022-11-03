// <copyright file="TransportStationAIPatches.cs" company="algernon (K. Algernon A. Sheppard)">
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
    /// Harmony patches to TransportStationAI to implement vehicle selection.
    /// Runs after Transport Lines Manager, effictively meaning that TLM takes precedence if installed.
    /// </summary>
    [HarmonyPatch(typeof(TransportStationAI))]
    [HarmonyAfter("com.klyte.redirectors.TLM")]
    public static class TransportStationAIPatches
    {
        /// <summary>
        /// Harmony transpiler for TransportStationAI.CreateIncomingVehicle, replacing existing calls to VehicleManager.GetRandomVehicleInfo with a call to our custom replacement instead.
        /// </summary>
        /// <param name="instructions">Original ILCode.</param>
        /// <param name="original">Method being transpiled.</param>
        /// <returns>New ILCode.</returns>
        [HarmonyPatch("CreateIncomingVehicle")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> CreateIncomingVehicleTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase original) => TransportStationTranspiler(instructions, original);

        /// <summary>
        /// Harmony transpiler for TransportStationAI.CreateOutgoingVehicle, replacing existing calls to VehicleManager.GetRandomVehicleInfo with a call to our custom replacement instead.
        /// </summary>
        /// <param name="instructions">Original ILCode.</param>
        /// <param name="original">Method being transpiled.</param>
        /// <returns>New ILCode.</returns>
        [HarmonyPatch("CreateOutgoingVehicle")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> CreateOutgoingVehicleTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase original) => TransportStationTranspiler(instructions, original);

        /// <summary>
        /// Harmony transpiler to replace existing calls to VehicleManager.GetRandomVehicleInfo with a call to our custom replacement instead.
        /// </summary>
        /// <param name="instructions">Original ILCode.</param>
        /// <param name="original">Method being transpiled.</param>
        /// <returns>New ILCode.</returns>
        private static IEnumerable<CodeInstruction> TransportStationTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            Logging.Message("transpiling ", original.DeclaringType, ":", original.Name);

            // Reflection to get original and inserted methods for calls.
            MethodInfo getRandomVehicleType = AccessTools.Method(typeof(VehicleManager), nameof(VehicleManager.GetRandomVehicleInfo), new Type[] { typeof(Randomizer).MakeByRefType(), typeof(ItemClass.Service), typeof(ItemClass.SubService), typeof(ItemClass.Level), typeof(VehicleInfo.VehicleType) });
            MethodInfo chooseVehicleType = AccessTools.Method(typeof(TransportStationAIPatches), nameof(ChooseVehicleType));

            // Instruction enumerator.
            IEnumerator<CodeInstruction> instructionsEnumerator = instructions.GetEnumerator();

            // Iterate through each instruction in original code.
            while (instructionsEnumerator.MoveNext())
            {
                // Get next instruction.
                CodeInstruction instruction = instructionsEnumerator.Current;

                // If this instruction calls the GetPrimaryRandomVehicleInfo method, then replace it with a call to our custom method.
                if (instruction.opcode == OpCodes.Callvirt)
                {
                    if (instruction.Calls(getRandomVehicleType))
                    {
                        // Add buildingID and material params to call.
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        instruction = new CodeInstruction(OpCodes.Call, chooseVehicleType);
                        Logging.Message("transpiled");
                    }
                }

                // Output this instruction.
                yield return instruction;
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
        /// <returns>Vehicle prefab to spawn.</returns>
        private static VehicleInfo ChooseVehicleType(VehicleManager vehicleManager, ref Randomizer r, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, VehicleInfo.VehicleType type, ushort buildingID)
        {
            // Get any custom vehicle list for this build
            List<VehicleInfo> vehicleList = VehicleControl.GetVehicles(buildingID, TransferManager.TransferReason.None);
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