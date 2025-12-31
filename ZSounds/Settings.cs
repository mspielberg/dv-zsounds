using System.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace DvMod.ZSounds
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        public string? version;

        [Draw("Enable logging")]
        public bool enableLogging;

        // Migration flag - true if sounds have been migrated from old structure to new
        public bool soundsMigrated = false;

        public Settings()
        {
        }

        public Settings(string? version)
        {
            this.version = version;
        }

        public void Draw()
        {
            this.Draw(Main.mod);

            // Current car info
            var currentCar = PlayerManager.Car;
            if (currentCar != null)
            {
                GUILayout.Space(5);
                GUILayout.Label($"Current Car: {currentCar.carType} ({currentCar.CarGUID})", GUILayout.ExpandWidth(false));

                if (Main.IsLoco(currentCar.carLivery))
                {
                    var isCustomized = Main.registryService?.IsCustomized(currentCar) ?? false;
                    var customSoundsCount = 0;

                    if (isCustomized)
                    {
                        var soundSet = Main.registryService?.GetSoundSet(currentCar);
                        customSoundsCount = soundSet?.sounds.Count ?? 0;
                    }

                    // Debug info when logging is enabled
                    var debugInfo = enableLogging ? $" (Customized: {isCustomized}, GUID: {currentCar.CarGUID.Substring(0, 8)}...)" : "";
                    GUILayout.Label($"Custom Sounds Applied: {customSoundsCount}{debugInfo}", GUILayout.ExpandWidth(false));

                    // Folder-based sound info
                    if (Main.loaderService != null)
                    {
                        var availableSounds = Main.loaderService.GetAvailableSoundsForTrain(currentCar.carType);
                        var folderSoundsCount = availableSounds.SelectMany(kvp => kvp.Value).Count();
                        GUILayout.Label($"Available Folder Sounds: {folderSoundsCount}", GUILayout.ExpandWidth(false));
                    }
                }
            }
            else
            {
                GUILayout.Space(5);
                GUILayout.Label("Current Car: None (enter a locomotive to manage sounds)", GUILayout.ExpandWidth(false));
            }
        }

        public void OnChange() { }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
