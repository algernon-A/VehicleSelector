// <copyright file="ModSettings.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace VehicleSelector
{
    using System.IO;
    using System.Xml.Serialization;
    using AlgernonCommons.Keybinding;
    using AlgernonCommons.XML;
    using UnityEngine;

    /// <summary>
    /// Global mod settings.
    /// </summary>
    [XmlRoot("VehicleSelector")]
    public class ModSettings : SettingsXMLBase
    {
        /// <summary>
        /// Copy key.
        /// </summary>
        [XmlIgnore]
        public static readonly Keybinding KeyCopy = new Keybinding(KeyCode.C, true, false, false);

        /// <summary>
        /// Paste key.
        /// </summary>
        [XmlIgnore]
        public static readonly Keybinding KeyPaste = new Keybinding(KeyCode.V, true, false, false);

        /// <summary>
        /// Gets the settings file name.
        /// </summary>
        [XmlIgnore]
        private static readonly string SettingsFileName = Path.Combine(ColossalFramework.IO.DataLocation.localApplicationData, "VehicleSelector.xml");

        /// <summary>
        /// Loads settings from file.
        /// </summary>
        internal static void Load() => XMLFileUtils.Load<ModSettings>(SettingsFileName);

        /// <summary>
        /// Saves settings to file.
        /// </summary>
        internal static void Save() => XMLFileUtils.Save<ModSettings>(SettingsFileName);
    }
}