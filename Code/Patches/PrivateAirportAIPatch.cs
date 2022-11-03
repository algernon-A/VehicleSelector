// <copyright file="PrivateAirportAIPatch.cs" company="algernon (K. Algernon A. Sheppard)">
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
    /// Harmony transpiler to implement aviation club vehicle selection.
    /// </summary>
    [HarmonyPatch(typeof(PrivateAirportAI), "CheckVehicles")]
    public static class PrivateAirportAIPatch
    {
        /// <summary>
        /// Harmony transpiler for PrivateAirportAI.CheckVehicles, replacing existing calls to VehicleManager.GetRandomVehicleInfo with a call to our custom replacement instead.
        /// </summary>
        /// <param name="instructions">Original ILCode.</param>
        /// <param name="original">Method being transpiled.</param>
        /// <returns>Modified ILCode.</returns>
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            Logging.Message("transpiling ", original.DeclaringType, ":", original.Name);

            // Reflection to get original methods for calls.
            MethodInfo getRandomVehicle = AccessTools.Method(typeof(VehicleManager), nameof(VehicleManager.GetRandomVehicleInfo), new Type[] { typeof(Randomizer).MakeByRefType(), typeof(ItemClass.Service), typeof(ItemClass.SubService), typeof(ItemClass.Level) });

            // Reflection to get inserted methods for calls.
            MethodInfo chooseVehicle = AccessTools.Method(typeof(PrivateAirportAIPatch), nameof(ChooseVehicle));

            // Instruction enumerator.
            IEnumerator<CodeInstruction> instructionsEnumerator = instructions.GetEnumerator();

            // Iterate through each instruction in original code.
            while (instructionsEnumerator.MoveNext())
            {
                // Get next instruction.
                CodeInstruction instruction = instructionsEnumerator.Current;

                // If this instruction calls the GetRandomVehicle method, then replace it with a call to our custom method.
                // Standard version.
                if (instruction.Calls(getRandomVehicle))
                {
                    // Add buildingID param to call.
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
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
        /// <param name="vehicleManager">VehicleManager instance.</param>
        /// <param name="r">Randomizer reference.</param>
        /// <param name="service">Vehicle service.</param>
        /// <param name="subService">Vehicle subservice.</param>
        /// <param name="level">Vehicle level.</param>
        /// <param name="buildingID">Building ID of owning building.</param>
        /// <returns>Vehicle prefab to spawn.</returns>
        public static VehicleInfo ChooseVehicle(VehicleManager vehicleManager, ref Randomizer r, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, ushort buildingID)
        {
            // Get any custom vehicle list for this building.
            List<VehicleInfo> vehicleList = VehicleControl.GetVehicles(buildingID, TransferManager.TransferReason.None);
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
    }
}