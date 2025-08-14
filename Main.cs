using System;
using System.Linq;

using HarmonyLib;

using UnityEngine;

using UnityModManagerNet;

namespace DvMod.ZSounds
{
    [EnableReloading]
    public static class Main
    {
        public static bool enabled = true;
        public static Settings settings = new Settings();
        public static UnityModManager.ModEntry? mod;
        public static FolderSoundLoader? soundLoader;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;

            try
            {
                var loaded = Settings.Load<Settings>(modEntry);
                modEntry.Logger.Log($"Loaded settings version: {loaded.version}");
                if (loaded.version == modEntry.Info.Version)
                {
                    settings = loaded;
                }
                else
                {
                    settings = new Settings(modEntry.Info.Version);
                    modEntry.Logger.Log($"Reset to default settings for version {settings.version}");
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                settings = new Settings(modEntry.Info.Version);
            }

            modEntry.OnGUI = OnGui;
            modEntry.OnSaveGUI = OnSaveGui;
            modEntry.OnToggle = OnToggle;

            // Initialize folder-based sound loader
            soundLoader = new FolderSoundLoader(modEntry.Path);
            soundLoader.LoadAllSounds();

            // Initialize CommsRadio API integration
            try
            {
                CommsRadioSoundSwitcherAPI.Initialize();
            }
            catch (System.IO.FileNotFoundException ex) when (ex.Message.Contains("CommsRadioAPI"))
            {
                modEntry.Logger.Warning("CommsRadioAPI not found - CommsRadio integration will not be available");
            }
            catch (Exception ex)
            {
                modEntry.Logger.Warning($"Failed to initialize CommsRadio integration: {ex.Message}");
            }

            return true;
        }

        private static void OnGui(UnityModManager.ModEntry modEntry)
        {
            settings.Draw();
        }

        private static void OnSaveGui(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            var harmony = new Harmony(modEntry.Info.Id);
            if (value)
            {
                modEntry.Logger.Log("ZSounds mod enabling...");
                modEntry.Logger.Log($"AudioUtils Defaults count at startup: {AudioUtils.GetDefaultsCount()}");

                try
                {
                    harmony.PatchAll();
                    modEntry.Logger.Log("Harmony patches applied successfully");

                    // Clear caches on startup
                    LayeredAudioSetPitchPatch.ClearCaches();
                    AudioSourcePitchPatch.ClearCaches();
                }
                catch (Exception ex)
                {
                    modEntry.Logger.Error($"Failed to apply Harmony patches: {ex}");
                }

                // Reinitialize CommsRadio API integration
                try
                {
                    CommsRadioSoundSwitcherAPI.Reinitialize();
                }
                catch (System.IO.FileNotFoundException ex) when (ex.Message.Contains("CommsRadioAPI"))
                {
                    modEntry.Logger.Warning("CommsRadioAPI not found - CommsRadio integration will not be available");
                }
                catch (Exception ex)
                {
                    modEntry.Logger.Warning($"Failed to reinitialize CommsRadio integration: {ex.Message}");
                }

                modEntry.Logger.Log("ZSounds mod enabled successfully");
            }
            else
            {
                harmony.UnpatchAll();

                // Clear performance caches
                LayeredAudioSetPitchPatch.ClearCaches();
                AudioSourcePitchPatch.ClearCaches();

                // Cleanup CommsRadio API integration
                try
                {
                    CommsRadioSoundSwitcherAPI.Cleanup();
                }
                catch (Exception ex)
                {
                    modEntry.Logger.Warning($"Failed to cleanup CommsRadio integration: {ex.Message}");
                }
            }

            return true;
        }

        public static void DebugLog(Func<string> message)
        {
            if (settings.enableLogging && mod != null)
                mod.Logger.Log(message());
        }

        public static void DebugLog(TrainCar car, Func<string> message)
        {
            if (car == PlayerManager.Car)
                DebugLog(message);
        }

        public static bool HasHorn(DV.ThingTypes.TrainCarType carType)
        {
            return AudioMapper.Mappers.ContainsKey(carType);
        }

    }
}