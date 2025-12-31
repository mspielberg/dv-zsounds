using System;
using DV.ThingTypes;
using DvMod.ZSounds.Patches;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using DvMod.ZSounds.SoundHandler;
using DerailValleyModToolbar;

namespace DvMod.ZSounds
{
    [EnableReloading]
    public static class Main
    {
        public static bool enabled = true;
        public static Settings settings = new Settings();
        public static UnityModManager.ModEntry? mod;


        // New service architecture
        public static SoundDiscovery? discoveryService;
        public static SoundLoader? loaderService;
        public static SoundApplicator? applicatorService;
        public static SoundRestorator? restoratorService;
        public static SoundRegistry? registryService;
        public static VanillaAudioCache? vanillaCache;


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

            // Initialize new service architecture
            modEntry.Logger.Log("Initializing ZSounds service architecture...");

            // Service 1: Sound Discovery (no dependencies)
            discoveryService = new SoundDiscovery();
            modEntry.Logger.Log("- SoundDiscovery initialized");

            // Perform migration if needed (must happen after discovery but before loading)
            if (!settings.soundsMigrated)
            {
                modEntry.Logger.Log("Checking for sound folder migration...");
                var migration = new SoundMigration(modEntry.Path);

                if (migration.NeedsMigration())
                {
                    modEntry.Logger.Log("Old sound folder structure detected. Starting migration...");
                    try
                    {
                        migration.Migrate();
                        settings.soundsMigrated = true;
                        settings.Save(modEntry);
                        modEntry.Logger.Log("Migration completed successfully and saved to settings");
                    }
                    catch (Exception migrationEx)
                    {
                        modEntry.Logger.Error($"Migration failed: {migrationEx.Message}");
                        modEntry.Logger.Error("You may need to manually organize your sound files or restore from backup");
                    }
                }
                else
                {
                    modEntry.Logger.Log("No migration needed - using current structure");
                    settings.soundsMigrated = true;
                    settings.Save(modEntry);
                }
            }

            // Service 2: Sound Loader (no dependencies)
            loaderService = new SoundLoader(modEntry.Path);
            loaderService.LoadAllSounds();
            modEntry.Logger.Log("- SoundLoader initialized and sounds loaded");

            // Service 3: Sound Applicator (depends on: SoundDiscovery, SoundLoader)
            applicatorService = new SoundApplicator(discoveryService, loaderService);
            modEntry.Logger.Log("- SoundApplicator initialized");

            // Service 4: Sound Restorator (depends on: SoundDiscovery)
            restoratorService = new SoundRestorator(discoveryService);
            modEntry.Logger.Log("- SoundRestorator initialized");

            // Service 5: Vanilla Audio Cache (no dependencies)
            vanillaCache = new VanillaAudioCache();
            modEntry.Logger.Log("- VanillaAudioCache initialized");

            // Service 6: Sound Registry (depends on: SoundLoader, SoundDiscovery)
            registryService = new SoundRegistry(modEntry.Path, loaderService, discoveryService);
            registryService.LoadAllStates();
            modEntry.Logger.Log("- SoundRegistry initialized and states loaded");


            // Initialize CommsRadio API integration
            try
            {
                CommsRadioSoundSwitcherAPI.Initialize();
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
                catch (Exception ex)
                {
                    modEntry.Logger.Warning($"Failed to reinitialize CommsRadio integration: {ex.Message}");
                }

                // Subscribe to world loading finished event
                WorldStreamingInit.LoadingFinished += OnWorldLoadingFinished;

                modEntry.Logger.Log("ZSounds mod enabled successfully");
            }
            else
            {
                // Unsubscribe from world loading event
                WorldStreamingInit.LoadingFinished -= OnWorldLoadingFinished;

                // Destroy the in-game UI button
                DestroySoundManagerButton();

                harmony.UnpatchAll(modEntry.Info.Id);

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

        private static void CreateSoundManagerButton()
        {
            try
            {
                if (mod != null)
                {
                    // Register with ModToolbarAPI for the main panel
                    ModToolbarAPI.Register(mod).AddPanelControl(
                        label: "ZSounds",
                        icon: null, // Optional icon
                        tooltip: "Open Zeibach's Sounds UI",
                        onGUIContent: _ =>
                        {
                            UI.SoundManagerUI.Instance.OnGUI();
                        },
                        title: "ZSounds UI"
                    ).Finish();

                    mod.Logger.Log("Sound Manager UI button registered with ModToolbar");
                }
            }
            catch (Exception ex)
            {
                mod?.Logger.Error($"Failed to register Sound Manager button with ModToolbar: {ex.Message}");
            }
        }

        private static void DestroySoundManagerButton()
        {
            try
            {
                if (mod != null)
                {
                    ModToolbarAPI.Unregister(mod);
                    mod.Logger.Log("Sound Manager UI button unregistered from ModToolbar");
                }
            }
            catch (Exception ex)
            {
                mod?.Logger.Warning($"Failed to cleanup UI components: {ex.Message}");
            }
        }

        public static bool IsLoco(TrainCarLivery livery)
        {
            return CarTypes.IsLocomotive(livery);
        }

        private static void OnWorldLoadingFinished()
        {
            try
            {
                mod?.Logger.Log("World loading finished - scanning audio prefabs and applying saved locomotive sounds");

                // Scan all locomotive audio prefabs to discover sounds (new service)
                discoveryService?.ScanAllLocomotives();

                // Create folder structure for discovered sounds
                if (mod != null)
                {
                    DynamicFolderCreator.CreateFolderStructure(mod.Path);
                    DynamicFolderCreator.ValidateFolderStructure(mod.Path);
                }

                // Reload sounds after folder structure is created
                loaderService?.LoadAllSounds();

                // Apply saved sounds
                registryService?.ApplySavedSounds();


                CreateSoundManagerButton();

                mod?.Logger.Log("World loading initialization complete");
            }
            catch (Exception ex)
            {
                mod?.Logger.Error($"Failed to apply saved sounds on world load: {ex.Message}");
            }
        }

    }
}
