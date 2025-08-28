using System;
using System.Collections.Generic;
using System.Linq;

using DV.Simulation.Ports;
using DV.ThingTypes;

using UnityEngine;

namespace DvMod.ZSounds
{
    public static class AudioUtils
    {
        public struct AudioSettings
        {
            public AudioClip? clip;
            public AudioClip[]? clips;
            public float pitch;
            public float minPitch;
            public float maxPitch;
            public float minVolume;
            public float maxVolume;
            public AnimationCurve? pitchCurve;
            public AnimationCurve? volumeCurve;

            public override string ToString()
            {
                return $"clip={clip?.length},clips={clips?.Length},pitch={pitch},minPitch={minPitch},maxPitch={maxPitch},minVolume={minVolume},maxVolume={maxVolume}";
            }
        }

        private static AudioSettings CreateAudioSettings(
            AudioClip? clip = null,
            AudioClip[]? clips = null,
            float pitch = 1.0f,
            float minPitch = 1.0f,
            float maxPitch = 1.0f,
            float minVolume = 1.0f,
            float maxVolume = 1.0f,
            AnimationCurve? pitchCurve = null,
            AnimationCurve? volumeCurve = null)
        {
            return new AudioSettings
            {
                clip = clip,
                clips = clips,
                pitch = pitch,
                minPitch = minPitch,
                maxPitch = maxPitch,
                minVolume = minVolume,
                maxVolume = maxVolume,
                pitchCurve = pitchCurve,
                volumeCurve = volumeCurve
            };
        }

        // Safely load an AudioClip with fallback and logging
        private static AudioClip? SafeLoadAudioClip(string resourceName, string? fallbackName = null)
        {
            // First try standard Resources.Load
            var clip = Resources.Load<AudioClip>(resourceName);
            if (clip != null)
            {
                return clip;
            }

            Main.mod?.Logger.Warning($"Failed to load AudioClip: {resourceName}");
            
            // Try to extract from existing locomotive audio prefabs
            clip = TryExtractFromLocomotiveAudio(resourceName);
            if (clip != null)
            {
                Main.DebugLog(() => $"Extracted AudioClip from locomotive: {resourceName}");
                return clip;
            }
            
            if (!string.IsNullOrEmpty(fallbackName))
            {
                var fallbackClip = Resources.Load<AudioClip>(fallbackName);
                if (fallbackClip != null)
                {
                    Main.DebugLog(() => $"Using fallback AudioClip: {fallbackName} for {resourceName}");
                    return fallbackClip;
                }
                Main.mod?.Logger.Warning($"Failed to load fallback AudioClip: {fallbackName}");
            }

            Main.mod?.Logger.Warning($"No AudioClip available for {resourceName}, will use null");
            return null;
        }

