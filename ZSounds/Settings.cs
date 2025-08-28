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

            GUILayout.Space(10);
            GUILayout.Label("ZSounds Actions:", GUILayout.ExpandWidth(false));

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Update Soundlist", GUILayout.Width(120)))
            {
                try
                {
                    Main.soundLoader?.LoadAllSounds();
                    Main.mod?.Logger.Log("Sounds reloaded successfully from folder structure");

                    // Reinitialize CommsRadio with updated sound list
                    try
                    {
                        CommsRadioSoundSwitcherAPI.Reinitialize();
                        Main.mod?.Logger.Log("CommsRadio reinitialized with updated sound list");
                    }
                    catch (System.Exception apiEx)
                    {
                        Main.mod?.Logger.Warning($"Could not reinitialize CommsRadio (CommsRadioAPI may not be available): {apiEx.Message}");
                    }
                }
                catch (System.Exception ex)
                {
                    Main.mod?.Logger.Error($"Failed to reload sounds: {ex.Message}");
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();


            // Removed reset button for now
            GUILayout.Label("Reset: For reliable sound reset, restart the game.", GUILayout.Width(300));
            GUILayout.EndHorizontal();

            // Current car info
            var currentCar = PlayerManager.Car;
            if (currentCar != null)
            {
                GUILayout.Space(5);
                GUILayout.Label($"Current Car: {currentCar.carType} ({currentCar.CarGUID})", GUILayout.ExpandWidth(false));

                if (Main.HasHorn(currentCar.carType))
                {
                    var isCustomized = Registry.IsCustomized(currentCar);
                    var customSoundsCount = 0;

                    if (isCustomized)
                    {
                        var soundSet = Registry.Get(currentCar);
                        customSoundsCount = soundSet.sounds.Count;
                    }

                    // Debug info when logging is enabled
                    var debugInfo = enableLogging ? $" (Customized: {isCustomized}, GUID: {currentCar.CarGUID.Substring(0, 8)}...)" : "";
                    GUILayout.Label($"Custom Sounds Applied: {customSoundsCount}{debugInfo}", GUILayout.ExpandWidth(false));

                    // Folder-based sound info
                    if (Main.soundLoader != null)
                    {
                        var availableSounds = Main.soundLoader.GetAvailableSoundsForTrain(currentCar.carType);
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