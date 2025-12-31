using System;
using System.Collections.Generic;
using UnityEngine;

namespace DvMod.ZSounds.SoundHandler
{
    /// <summary>
    /// Caches the original vanilla audio settings before any modifications are made.
    /// This allows us to restore true vanilla behavior when resetting sounds.
    /// </summary>
    public class VanillaAudioCache
    {
        // Cache structure: trainCarID -> soundType -> cached settings
        private readonly Dictionary<string, Dictionary<SoundType, CachedAudioSettings>> _cache =
            new Dictionary<string, Dictionary<SoundType, CachedAudioSettings>>();

        /// <summary>
        /// Caches the current state of a LayeredAudio component if not already cached.
        /// This should be called BEFORE any modifications are made.
        /// </summary>
        public void CacheIfNeeded(TrainCar car, SoundType soundType, LayeredAudio layeredAudio)
        {
            var carId = car.logicCar.ID;

            // Check if already cached
            if (_cache.ContainsKey(carId) && _cache[carId].ContainsKey(soundType))
            {
                Main.DebugLog(() => $"VanillaCache: Already cached {soundType} for {carId}");
                return;
            }

            // Cache the current settings
            var settings = new CachedAudioSettings(layeredAudio);

            if (!_cache.ContainsKey(carId))
            {
                _cache[carId] = new Dictionary<SoundType, CachedAudioSettings>();
            }

            _cache[carId][soundType] = settings;

            Main.DebugLog(() => $"VanillaCache: Cached vanilla settings for {carId}/{soundType} - " +
                              $"pitch range [{settings.MinPitch}, {settings.MaxPitch}], " +
                              $"layers: {settings.LayerSettings.Count}");
        }

        /// <summary>
        /// Restores the cached vanilla settings to a LayeredAudio component.
        /// </summary>
        public bool RestoreCached(TrainCar car, SoundType soundType, LayeredAudio layeredAudio)
        {
            var carId = car.logicCar.ID;

            if (!_cache.ContainsKey(carId) || !_cache[carId].ContainsKey(soundType))
            {
                Main.mod?.Logger.Warning($"VanillaCache: No cached settings for {carId}/{soundType}");
                return false;
            }

            var settings = _cache[carId][soundType];
            settings.ApplyTo(layeredAudio);

            Main.DebugLog(() => $"VanillaCache: Restored vanilla settings for {carId}/{soundType}");
            return true;
        }
    }

    /// <summary>
    /// Stores the vanilla settings for a LayeredAudio component.
    /// </summary>
    public class CachedAudioSettings
    {
        public float MinPitch { get; }
        public float MaxPitch { get; }
        public List<CachedLayerSettings> LayerSettings { get; }

        public CachedAudioSettings(LayeredAudio layeredAudio)
        {
            MinPitch = layeredAudio.minPitch;
            MaxPitch = layeredAudio.maxPitch;
            LayerSettings = new List<CachedLayerSettings>();

            if (layeredAudio.layers != null)
            {
                foreach (var layer in layeredAudio.layers)
                {
                    LayerSettings.Add(new CachedLayerSettings(layer));
                }
            }
        }

        public void ApplyTo(LayeredAudio layeredAudio)
        {
            layeredAudio.minPitch = MinPitch;
            layeredAudio.maxPitch = MaxPitch;

            if (layeredAudio.layers != null && LayerSettings != null)
            {
                for (int i = 0; i < Math.Min(layeredAudio.layers.Length, LayerSettings.Count); i++)
                {
                    LayerSettings[i].ApplyTo(layeredAudio.layers[i]);
                }
            }
        }
    }

    /// <summary>
    /// Stores the vanilla settings for a single layer.
    /// </summary>
    public class CachedLayerSettings
    {
        public AudioClip? Clip { get; }
        public float StartPitch { get; }
        public AnimationCurve? PitchCurve { get; }
        public AnimationCurve? VolumeCurve { get; }
        public bool RandomizeStartTime { get; }
        public bool Muted { get; }

        public CachedLayerSettings(LayeredAudio.Layer layer)
        {
            Clip = layer.source?.clip;
            StartPitch = layer.startPitch;
            RandomizeStartTime = layer.randomizeStartTime;
            Muted = layer.source?.mute ?? false;

            // Deep copy the curves to avoid reference issues
            PitchCurve = layer.pitchCurve != null ? new AnimationCurve(layer.pitchCurve.keys) : null;
            VolumeCurve = layer.volumeCurve != null ? new AnimationCurve(layer.volumeCurve.keys) : null;
        }

        public void ApplyTo(LayeredAudio.Layer layer)
        {
            if (layer.source != null && Clip != null)
            {
                layer.source.clip = Clip;
                layer.source.mute = Muted;
            }

            layer.startPitch = StartPitch;
            layer.randomizeStartTime = RandomizeStartTime;

            // Apply deep-copied curves
            if (PitchCurve != null)
            {
                layer.pitchCurve = new AnimationCurve(PitchCurve.keys);
            }

            if (VolumeCurve != null)
            {
                layer.volumeCurve = new AnimationCurve(VolumeCurve.keys);
            }
        }
    }
}

