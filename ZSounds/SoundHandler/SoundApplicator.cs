using System.Linq;
using UnityEngine;

namespace DvMod.ZSounds.SoundHandler
{
    /// <summary>
    /// Service for applying custom sounds to locomotive audio components.
    /// Consolidates functionality from AudioUtils.cs Apply() methods.
    /// </summary>
    public class SoundApplicator
    {
        private readonly SoundDiscovery _soundDiscovery;
        private readonly SoundLoader _soundLoader;

        public SoundApplicator(SoundDiscovery soundDiscovery, SoundLoader soundLoader)
        {
            _soundDiscovery = soundDiscovery;
            _soundLoader = soundLoader;
        }

        #region Public API - Main Entry Points

        /// <summary>
        /// Applies a complete sound set to a train car.
        /// </summary>
        public void ApplySoundSet(TrainCar car, SoundSet soundSet)
        {
            var trainAudio = GetTrainAudio(car);
            if (trainAudio == null)
            {
                Main.mod?.Logger.Warning($"SoundApplicator: Could not get TrainAudio for car {car.ID}");
                return;
            }

            ApplySoundSet(trainAudio, soundSet);
        }

        /// <summary>
        /// Applies a complete sound set to a train audio component.
        /// </summary>
        public void ApplySoundSet(TrainAudio trainAudio, SoundSet soundSet)
        {
            var car = trainAudio.car;
            var carType = car.carType;

            // Process AudioClipPortReader sounds
            foreach (var soundType in SoundTypes.audioClipsSoundTypes)
            {
                var portReader = _soundDiscovery.GetAudioClipPortReader(trainAudio, soundType);
                if (portReader == null)
                    continue;

                Main.DebugLog(() => $"SoundApplicator: Processing AudioClipPortReader {soundType} for {carType}");
                ApplyToAudioClipArray(car, soundType, soundSet, ref portReader.clips);
            }

            // Process LayeredAudio sounds
            foreach (var soundType in SoundTypes.layeredAudioSoundTypes)
            {
                var layeredAudio = _soundDiscovery.GetLayeredAudio(trainAudio, soundType);
                if (layeredAudio == null)
                    continue;

                Main.DebugLog(() => $"SoundApplicator: Processing LayeredAudio {soundType} for {carType}");
                ApplyToLayeredAudio(car, soundType, soundSet, layeredAudio);
            }
        }

        #endregion

        #region Public API - Individual Sound Application

        /// <summary>
        /// Applies a sound definition to an AudioClip array (e.g., horn hit sounds).
        /// </summary>
        public void ApplyToAudioClipArray(TrainCar car, SoundType soundType, SoundSet soundSet, ref AudioClip[] clips)
        {
            var carType = car.carType;
            Main.DebugLog(() => $"SoundApplicator: *** PROCESSING AUDIOCLIP[] *** for {carType}/{soundType}");

            var soundDefinition = soundSet[soundType];
            Main.DebugLog(() => $"SoundApplicator: Sound definition for {soundType}: {soundDefinition?.name ?? "NULL"}");

            // If no custom sound definition, keep original clips
            if (soundDefinition == null)
            {
                Main.DebugLog(() => $"SoundApplicator: No custom sound definition for {soundType}, keeping original clips");
                return;
            }

            // Note: AudioClip[] sounds don't support pitch/volume configuration
            if (soundDefinition.pitch != null || soundDefinition.minPitch != null || soundDefinition.maxPitch != null)
            {
                Main.mod?.Logger.Warning($"SoundApplicator: Pitch settings ignored for {soundType} - AudioClip[] sounds don't support pitch configuration");
            }

            if ((soundDefinition.filenames?.Length ?? 0) > 0)
            {
                Main.DebugLog(() => $"SoundApplicator: Using custom filenames for {soundType}: {string.Join(", ", soundDefinition.filenames!)}");
                clips = soundDefinition.filenames!.Select(f => _soundLoader.LoadAudioClip(f)).ToArray();
            }
            else if (soundDefinition.filename != null)
            {
                Main.DebugLog(() => $"SoundApplicator: Using custom filename for {soundType}: {soundDefinition.filename}");
                clips = new[] { _soundLoader.LoadAudioClip(soundDefinition.filename) };
            }
            else
            {
                Main.DebugLog(() => $"SoundApplicator: No custom sounds found for {soundType}, keeping original clips");
            }

            var finalClipCount = clips.Length;
            Main.DebugLog(() => $"SoundApplicator: Final clip count for {soundType}: {finalClipCount}");
        }

