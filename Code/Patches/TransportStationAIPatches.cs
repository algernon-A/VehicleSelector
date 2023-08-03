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
    using AlgernonCommons.Patching;
    using ColossalFramework.Math;
    using HarmonyLib;

    /// <summary>
    /// Harmony patches to TransportStationAI to implement vehicle selection.
    /// Runs before Transport Lines Manager, pre-empting TLM patching.
    /// </summary>
    [HarmonyPatch(typeof(TransportStationAI))]
    [HarmonyBefore("com.klyte.redirectors.TLM", "com.redirectors.TLM")]
    public static class TransportStationAIPatches
    {
        // TLM mod TryGetRandomVehicle delegate.
        private static TLMVehicleDelegate s_tlmVehicleDelegate;

        // TLM mod TryGetRandomVehicleStation method.
        private static MethodInfo s_tlmTryGetRandomVehicleStation;

        /// <summary>
        /// Delegate to Transport Line Manager mod's custom GetRandomVehicleInfo method for TransportStationAI.
        /// </summary>
        /// <param name="vm">VehicleManager instance.</param>
        /// <param name="r">Randomizer instance.</param>
        /// <param name="service">Transfer service.</param>
        /// <param name="subService">Transfer sub-service.</param>
        /// <param name="level">Transfer level.</param>
        /// <param name="type">Transfer vehicle type.</param>
        /// <returns>Selected VehicleInfo for spawning.</returns>
        private delegate VehicleInfo TLMVehicleDelegate(VehicleManager vm, ref Randomizer r, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, VehicleInfo.VehicleType type);

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
        /// Checks for the Transport Lines Manager mod, and if found, creates the delegate to its custom method for TryGetRandomVehicleStation.
        /// </summary>
        internal static void CheckMods()
        {
            try
            {
                Assembly tlm = AssemblyUtils.GetEnabledAssembly("TransportLinesManager");
                if (tlm != null)
                {
                    Logging.Message("found TransportLinesManager assembly");

                    MethodInfo tlmMethod = AccessTools.Method(tlm.GetType("Klyte.TransportLinesManager.Overrides.OutsideConnectionOverrides"), "TryGetRandomVehicleStation");

                    if (tlmMethod == null)
                    {
                        Logging.Message("using new TransportLinesManager");
                        tlmMethod = AccessTools.Method(tlm.GetType("TransportLinesManager.Overrides.TransportStationAIOverrides"), "TryGetRandomVehicleStation");

                        // Get random station method.
                        s_tlmTryGetRandomVehicleStation = AccessTools.Method(Type.GetType("TransportLinesManager.Overrides.TransportStationAIOverrides,TransportLinesManager"), "TryGetRandomVehicleStation");

                        // Transpile new TLM methods.
                        if (s_tlmTryGetRandomVehicleStation != null)
                        {
                            MethodInfo tlmCreateVehicle = AccessTools.Method(tlm.GetType("TransportLinesManager.Overrides.TransportStationAIOverrides"), "CreateIncomingVehicle");
                            PatcherManager<PatcherBase>.Instance.TranspileMethod(tlmCreateVehicle, AccessTools.Method(typeof(TransportStationAIPatches), nameof(TransportStationTranspiler)));

                            tlmCreateVehicle = AccessTools.Method(tlm.GetType("TransportLinesManager.Overrides.TransportStationAIOverrides"), "CreateOutgoingVehicle");
                            PatcherManager<PatcherBase>.Instance.TranspileMethod(tlmCreateVehicle, AccessTools.Method(typeof(TransportStationAIPatches), nameof(TransportStationTranspiler)));
                        }
                    }

                    if (tlmMethod != null)
                    {
                        s_tlmVehicleDelegate = AccessTools.MethodDelegate<TLMVehicleDelegate>(tlmMethod);
                        if (s_tlmVehicleDelegate != null)
                        {
                            Logging.Message("got delegate to TLM");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception getting delegate from TLM");
            }
        }

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

                // If this instruction calls the GetPrimaryRandomVehicleInfo method, or the TLM target method, then replace it with a call to our custom method.
                if (instruction.Calls(getRandomVehicleType) || (s_tlmTryGetRandomVehicleStation != null && instruction.Calls(s_tlmTryGetRandomVehicleStation)))
                {
                    // Add buildingID and material params to call.
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    instruction = new CodeInstruction(OpCodes.Call, chooseVehicleType);
                    Logging.Message("transpiled");
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

                // Insert check for TLM.
                if (s_tlmVehicleDelegate != null)
                {
                    return s_tlmVehicleDelegate.Invoke(vehicleManager, ref r, service, subService, level, type);
                }

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