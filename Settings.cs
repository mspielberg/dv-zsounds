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


            if (GUILayout.Button("Reset Current Car", GUILayout.Width(120)))
            {
                var car = PlayerManager.Car;
                if (car == null)
                {
                    Main.mod?.Logger.Log("No car selected. Enter a locomotive to reset its sounds.");
                }
                else if (!Main.HasHorn(car.carType))
                {
                    Main.mod?.Logger.Log("Current car is not a locomotive. Only locomotive sounds can be reset.");
                }
                else
                {
                    try
                    {
                        Main.mod?.Logger.Log($"Resetting {car.carType} sounds to default...");

                        var hasValidDefaults = false;
                        foreach (var soundType in SoundTypes.audioClipsSoundTypes.Concat(SoundTypes.layeredAudioSoundTypes))
                        {
                            var key = new AudioUtils.DefaultKey(car.carType, soundType);
                            if (AudioUtils.HasDefaults(key))
                            {
                                hasValidDefaults = true;
                            }
                        }

                        // Clear customization tracking and sound set
                        Registry.soundSets.Remove(car.logicCar.carGuid);
                        Registry.ClearCustomization(car);

                        if (hasValidDefaults)
                        {
                            Main.mod?.Logger.Log("Using stored defaults to reset sounds...");
                            AudioUtils.ResetAllToDefaults(car);

                            // After resetting to defaults, reapply sounds from cleared Registry
                            // This causes game to use original audio with default settings
                            Main.mod?.Logger.Log("Reapplying sounds from cleared Registry to restore game defaults...");
                            var emptySoundSet = Registry.Get(car);
                            AudioUtils.Apply(car, emptySoundSet);
                        }
                        else
                        {
                            Main.mod?.Logger.Warning("No stored defaults found - this shouldn't happen with comprehensive defaults capture!");
                            Main.mod?.Logger.Log("Clearing customizations only - game should restore originals automatically");
                            // When no defaults exist, clearing customizations allows game
                            // to restore original sounds automatically
                        }

                        Main.mod?.Logger.Log($"Successfully reset {car.carType} sounds to default");
                    }
                    catch (System.Exception ex)
                    {
                        Main.mod?.Logger.Error($"Failed to reset car sounds: {ex.Message}");
                        Main.mod?.Logger.Error($"Stack trace: {ex.StackTrace}");
                    }
                }
            }

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