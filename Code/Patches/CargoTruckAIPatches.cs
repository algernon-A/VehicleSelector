// <copyright file="CargoTruckAIPatches.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace VehicleSelector.Code.Patches
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using ColossalFramework.Math;
    using HarmonyLib;

    /// <summary>
    /// Harmony patches for CargoTruckAI to (surprisingly) implement custom cargo train selection.
    /// </summary>
    [HarmonyPatch(typeof(CargoTruckAI))]
    [HarmonyBefore("NoBigTruck", "github.com/bloodypenguin/Skylines-CargoFerries")]
    internal static class CargoTruckAIPatches
    {
        /// <summary>
        /// Harmony transpiler for CargoTruckAI.ChangeVehicleType, replacing existing calls to VehicleManager.GetRandomVehicleInfo with a call to our custom replacement instead.
        /// </summary>
        /// <param name="instructions">Original ILCode.</param>
        /// <returns>Modified ILCode.</returns>
        [HarmonyPatch(
            nameof(CargoTruckAI.ChangeVehicleType),
            new Type[] { typeof(VehicleInfo), typeof(ushort), typeof(Vehicle), typeof(PathUnit.Position), typeof(uint) },
            new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal })]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Original GetRandomVehicle method.
            MethodInfo getRandomVehicle = AccessTools.Method(typeof(VehicleManager), nameof(VehicleManager.GetRandomVehicleInfo), new Type[] { typeof(Randomizer).MakeByRefType(), typeof(ItemClass.Service), typeof(ItemClass.SubService), typeof(ItemClass.Level) });

            // Replacement method.
            MethodInfo chooseVehicle = AccessTools.Method(typeof(CargoTruckAIPatches), nameof(ChooseVehicle));

            // Instruction enumerator.
            IEnumerator<CodeInstruction> instructionsEnumerator = instructions.GetEnumerator();

            // Iterate through each instruction in original code.
            while (instructionsEnumerator.MoveNext())
            {
                // Get next instruction.
                CodeInstruction instruction = instructionsEnumerator.Current;

                // If this instruction calls the GetRandomVehicle method, then replace it with a call to our custom method.
                if (instruction.Calls(getRandomVehicle))
                {
                    // Add arguments for cargo station building IDs and the BuildingManager instance..
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 6);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 7);
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    instruction = new CodeInstruction(OpCodes.Call, chooseVehicle);
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
        /// <param name="cargoStationSource">Cargo station (if any) at vehicle path's current position.</param>
        /// <param name="cargoStationDest">Cargo station (if any) at vehicle path's previous position.</param>
        /// <param name="buildingManager">BuildingManager instance.</param>
        /// <returns>Vehicle prefab to spawn.</returns>
        public static VehicleInfo ChooseVehicle(VehicleManager vehicleManager, ref Randomizer r, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, ushort cargoStationSource, ushort cargoStationDest, BuildingManager buildingManager)
        {
            // Determine transfer direction.
            BuildingInfo sourceStation = buildingManager.m_buildings.m_buffer[cargoStationSource].Info;
            bool isIncoming = sourceStation?.m_buildingAI is OutsideConnectionAI;

            // Get any custom vehicle list for this building.
            List<VehicleInfo> vehicleList = VehicleControl.GetVehicles(isIncoming ? cargoStationDest : cargoStationSource, TransferManager.TransferReason.None);
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
