using System;
using System.Linq;
using DV.ModularAudioCar;
using DV.Simulation.Cars;
using DV.Simulation.Controllers;
using DV.Simulation.Ports;
using UnityEngine;

namespace DvMod.ZSounds.SoundHandler
{
    /// <summary>
    /// Service for restoring locomotive sounds to their original game defaults.
    /// Consolidates functionality from AudioUtils.cs GetOriginal*FromPrefab() methods and Registry.ResetToDefaults().
    /// </summary>
    public class SoundRestorator
    {
        private readonly SoundDiscovery _soundDiscovery;

        public SoundRestorator(SoundDiscovery soundDiscovery)
        {
            _soundDiscovery = soundDiscovery;
        }

        #region Public API - Main Restoration Methods

        /// <summary>
        /// Resets all sounds for a car to their default values.
        /// Clears any custom sound mappings and restores the original game sounds.
        /// </summary>
        public void RestoreAllSounds(TrainCar car)
        {
            Main.DebugLog(() => $"SoundRestorator: Restoring all sounds to defaults for {car.carType}");

            var trainAudio = GetTrainAudio(car);
            if (trainAudio == null)
            {
                Main.mod?.Logger.Warning($"SoundRestorator: Could not get TrainAudio for car {car.ID}");
                return;
            }

            // Restore all supported sound types
            foreach (var soundType in SoundTypes.audioClipsSoundTypes)
            {
                RestoreAudioClipArraySound(car, trainAudio, soundType);
            }

            foreach (var soundType in SoundTypes.layeredAudioSoundTypes)
            {
                RestoreLayeredAudioSound(car, trainAudio, soundType);
            }

            Main.DebugLog(() => $"SoundRestorator: Completed restoration for {car.carType}");
        }

        /// <summary>
        /// Restores a specific sound type to its default value.
        /// </summary>
        public void RestoreSound(TrainCar car, SoundType soundType)
        {
            Main.DebugLog(() => $"SoundRestorator: Restoring {soundType} to default for {car.carType}");

            var trainAudio = GetTrainAudio(car);
            if (trainAudio == null)
            {
                Main.mod?.Logger.Warning($"SoundRestorator: Could not get TrainAudio for car {car.ID}");
                return;
            }

            if (SoundTypes.audioClipsSoundTypes.Contains(soundType))
            {
                RestoreAudioClipArraySound(car, trainAudio, soundType);
            }
            else if (SoundTypes.layeredAudioSoundTypes.Contains(soundType))
            {
                RestoreLayeredAudioSound(car, trainAudio, soundType);
            }
        }

        #endregion

        #region Public API - Get Original Components

