using HarmonyLib;
using System;
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

            Config.Config.LoadAll();
            Commands.Register();

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
                harmony.PatchAll();
            else
                harmony.UnpatchAll();
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
    }
}