        // Try to extract audio clip from locomotive audio prefabs by finding the clip in LayeredAudio components
        private static AudioClip? TryExtractFromLocomotiveAudio(string clipName)
        {
            try
            {
                // Find all LayeredAudio components in the scene that might contain this clip
                var layeredAudios = UnityEngine.Object.FindObjectsOfType<LayeredAudio>();
                
                foreach (var layeredAudio in layeredAudios)
                {
                    // Check each layer for the clip
                    foreach (var layer in layeredAudio.layers)
                    {
                        if (layer?.source?.clip?.name == clipName)
                        {
                            return layer.source.clip;
                        }
                    }
                }
                
                // Also check AudioClipPortReader components
                var audioClipReaders = UnityEngine.Object.FindObjectsOfType<AudioClipPortReader>();
                foreach (var reader in audioClipReaders)
                {
                    if (reader.clips != null)
                    {
                        foreach (var clip in reader.clips)
                        {
                            if (clip?.name == clipName)
                            {
                                return clip;
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Main.mod?.Logger.Warning($"Error extracting clip {clipName} from locomotive audio: {ex.Message}");
            }
            
            return null;
        }

        public static void Apply(TrainCarType carType, SoundType soundType, SoundSet soundSet, ref AudioClip clip)
        {
            Main.DebugLog(() => $"AudioUtils.Apply: Processing single AudioClip for {carType}/{soundType}");

            var soundDefinition = soundSet[soundType];

            if (soundDefinition?.filename != null)
            {
                Main.DebugLog(() => $"AudioUtils.Apply: Using custom filename for {soundType}: {soundDefinition.filename}");
                clip = FileAudio.Load(soundDefinition.filename);
            }
            else
            {
                Main.DebugLog(() => $"AudioUtils.Apply: No custom sound found for {soundType}, keeping original clip");
            }

            var clipName = clip?.name ?? "null";
            Main.DebugLog(() => $"AudioUtils.Apply: Final clip for {soundType}: {clipName}");
        }

        public static void Apply(TrainCarType carType, SoundType soundType, SoundSet soundSet, ref AudioClip[] clips)
        {
            Main.DebugLog(() => $"AudioUtils.Apply: *** PROCESSING AUDIOCLIP[] *** for {carType}/{soundType}");

            var soundDefinition = soundSet[soundType];

            Main.DebugLog(() => $"AudioUtils.Apply: Sound definition for {soundType}: {soundDefinition?.name ?? "NULL"}");

            // Note: AudioClip[] sounds (generic sounds) don't support pitch/volume configuration
            // as they're played directly by the game's AudioManager without LayeredAudio
            if (soundDefinition?.pitch != null || soundDefinition?.minPitch != null || soundDefinition?.maxPitch != null)
            {
                Main.mod?.Logger.Warning($"AudioUtils.Apply: Pitch settings ignored for {soundType} - AudioClip[] sounds don't support pitch configuration");
            }
            if (soundDefinition != null && (soundDefinition.filenames?.Length ?? 0) > 0)
            {
                Main.DebugLog(() => $"AudioUtils.Apply: Using custom filenames for {soundType}: {string.Join(", ", soundDefinition.filenames!)}");
                clips = soundDefinition.filenames!.Select(FileAudio.Load).ToArray();
            }
            else if (soundDefinition?.filename != null)
            {
                Main.DebugLog(() => $"AudioUtils.Apply: Using custom filename for {soundType}: {soundDefinition.filename}");
                clips = new AudioClip[] { FileAudio.Load(soundDefinition.filename) };
            }
            else
            {
                Main.DebugLog(() => $"AudioUtils.Apply: No custom sounds found for {soundType}, keeping original clips");
                // For AudioClip arrays, we don't have good defaults in AudioMapper
                // so we just keep the original clips
            }

            var finalClipCount = clips?.Length ?? 0;
            Main.DebugLog(() => $"AudioUtils.Apply: Final clip count for {soundType}: {finalClipCount}");
        }

        private static AnimationCurve MakeCurve(AnimationCurve? defaultCurve, float? newMin, float? newMax)
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

        public static void Apply(TrainCarType carType, SoundType soundType, SoundSet soundSet, LayeredAudio audio)
        {
            Main.DebugLog(() => $"AudioUtils.Apply: Processing LayeredAudio for {carType}/{soundType}");

            var soundDefinition = soundSet[soundType];
            
            // Only apply if we have a custom sound definition
            if (soundDefinition == null)
            {
                Main.DebugLog(() => $"AudioUtils.Apply: No custom sound definition for {soundType}, keeping original settings");
                return;
            }

            var mainLayer = audio.layers[0];
            Main.DebugLog(() => $"AudioUtils.Apply: Applying custom sound definition for {soundType}");

            // For EngineLoop sounds, always stop the audio when applying new clips
            bool wasPlaying = mainLayer.source.isPlaying;
            if (wasPlaying && soundType == SoundType.EngineLoop)
            {
                mainLayer.source.Stop();
                audio.Set(0f);
            }

            // Apply audio clip if specified
            if (soundDefinition.filename != null)
            {
                var newClip = FileAudio.Load(soundDefinition.filename);
                if (newClip != null)
                {
                    // Stop playing audio if we're changing the clip
                    if (wasPlaying && mainLayer.source.clip != newClip)
                    {
                        mainLayer.source.Stop();
                        audio.Set(0f);
                    }
                    mainLayer.source.clip = newClip;
                    Main.DebugLog(() => $"AudioUtils.Apply: Applied clip {newClip.name} for {soundType}");
                }
            }

            // Apply pitch settings only if specified in config
            if (soundDefinition.pitch != null)
            {
                mainLayer.startPitch = soundDefinition.pitch.Value;
                Main.DebugLog(() => $"AudioUtils.Apply: Applied pitch {soundDefinition.pitch} for {soundType}");
            }

            if (soundDefinition.minPitch != null || soundDefinition.maxPitch != null)
            {
                if (soundDefinition.minPitch != null)
                    audio.minPitch = soundDefinition.minPitch.Value;
                if (soundDefinition.maxPitch != null)
                    audio.maxPitch = soundDefinition.maxPitch.Value;
                Main.DebugLog(() => $"AudioUtils.Apply: Applied pitch range [{audio.minPitch}, {audio.maxPitch}] for {soundType}");
            }

            // Apply curves only if specified in config
            if (soundDefinition.pitchCurve != null)
            {
                mainLayer.pitchCurve = MakeCurve(soundDefinition.pitchCurve, soundDefinition.minPitch, soundDefinition.maxPitch);
                Main.DebugLog(() => $"AudioUtils.Apply: Applied custom pitch curve for {soundType}");
            }

            if (soundDefinition.volumeCurve != null)
            {
                mainLayer.volumeCurve = MakeCurve(soundDefinition.volumeCurve, soundDefinition.minVolume, soundDefinition.maxVolume);
                Main.DebugLog(() => $"AudioUtils.Apply: Applied custom volume curve for {soundType}");
            }

            // Mute other layers for custom sounds
            for (int i = 1; i < audio.layers.Length; i++)
                audio.layers[i].source.mute = true;

            // For EngineLoop sounds, ensure they're stopped after applying to prevent unwanted playback
            if (soundType == SoundType.EngineLoop)
            {
                audio.Set(0f);
            }

            var finalClipName = mainLayer.source.clip?.name ?? "null";
            Main.DebugLog(() => $"AudioUtils.Apply: LayeredAudio application completed for {soundType}. Final clip: {finalClipName}");
        }

        public static TrainAudio GetTrainAudio(TrainCar car)
        {
            return car.interior.GetComponentInChildren<TrainAudio>();
        }

        public static void Apply(TrainCar car, SoundSet soundSet)
        {
            Apply(GetTrainAudio(car), soundSet);
        }

        public static void Apply(TrainAudio trainAudio, SoundSet soundSet)
        {
            var carType = trainAudio.car.carType;
            if (!AudioMapper.Mappers.TryGetValue(carType, out var audioMapper))
                return;

            foreach (var soundType in SoundTypes.audioClipsSoundTypes)
            {
                var portReader = audioMapper.GetAudioClipPortReader(soundType, trainAudio);
                if (portReader == null) continue;
                Main.DebugLog(() => $"AudioUtils.Apply: Processing AudioClipPortReader {soundType} for {carType}");
                Apply(carType, soundType, soundSet, ref portReader.clips);
            }

            foreach (var soundType in SoundTypes.layeredAudioSoundTypes)
            {
                var layeredAudio = audioMapper.GetLayeredAudio(soundType, trainAudio);
                if (layeredAudio == null) continue;
                Main.DebugLog(() => $"AudioUtils.Apply: Processing LayeredAudio {soundType} for {carType}");
                Apply(carType, soundType, soundSet, layeredAudio);
            }
        }
    }
}