        /// <summary>
        /// Gets the original AudioClip array from the locomotive prefab.
        /// </summary>
        public AudioClip[]? GetOriginalClips(TrainCar car, SoundType soundType)
        {
            try
            {
                var carType = car.carType;
                Main.DebugLog(() => $"SoundRestorator.GetOriginalClips: Starting search for {carType}/{soundType}");

                var audioPrefab = GetAudioPrefab(car);
                if (audioPrefab == null)
                    return null;

                var path = _soundDiscovery.GetClipName(carType, soundType);
                if (path == null)
                {
                    Main.mod?.Logger.Warning($"SoundRestorator.GetOriginalClips: No sound mapping found for {carType}/{soundType}");
                    return null;
                }

                Main.DebugLog(() => $"SoundRestorator.GetOriginalClips: Searching for path '{path}' in prefab hierarchy");

                // Search for AudioClipPortReader component in the GameObject hierarchy
                var portReaders = audioPrefab.GetComponentsInChildren<AudioClipPortReader>(includeInactive: true);
                Main.DebugLog(() => $"SoundRestorator.GetOriginalClips: Found {portReaders.Length} AudioClipPortReader components in prefab");

                // Try to find by matching clip name
                AudioClipPortReader? prefabPortReader = null;
                foreach (var reader in portReaders)
                {
                    if (reader.clips != null && reader.clips.Length > 0)
                    {
                        foreach (var clip in reader.clips)
                        {
                            if (clip != null && string.Equals(clip.name, path, StringComparison.OrdinalIgnoreCase))
                            {
                                prefabPortReader = reader;
                                Main.DebugLog(() => $"SoundRestorator.GetOriginalClips: Found match by clip name: {clip.name}");
                                break;
                            }
                        }
                        if (prefabPortReader != null) break;
                    }
                }

                if (prefabPortReader == null)
                {
                    Main.mod?.Logger.Warning($"SoundRestorator.GetOriginalClips: Could not find AudioClipPortReader with path '{path}' for {carType}/{soundType}");
                    return null;
                }

                Main.DebugLog(() => $"SoundRestorator.GetOriginalClips: Found AudioClipPortReader at '{GetGameObjectPath(prefabPortReader.gameObject)}'");

                if (prefabPortReader.clips == null || prefabPortReader.clips.Length == 0)
                {
                    Main.mod?.Logger.Warning($"SoundRestorator.GetOriginalClips: AudioClipPortReader.clips is NULL or empty for {carType}/{soundType}");
                    return null;
                }

                Main.DebugLog(() => $"SoundRestorator.GetOriginalClips: Successfully found {prefabPortReader.clips.Length} clips for {carType}/{soundType}");
                return prefabPortReader.clips;
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"SoundRestorator.GetOriginalClips: Error getting clips from prefab for {car.carType}/{soundType}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the original LayeredAudio component from the locomotive prefab.
        /// This allows us to read the true vanilla values for pitch, curves, etc.
        /// </summary>
        public LayeredAudio? GetOriginalLayeredAudio(TrainCar car, SoundType soundType)
        {
            try
            {
                var carType = car.carType;
                Main.DebugLog(() => $"SoundRestorator.GetOriginalLayeredAudio: Starting search for {carType}/{soundType}");

                var audioPrefab = GetAudioPrefab(car);
                if (audioPrefab == null)
                    return null;

                // Note: GetClipName() actually returns the GameObject name, not the AudioClip name
                // For LayeredAudio, this is the name of the GameObject containing the LayeredAudio component
                var gameObjectName = _soundDiscovery.GetClipName(carType, soundType);
                if (gameObjectName == null)
                {
                    Main.DebugLog(() => $"SoundRestorator.GetOriginalLayeredAudio: No sound mapping found for {carType}/{soundType}");
                    return null;
                }

                Main.DebugLog(() => $"SoundRestorator.GetOriginalLayeredAudio: Searching for GameObject '{gameObjectName}' in prefab hierarchy");

                // Search for LayeredAudio components in the prefab
                var layeredAudios = audioPrefab.GetComponentsInChildren<LayeredAudio>(includeInactive: true);
                Main.DebugLog(() => $"SoundRestorator.GetOriginalLayeredAudio: Found {layeredAudios.Length} LayeredAudio components in prefab");

                // Find by matching GameObject name
                foreach (var audio in layeredAudios)
                {
                    if (string.Equals(audio.gameObject.name, gameObjectName, StringComparison.OrdinalIgnoreCase))
                    {
                        Main.DebugLog(() => $"SoundRestorator.GetOriginalLayeredAudio: Found match at '{GetGameObjectPath(audio.gameObject)}'");
                        return audio;
                    }
                }

                Main.DebugLog(() => $"SoundRestorator.GetOriginalLayeredAudio: Could not find LayeredAudio with GameObject name '{gameObjectName}' for {carType}/{soundType}");
                return null;
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"SoundRestorator.GetOriginalLayeredAudio: Error getting LayeredAudio from prefab for {car.carType}/{soundType}: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Private - Restoration Application

        private void RestoreAudioClipArraySound(TrainCar car, TrainAudio trainAudio, SoundType soundType)
        {
            var portReader = _soundDiscovery.GetAudioClipPortReader(trainAudio, soundType);
            if (portReader == null)
                return;

            Main.DebugLog(() => $"SoundRestorator: Restoring AudioClip[] for {soundType}");

            // Try to restore from cache first (includes pitch, volume, and clips)
            bool restoredFromCache = false; //Main.vanillaCache?.RestoreCached(car, soundType, portReader) ?? false;

            if (restoredFromCache)
            {
                Main.DebugLog(() => $"SoundRestorator: Restored AudioClip settings from cache for {soundType}");
            }
            else
            {
                // Fallback: restore clips from prefab
                var originalClips = GetOriginalClips(car, soundType);
                if (originalClips != null && originalClips.Length > 0)
                {
                    portReader.clips = originalClips;
                    Main.DebugLog(() => $"SoundRestorator: Restored {originalClips.Length} original clip(s) for {soundType} (no cache available)");
                }
                else
                {
                    Main.mod?.Logger.Warning($"SoundRestorator: Could not restore original clips for {car.carType}/{soundType}");
                }
            }
        }

        private void RestoreLayeredAudioSound(TrainCar car, TrainAudio trainAudio, SoundType soundType)
        {
            var layeredAudio = _soundDiscovery.GetLayeredAudio(trainAudio, soundType);
            if (layeredAudio == null || layeredAudio.layers == null || layeredAudio.layers.Length == 0)
                return;

            Main.DebugLog(() => $"SoundRestorator: Restoring LayeredAudio for {soundType}");

            // Log BEFORE state for detailed debugging
            Main.DebugLog(() =>
            {
                if (layeredAudio.layers.Length > 0)
                {
                    var layer0 = layeredAudio.layers[0];
                    return $"SoundRestorator: BEFORE restoration for {soundType}: " +
                           $"minPitch={layeredAudio.minPitch}, maxPitch={layeredAudio.maxPitch}, startPitch={layer0.startPitch}, " +
                           $"clip={layer0.source?.clip?.name ?? "null"}, " +
                           $"pitch={layer0.source?.pitch ?? 0f}, volume={layer0.source?.volume ?? 0f}, " +
                           $"pitchCurve keys={layer0.pitchCurve?.keys.Length ?? 0}, volumeCurve keys={layer0.volumeCurve?.keys.Length ?? 0}";
                }
                return $"SoundRestorator: BEFORE restoration for {soundType}: no layers";
            });
            
            // Log full curve details for chuff sounds
            if (IsChuffSoundType(soundType) && layeredAudio.layers.Length > 0)
            {
                var layer0 = layeredAudio.layers[0];
                LogCurveDetails("BEFORE", layer0.pitchCurve, "pitchCurve");
                LogCurveDetails("BEFORE", layer0.volumeCurve, "volumeCurve");
            }

            // Stop playing audio before restoring
            var mainLayer = layeredAudio.layers[0];
            bool wasPlaying = mainLayer.source.isPlaying;
            if (wasPlaying)
            {
                mainLayer.source.Stop();
                layeredAudio.Set(0f);
            }

            // Get the original LayeredAudio from the prefab to copy vanilla values
            var prefabLayeredAudio = GetOriginalLayeredAudio(car, soundType);
            if (prefabLayeredAudio == null)
            {
                Main.mod?.Logger.Warning($"SoundRestorator: Could not find LayeredAudio in prefab for {car.carType}/{soundType}");
                return;
            }

            Main.DebugLog(() => $"SoundRestorator: Found prefab LayeredAudio for {soundType}");

            // Log prefab values for comparison
            Main.DebugLog(() =>
            {
                if (prefabLayeredAudio.layers != null && prefabLayeredAudio.layers.Length > 0)
                {
                    var prefabLayer0 = prefabLayeredAudio.layers[0];
                    return $"SoundRestorator: PREFAB values for {soundType}: " +
                           $"minPitch={prefabLayeredAudio.minPitch}, maxPitch={prefabLayeredAudio.maxPitch}, startPitch={prefabLayer0.startPitch}, " +
                           $"clip={prefabLayer0.source?.clip?.name ?? "null"}, " +
                           $"pitchCurve keys={prefabLayer0.pitchCurve?.keys.Length ?? 0}, volumeCurve keys={prefabLayer0.volumeCurve?.keys.Length ?? 0}";
                }
                return $"SoundRestorator: PREFAB values for {soundType}: no layers";
            });
            
            // Log full curve details for chuff sounds
            if (IsChuffSoundType(soundType) && prefabLayeredAudio.layers != null && prefabLayeredAudio.layers.Length > 0)
            {
                var prefabLayer0 = prefabLayeredAudio.layers[0];
                LogCurveDetails("PREFAB", prefabLayer0.pitchCurve, "pitchCurve");
                LogCurveDetails("PREFAB", prefabLayer0.volumeCurve, "volumeCurve");
            }

            // Reset the LayeredAudio to ensure it's in a clean state
            // EXCEPT for chuff sounds - Reset() breaks their dynamic pitch control
            bool isChuffSound = IsChuffSoundType(soundType);
            if (!isChuffSound)
            {
                layeredAudio.Reset();
                Main.DebugLog(() => $"SoundRestorator: Reset LayeredAudio for {soundType}");
            }
            else
            {
                Main.DebugLog(() => $"SoundRestorator: Skipping Reset() for chuff sound {soundType}");
            }

            // Copy vanilla values from prefab
            layeredAudio.minPitch = prefabLayeredAudio.minPitch;
            layeredAudio.maxPitch = prefabLayeredAudio.maxPitch;

            // Restore clips and settings for each layer
            if (prefabLayeredAudio.layers == null || layeredAudio.layers == null)
            {
                Main.mod?.Logger.Warning($"SoundRestorator: LayeredAudio layers is null for {soundType}");
                return;
            }

            int layerCount = Math.Min(layeredAudio.layers.Length, prefabLayeredAudio.layers.Length);
            for (int i = 0; i < layerCount; i++)
            {
                var runtimeLayer = layeredAudio.layers[i];
                var prefabLayer = prefabLayeredAudio.layers[i];

                // Restore clip
                if (runtimeLayer.source != null && prefabLayer.source != null && prefabLayer.source.clip != null)
                {
                    runtimeLayer.source.clip = prefabLayer.source.clip;
                    runtimeLayer.source.mute = prefabLayer.source.mute;
                }

                // Restore layer settings
                runtimeLayer.startPitch = prefabLayer.startPitch;
                runtimeLayer.randomizeStartTime = prefabLayer.randomizeStartTime;

                // ALWAYS restore curves (deep copy) from prefab
                // The curves are essential for LayeredAudio.SetPitch() to work correctly
                // ChuffClipsSimReader calls SetPitch(), which uses the pitchCurve to calculate the final AudioSource pitch
                if (prefabLayer.pitchCurve != null)
                {
                    runtimeLayer.pitchCurve = new AnimationCurve(prefabLayer.pitchCurve.keys);
                }

                if (prefabLayer.volumeCurve != null)
                {
                    runtimeLayer.volumeCurve = new AnimationCurve(prefabLayer.volumeCurve.keys);
                }
            }

            Main.DebugLog(() => $"SoundRestorator: Successfully restored {soundType} from prefab");
            
            // Log the restored values for debugging
            Main.DebugLog(() =>
            {
                if (layeredAudio.layers.Length > 0)
                {
                    var layer0 = layeredAudio.layers[0];
                    return $"SoundRestorator: AFTER restoration for {soundType}: " +
                           $"minPitch={layeredAudio.minPitch}, maxPitch={layeredAudio.maxPitch}, startPitch={layer0.startPitch}, " +
                           $"clip={layer0.source?.clip?.name ?? "null"}, " +
                           $"pitch={layer0.source?.pitch ?? 0f}, volume={layer0.source?.volume ?? 0f}, " +
                           $"pitchCurve keys={layer0.pitchCurve?.keys.Length ?? 0}, volumeCurve keys={layer0.volumeCurve?.keys.Length ?? 0}";
                }
                return $"SoundRestorator: AFTER restoration for {soundType}: no layers";
            });
            
            // Log full curve details for chuff sounds
            if (IsChuffSoundType(soundType) && layeredAudio.layers.Length > 0)
            {
                var layer0 = layeredAudio.layers[0];
                LogCurveDetails("AFTER", layer0.pitchCurve, "pitchCurve");
                LogCurveDetails("AFTER", layer0.volumeCurve, "volumeCurve");
            }
        }
    
        private bool IsChuffSoundType(SoundType soundType)
        {
            return soundType == SoundType.SteamChuff2_67Hz ||
                   soundType == SoundType.SteamChuff3Hz ||
                   soundType == SoundType.SteamChuff4Hz ||
                   soundType == SoundType.SteamChuff5_33Hz ||
                   soundType == SoundType.SteamChuff8Hz ||
                   soundType == SoundType.SteamChuff10_67Hz ||
                   soundType == SoundType.SteamChuff16Hz ||
                   soundType == SoundType.SteamChuff4HzWater ||
                   soundType == SoundType.SteamChuff8HzWater ||
                   soundType == SoundType.SteamChuff16HzWater ||
                   soundType == SoundType.SteamChuff2HzAsh ||
                   soundType == SoundType.SteamChuff4HzAsh ||
                   soundType == SoundType.SteamChuff8HzAsh;
        }

        #endregion

        #region Private Helpers

        private TrainAudio? GetTrainAudio(TrainCar car)
        {
            return car.interior?.GetComponentInChildren<TrainAudio>();
        }

        private GameObject? GetAudioPrefab(TrainCar car)
        {
            var livery = car.carLivery;
            if (livery == null || livery.parentType == null || livery.parentType.audioPrefab == null)
            {
                Main.mod?.Logger.Warning($"SoundRestorator: Could not find audio prefab for {car.carType}");
                return null;
            }

            return livery.parentType.audioPrefab;
        }

        private ChuffClipsSimReader? GetChuffClipsSimReader(TrainCar car)
        {
            var trainAudio = GetTrainAudio(car);
            if (trainAudio == null)
            {
                Main.DebugLog(() => "GetChuffClipsSimReader: No TrainAudio found");
                return null;
            }
            
            // Try to get CarModularAudio (steam locomotives use this)
            if (trainAudio is not CarModularAudio modularAudio)
            {
                Main.DebugLog(() => $"GetChuffClipsSimReader: TrainAudio is not CarModularAudio (type: {trainAudio.GetType().Name})");
                return null;
            }
            
            // Access the SimAudioModule
            var simAudio = modularAudio.audioModules?.OfType<SimAudioModule>().FirstOrDefault();
            if (simAudio?.audioClipSimReadersController?.entries == null)
            {
                Main.DebugLog(() => "GetChuffClipsSimReader: No SimAudioModule or audioClipSimReadersController found");
                return null;
            }
            
            // Find ChuffClipsSimReader
            foreach (var entry in simAudio.audioClipSimReadersController.entries)
            {
                if (entry is ChuffClipsSimReader chuffReader)
                {
                    Main.DebugLog(() => "GetChuffClipsSimReader: Found ChuffClipsSimReader");
                    return chuffReader;
                }
            }
            
            Main.DebugLog(() => "GetChuffClipsSimReader: No ChuffClipsSimReader found in entries");
            return null;
        }

        private string GetGameObjectPath(GameObject obj)
        {
            if (obj == null) return "null";

            var path = obj.name;
            var parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        private void LogCurveDetails(string prefix, AnimationCurve? curve, string curveName)
        {
            if (curve == null)
            {
                Main.DebugLog(() => $"  {prefix} {curveName}: NULL");
                return;
            }

            if (curve.keys.Length == 0)
            {
                Main.DebugLog(() => $"  {prefix} {curveName}: EMPTY (0 keys)");
                return;
            }

            Main.DebugLog(() => $"  {prefix} {curveName}: {curve.keys.Length} keys");
            for (int i = 0; i < curve.keys.Length; i++)
            {
                var key = curve.keys[i];
                Main.DebugLog(() => $"    [{i}] time={key.time:F4}, value={key.value:F4}, inTangent={key.inTangent:F4}, outTangent={key.outTangent:F4}, weightedMode={key.weightedMode}");
            }
        }

        #endregion
    }
}