        /// <summary>
        /// Applies a sound definition to a LayeredAudio component (e.g., engine, horn loop).
        /// </summary>
        public void ApplyToLayeredAudio(TrainCar car, SoundType soundType, SoundSet soundSet, LayeredAudio audio)
        {
            var carType = car.carType;
            Main.DebugLog(() => $"SoundApplicator: Processing LayeredAudio for {carType}/{soundType}");

            // IMPORTANT: Cache vanilla settings BEFORE making any modifications
            Main.vanillaCache?.CacheIfNeeded(car, soundType, audio);

            var soundDefinition = soundSet[soundType];
            Main.DebugLog(() => $"SoundApplicator: soundDefinition for {soundType} = {soundDefinition?.name ?? "NULL"}, randomizeStartTime = {soundDefinition?.randomizeStartTime?.ToString() ?? "NULL"}");

            if (audio.layers == null || audio.layers.Length == 0)
            {
                Main.mod?.Logger.Warning($"SoundApplicator: LayeredAudio has no layers for {carType}/{soundType}");
                return;
            }

            var mainLayer = audio.layers[0];
            bool wasPlaying = mainLayer.source.isPlaying;

            // If no custom sound definition, nothing to apply (restoration is handled by SoundRestorator)
            if (soundDefinition == null)
            {
                Main.DebugLog(() => $"SoundApplicator: No custom sound definition for {soundType}, nothing to apply");
                return;
            }

            // For EngineLoop sounds, always stop the audio when applying new clips
            if (wasPlaying && soundType == SoundType.EngineLoop)
            {
                mainLayer.source.Stop();
                audio.Set(0f);
            }

            // Apply audio clip if specified
            if (soundDefinition.filename != null)
            {
                var newClip = _soundLoader.LoadAudioClip(soundDefinition.filename);
                if (newClip != null)
                {
                    // Stop playing audio if we're changing the clip
                    if (wasPlaying && mainLayer.source.clip != newClip)
                    {
                        mainLayer.source.Stop();
                        audio.Set(0f);
                    }
                    mainLayer.source.clip = newClip;
                    Main.DebugLog(() => $"SoundApplicator: Applied clip {newClip.name} for {soundType}");
                }
            }

            // Apply pitch settings only if specified in config
            if (soundDefinition.pitch != null)
            {
                mainLayer.startPitch = soundDefinition.pitch.Value;
                Main.DebugLog(() => $"SoundApplicator: Applied pitch {soundDefinition.pitch} for {soundType}");
            }

            if (soundDefinition.minPitch != null || soundDefinition.maxPitch != null)
            {
                if (soundDefinition.minPitch != null)
                    audio.minPitch = soundDefinition.minPitch.Value;
                if (soundDefinition.maxPitch != null)
                    audio.maxPitch = soundDefinition.maxPitch.Value;
                Main.DebugLog(() => $"SoundApplicator: Applied pitch range [{audio.minPitch}, {audio.maxPitch}] for {soundType}");
            }

            // Apply curves only if specified in config
            if (soundDefinition.pitchCurve != null)
            {
                mainLayer.pitchCurve = MakeCurve(soundDefinition.pitchCurve, soundDefinition.minPitch, soundDefinition.maxPitch);
                Main.DebugLog(() => $"SoundApplicator: Applied custom pitch curve for {soundType}");
            }

            if (soundDefinition.volumeCurve != null)
            {
                mainLayer.volumeCurve = MakeCurve(soundDefinition.volumeCurve, soundDefinition.minVolume, soundDefinition.maxVolume);
                Main.DebugLog(() => $"SoundApplicator: Applied custom volume curve for {soundType}");
            }

            // Apply randomizeStartTime setting
            if (soundDefinition.randomizeStartTime.HasValue)
            {
                foreach (var layer in audio.layers)
                {
                    layer.randomizeStartTime = soundDefinition.randomizeStartTime.Value;
                }
                Main.DebugLog(() => $"SoundApplicator: Applied randomizeStartTime={soundDefinition.randomizeStartTime.Value} for {soundType}");
            }

            // Mute other layers for full-layer replacement
            for (int i = 1; i < audio.layers.Length; i++)
                audio.layers[i].source.mute = true;

            // For EngineLoop sounds, ensure they're stopped after applying to prevent unwanted playback
            if (soundType == SoundType.EngineLoop)
            {
                audio.Set(0f);
            }

            var finalClipName = mainLayer.source.clip?.name ?? "null";
            Main.DebugLog(() => $"SoundApplicator: Completed LayeredAudio replacement for {soundType}. Final clip: {finalClipName}");
        }

        #endregion

        #region Private Helpers

        private TrainAudio? GetTrainAudio(TrainCar car)
        {
            return car.interior?.GetComponentInChildren<TrainAudio>();
        }

        private AnimationCurve MakeCurve(AnimationCurve? defaultCurve, float? newMin, float? newMax)
        {
            // If defaultCurve is null, return a linear curve
            if (defaultCurve == null)
            {
                Main.mod?.Logger.Warning("MakeCurve received null defaultCurve, using linear fallback");
                return AnimationCurve.Linear(0, 1, 1, 1);
            }

            if (!newMin.HasValue && !newMax.HasValue)
                return defaultCurve;

            var (start, end) = defaultCurve.length > 0
                ? (defaultCurve[0].time, defaultCurve.keys[defaultCurve.keys.Length - 1].time)
                : (0f, 1f);

            return AnimationCurve.EaseInOut(
                start, newMin ?? defaultCurve.Evaluate(start),
                end, newMax ?? defaultCurve.Evaluate(end));
        }

        #endregion
    }
}
