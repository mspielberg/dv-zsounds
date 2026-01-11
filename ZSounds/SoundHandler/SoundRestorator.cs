using System;
using System.Linq;
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
            Main.mod?.Logger.Log($"SoundRestorator: Restoring all sounds to defaults for {car.carType}");

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

            Main.mod?.Logger.Log($"SoundRestorator: Completed restoration for {car.carType}");
        }

        /// <summary>
        /// Restores a specific sound type to its default value.
        /// </summary>
        public void RestoreSound(TrainCar car, SoundType soundType)
        {
            Main.mod?.Logger.Log($"SoundRestorator: Restoring {soundType} to default for {car.carType}");

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

        #endregion

        #region Private - Restoration Application

        private void RestoreAudioClipArraySound(TrainCar car, TrainAudio trainAudio, SoundType soundType)
        {
            var portReader = _soundDiscovery.GetAudioClipPortReader(trainAudio, soundType);
            if (portReader == null)
                return;

            Main.DebugLog(() => $"SoundRestorator: Restoring AudioClip[] for {soundType}");

            // Try to restore from cache first (includes pitch, volume, and clips)
            bool restoredFromCache = Main.vanillaCache?.RestoreCached(car, soundType, portReader) ?? false;

            if (restoredFromCache)
            {
                Main.mod?.Logger.Log($"SoundRestorator: Restored AudioClip settings from cache for {soundType}");
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

            // Stop playing audio before restoring
            var mainLayer = layeredAudio.layers[0];
            bool wasPlaying = mainLayer.source.isPlaying;
            if (wasPlaying)
            {
                mainLayer.source.Stop();
                layeredAudio.Set(0f);
            }

            // Try to restore from vanilla cache first
            bool restoredFromCache = Main.vanillaCache?.RestoreCached(car, soundType, layeredAudio) ?? false;

            if (restoredFromCache)
            {
                Main.DebugLog(() => $"SoundRestorator: Successfully restored {soundType} from vanilla cache");
            }
            else
            {
                Main.mod?.Logger.Warning($"SoundRestorator: No cached vanilla settings for {car.carType}/{soundType}, cannot restore!");
                Main.mod?.Logger.Warning($"This sound was never cached before modification. Reset may not work correctly.");
            }

            // Reset the LayeredAudio to reinitialize internal state
            layeredAudio.Reset();
            Main.DebugLog(() => $"SoundRestorator: Reset LayeredAudio for {soundType}");
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

        #endregion
    }
}
