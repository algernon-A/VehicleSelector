// <copyright file="CargoTruckAIPatches.cs" company="algernon (K. Algernon A. Sheppard)">
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
    /// Harmony patches for CargoTruckAI to (surprisingly) implement custom cargo train selection.
    /// </summary>
    [HarmonyPatch(typeof(CargoTruckAI))]
    [HarmonyBefore("NoBigTruck", "github.com/bloodypenguin/Skylines-CargoFerries")]
    internal static class CargoTruckAIPatches
    {
        // Barges mod GetRandomVehicleInfo delegate.
        private static BargesVehicleDelegate s_bargesVehicleDelegate;

        // AFT mod GetRandomVehicleInfo delegate.
        private static AFTVehicleDelegate s_aftVehicleDelegate;

        // NoBigTruck mod GetRandomVehicleInfo delegate.
        internal static NBTDelegate s_NBTGetRandomVehicleInfoDelegate;

        /// <summary>
        /// Delegate to Barges mod's custom GetRandomVehicleInfo method.
        /// </summary>
        /// <param name="instance">VehicleManager instance.</param>
        /// <param name="cargoStation1">Source cargo station.</param>
        /// <param name="cargoStation2">Destination cargo station.</param>
        /// <param name="service">Transfer service.</param>
        /// <param name="subService">Transfer sub-service.</param>
        /// <param name="level">Transfer level.</param>
        /// <returns>Selected VehicleInfo for spawning.</returns>
        private delegate VehicleInfo BargesVehicleDelegate(VehicleManager instance, ushort cargoStation1, ushort cargoStation2, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level);

        /// <summary>
        /// Delegate to AFT mod's custom GetRandomVehicleInfo method.
        /// </summary>
        /// <param name="instance">VehicleManager instance.</param>
        /// <param name="cargoStation1">Source cargo station.</param>
        /// <param name="cargoStation2">Destination cargo station.</param>
        /// <param name="service">Transfer service.</param>
        /// <param name="subService">Transfer sub-service.</param>
        /// <param name="level">Transfer level.</param>
        /// <returns>Selected VehicleInfo for spawning.</returns>
        private delegate VehicleInfo AFTVehicleDelegate(VehicleManager instance, ushort cargoStation1, ushort cargoStation2, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level);

        /// <summary>
        /// Delegate to NoBigTruck mod's custom GetRandomVehicleInfo method.
        /// </summary>
        /// <param name="manager">VehicleManager instance.</param>
        /// <param name="r">Randomizer reference.</param>
        /// <param name="service">Vehicle service.</param>
        /// <param name="subService">Vehicle subservice.</param>
        /// <param name="level">Vehicle level.</param>
        /// <param name="sourceBuildingId">Source cargo station.</param>
        /// <param name="targetBuildingId">Destination cargo station.</param>
        /// <param name="material">Vehicle's transfer reason.</param>
        /// <returns>Selected VehicleInfo for spawning.</returns>
        internal delegate VehicleInfo NBTDelegate(VehicleManager manager, ref Randomizer r, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, ushort sourceBuildingId, ushort targetBuildingId, TransferManager.TransferReason material);

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

                    // Get material parameter for No Big Truck mod.
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Vehicle), nameof(Vehicle.m_transferType)));

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
        /// <param name="material">Vehicle's transfer reason.</param>
        /// <returns>Vehicle prefab to spawn.</returns>
        public static VehicleInfo ChooseVehicle(VehicleManager vehicleManager, ref Randomizer r, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, ushort cargoStationSource, ushort cargoStationDest, BuildingManager buildingManager, TransferManager.TransferReason material)
        {
            // Determine transfer direction.
            Building[] buildings = buildingManager.m_buildings.m_buffer;
            BuildingInfo sourceStation = buildings[cargoStationSource].Info;
            bool isIncoming = sourceStation?.m_buildingAI is OutsideConnectionAI;

            // Check for cargo hub (ships and trains).
            TransferManager.TransferReason reason = TransferManager.TransferReason.None;
            if (subService == ItemClass.SubService.PublicTransportTrain)
            {
                BuildingInfo cargoBuilding = isIncoming ? buildings[cargoStationDest].Info : sourceStation;
                if (cargoBuilding.m_class.m_subService == ItemClass.SubService.PublicTransportShip)
                {
                    // We're using DummyTrain to differentiate trains here.
                    reason = TransferManager.TransferReason.DummyTrain;
                }
            }

            // Get any custom vehicle list for this building.
            List<VehicleInfo> vehicleList = VehicleControl.GetVehicles(isIncoming ? cargoStationDest : cargoStationSource, reason);
            if (vehicleList == null)
            {
                // Insert check for No Big Truck mod.
                if (s_NBTGetRandomVehicleInfoDelegate != null && !(sourceStation?.m_class?.name == "Helicopter Cargo Facility" || sourceStation?.m_class?.name == "Ferry Cargo Facility"))
                {
                    return s_NBTGetRandomVehicleInfoDelegate.Invoke(vehicleManager, ref r, service, subService, level, cargoStationSource, cargoStationDest, material);
                }

                // Insert check for barges mod.
                if (s_bargesVehicleDelegate != null)
                {
                    return s_bargesVehicleDelegate.Invoke(vehicleManager, cargoStationSource, cargoStationDest, service, subService, level);
                }

                // Insert check for aft mod.
                if (s_aftVehicleDelegate != null)
                {
                    return s_aftVehicleDelegate.Invoke(vehicleManager, cargoStationSource, cargoStationDest, service, subService, level);
                }

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
        /// Checks for the Barges mod / AFT mod, and No Big Truck mod, if two of them is found, creates the delegate to their custom method for CargoTruckAI.ChangeVehicleType.
        /// </summary>
        internal static void CheckMods()
        {
            CheckBargesMod();
            CheckAFTMod();
            CheckNBTMod();
        }

        /// <summary>
        /// Checks for the Barges mod, and if found, creates the delegate to its custom method for CargoTruckAI.ChangeVehicleType.
        /// </summary>
        internal static void CheckBargesMod()
        {
            try
            {
                Assembly barges = AssemblyUtils.GetEnabledAssembly("CargoFerries");
                if (barges != null)
                {
                    s_bargesVehicleDelegate = AccessTools.MethodDelegate<BargesVehicleDelegate>(AccessTools.Method(barges.GetType("CargoFerries.HarmonyPatches.CargoTruckAIPatch.ChangeVehicleTypePatch"), "GetCargoVehicleInfo"));
                    if (s_bargesVehicleDelegate != null)
                    {
                        Logging.Message("got delegate to barges mod");
                    }
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception getting delegate from barges mod");
            }
        }

        /// <summary>
        /// Checks for the AFT mod, and if found, creates the delegate to its custom method for CargoTruckAI.ChangeVehicleType.
        /// </summary>
        internal static void CheckAFTMod()
        {
            try
            {
                Assembly aft = AssemblyUtils.GetEnabledAssembly("AdditionalFreightTransporters");
                if (aft != null)
                {
                    s_aftVehicleDelegate = AccessTools.MethodDelegate<AFTVehicleDelegate>(AccessTools.Method(aft.GetType("AdditionalFreightTransporters.HarmonyPatches.CargoTruckAIPatch"), "GetCargoVehicleInfo"));
                    if (s_aftVehicleDelegate != null)
                    {
                        Logging.Message("got delegate to aft mod");
                    }
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception getting delegate from aft mod");
            }
        }

        /// <summary>
        /// Checks for the No Big Truck mod, and if found, creates the delegate to its custom method for CargoTruckAI.ChangeVehicleType.
        /// </summary>
        internal static void CheckNBTMod()
        {
            try
            {
                Assembly noBigTruck = AssemblyUtils.GetEnabledAssembly("NoBigTruck");
                if (noBigTruck != null)
                {
                    s_NBTGetRandomVehicleInfoDelegate = AccessTools.MethodDelegate<NBTDelegate>(AccessTools.Method(noBigTruck.GetType("NoBigTruck.Manager"), "GetRandomVehicleInfo"));
                    if (s_NBTGetRandomVehicleInfoDelegate != null)
                    {
                        Logging.Message("got delegate to No Big Truck mod (NoBigTruck.Manager.GetRandomVehicleInfo)");
                    }
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception getting delegate from No Big Truck mod (NoBigTruck.Manager.GetRandomVehicleInfo)");
            }
        }
    }
}
