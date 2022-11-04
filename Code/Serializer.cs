// <copyright file="Serializer.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace VehicleSelector
{
    using System;
    using System.IO;
    using AlgernonCommons;
    using ICities;

    /// <summary>
    /// Handles savegame data saving and loading.
    /// </summary>
    public class Serializer : SerializableDataExtensionBase
    {
        // Current data version.
        private const int DataVersion = 0;

        // Unique data ID.
        private readonly string dataID = "VehicleSelector";

        /// <summary>
        /// Serializes data to the savegame.
        /// Called by the game on save.
        /// </summary>
        public override void OnSaveData()
        {
            base.OnSaveData();

            using (MemoryStream stream = new MemoryStream())
            {
                // Serialise savegame settings.
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    // Write version.
                    writer.Write(DataVersion);

                    // Serialize vehicle data.
                    VehicleControl.Serialize(writer);

                    // Write to savegame.
                    serializableDataManager.SaveData(dataID, stream.ToArray());

                    Logging.Message("wrote ", stream.Length);
                }
            }
        }

        /// <summary>
        /// Deserializes data from a savegame (or initialises new data structures when none available).
        /// Called by the game on load (including a new game).
        /// </summary>
        public override void OnLoadData()
        {
            base.OnLoadData();

            // Read data from savegame.
            byte[] data = serializableDataManager.LoadData(dataID);

            // Check to see if anything was read.
            if (data != null && data.Length != 0)
            {
                // Data was read - go ahead and deserialise.
                using (MemoryStream stream = new MemoryStream(data))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        // Read version.
                        int version = reader.ReadInt32();
                        Logging.Message("found data version ", version);

                        // Deserialize vehicle settings.
                        VehicleControl.Deserialize(reader);

                        Logging.Message("read ", stream.Length);
                    }
                }

                // If we've read vehicle selector data, ignore any Transfer Controller data.
                return;
            }
            else
            {
                // No data read.
                Logging.Message("no Vehicle Selector data read");
            }

            try
            {
                // Try to read Transfer Controller data from savegame.
                data = serializableDataManager.LoadData("TransferController");

                // Check to see if anything was read.
                if (data != null && data.Length != 0)
                {
                    // Data was read - go ahead and deserialise.
                    using (MemoryStream stream = new MemoryStream(data))
                    {
                        using (BinaryReader reader = new BinaryReader(stream))
                        {
                            // Read version.
                            int version = reader.ReadInt32();
                            if (version >= 2 && version <= 4)
                            {
                                Logging.Message("found Transfer Controller data version ", version);

                                // Skip TC building settings.
                                SkipTCBuildings(reader, version);

                                // Skip TC warehouse settings.
                                SkipTCWarehouses(reader);

                                // Deserialize vehicle settings.
                                VehicleControl.Deserialize(reader);

                                Logging.Message("read Transfer Controller data ", stream.Length);
                            }
                        }
                    }
                }
                else
                {
                    // No data read.
                    Logging.Message("no Transfer Controller data read");
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception reading Transfer Controller data");
            }
        }

        /// <summary>
        /// Skips Transfer Controller building data when deserializing.
        /// </summary>
        /// <param name="reader">Reader to deserialize from.</param>
        /// <param name="dataVersion">Data version.</param>
        private void SkipTCBuildings(BinaryReader reader, int dataVersion)
        {
            // Iterate through each entry read.
            int numEntries = reader.ReadInt32();
            for (int i = 0; i < numEntries; ++i)
            {
                // Dictionary entry key.
                reader.ReadUInt32();

                // Legacy dataversion.
                if (dataVersion <= 2)
                {
                    // Discard old recordnumber byte.
                    reader.ReadByte();

                    // Read restriction flags.
                    reader.ReadByte();

                    // Read reason,
                    reader.ReadInt32();
                }
                else
                {
                    // Read flags.
                    reader.ReadUInt32();
                }

                // Deserialize district entries for this building.
                int districtCount = reader.ReadInt32();

                // If serialized count is zero, there's nothing further to deserialize.
                if (districtCount > 0)
                {
                    // Deserialize district entries.
                    for (int j = 0; j < districtCount; ++j)
                    {
                        reader.ReadInt32();
                    }
                }

                // Deserialize building entries for this building.
                int buildingCount = reader.ReadInt32();

                // If serialized count is zero, there's nothing further to deserialize.
                if (buildingCount > 0)
                {
                    // Deserialise building entries.
                    for (int j = 0; j < buildingCount; ++j)
                    {
                        reader.ReadUInt32();
                    }
                }
            }
        }

        /// <summary>
        /// Skips Transfer Controller warehouse data when deserializing.
        /// </summary>
        /// <param name="reader">Binary reader instance to deserialize from.</param>
        private void SkipTCWarehouses(BinaryReader reader)
        {
            // Iterate through each entry read.
            int numEntries = reader.ReadInt32();
            for (int i = 0; i < numEntries; ++i)
            {
                // Dictionary entry key.
                reader.ReadUInt32();

                // Deserialize basic building record fields.
                reader.ReadByte();
            }
        }
    }
}