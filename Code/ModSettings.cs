using System.IO;

using UnityEngine;

namespace EchKode.PBMods.TimelinePopups
{
	partial class ModLink
	{
		internal sealed class ModSettings
		{
			[System.Flags]
			internal enum DisplayElement
			{
				None = 0,
				TargetPopup = 1,
				EquipmentInfoPopup = 2,
				Range = 4,
			}
#pragma warning disable CS0649
			public bool enableLogging;
			public bool showPopupsForShields;
			public bool showPopupsOnDrag;
			public DisplayElement displayElements = DisplayElement.TargetPopup | DisplayElement.EquipmentInfoPopup;
			public bool enableTargetPopupInSimulation;  // XXX experimental
#pragma warning restore CS0649
		}

		internal static ModSettings Settings;

		internal static void LoadSettings()
		{
			var settingsPath = Path.Combine(modPath, "settings.yaml");
			Settings = UtilitiesYAML.ReadFromFile<ModSettings>(settingsPath, false);
			if (Settings == null)
			{
				Settings = new ModSettings();

				Debug.LogFormat(
					"Mod {0} ({1}) no settings file found, using defaults | path: {2}",
					modIndex,
					modId,
					settingsPath);
			}

			if (Settings.enableLogging)
			{
				Debug.LogFormat(
					"Mod {0} ({1}) display elements: {2}",
					modIndex,
					modId,
					Settings.displayElements);
			}
		}
	}
}
