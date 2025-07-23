using DV.ThingTypes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DV.Simulation.Ports;
using System;

namespace DvMod.ZSounds
{
    public static class AudioUtils
    {
        private struct AudioSettings
        {
            public AudioClip? clip;
            public AudioClip[]? clips;
            public float pitch;
            public float minPitch;
            public float maxPitch;
            public AnimationCurve? pitchCurve;
            public AnimationCurve? volumeCurve;

            public override string ToString()
            {
                return $"clip={clip?.length},clips={clips?.Length},pitch={pitch},minPitch={minPitch},maxPitch={maxPitch}";
            }
        }

        public struct DefaultKey
        {
            public readonly TrainCarType carType;
            public readonly SoundType soundType;

            public DefaultKey(TrainCarType carType, SoundType soundType)
            {
                this.carType = carType;
                this.soundType = soundType;
            }

            public override string ToString()
            {
                return $"({carType}, {soundType})";
            }
        }

        private static Dictionary<DefaultKey, AudioSettings> CreateDefaults()
        {
            var defaults = new Dictionary<DefaultKey, AudioSettings>();
            
            try
            {
                // LocoMicroshunter defaults
                defaults[new DefaultKey(TrainCarType.LocoMicroshunter, SoundType.HornLoop)] = new AudioSettings()
                {
                    clip = Resources.Load<AudioClip>("Horn_Microshunter_01"),
                    minPitch = 1.000f,
                    maxPitch = 1.000f,
                    pitch = 1.000f,
                    pitchCurve = new AnimationCurve(),
                    volumeCurve = new AnimationCurve(
                        new Keyframe(0.000f, 0.000f),
                        new Keyframe(0.395f, 0.000f),
                        new Keyframe(0.500f, 0.499f),
                        new Keyframe(1.000f, 0.499f)
                    )
                };
        
            defaults[new DefaultKey(TrainCarType.LocoMicroshunter, SoundType.TractionMotors)] = new AudioSettings()
            {
                clip = Resources.Load<AudioClip>("TM_E2_01"),
                minPitch = 0.400f,
                maxPitch = 12.000f,
                pitch = 1.000f,
                pitchCurve = new AnimationCurve(), // 0 keys - empty curve
                volumeCurve = new AnimationCurve(
                    new Keyframe(0.000f, 0.000f),
                    new Keyframe(0.200f, 0.500f),
                    new Keyframe(1.000f, 1.000f)
                )
            };
            
                            // LocoSteamHeavy defaults
            defaults[new DefaultKey(TrainCarType.LocoSteamHeavy, SoundType.Whistle)] = new AudioSettings()
            {
                clip = Resources.Load<AudioClip>("Whistle_03_Hoarse"),
                minPitch = 1.000f,
                maxPitch = 1.000f,
                pitch = 1.000f,
                pitchCurve = new AnimationCurve(
                    new Keyframe(-0.004f, 0.800f),
                    new Keyframe(0.496f, 0.851f),
                    new Keyframe(0.747f, 0.901f),
                    new Keyframe(0.963f, 0.949f),
                    new Keyframe(1.000f, 0.951f)
                ),
                volumeCurve = new AnimationCurve(
                    new Keyframe(0.000f, -0.498f),
                    new Keyframe(0.397f, 0.000f),
                    new Keyframe(0.700f, 0.647f),
                    new Keyframe(1.000f, 0.810f)
                )
            };
            
            defaults[new DefaultKey(TrainCarType.LocoSteamHeavy, SoundType.Bell)] = new AudioSettings()
            {
                clip = Resources.Load<AudioClip>("Train_Bell-S282_01"),
                minPitch = 1.000f,
                maxPitch = 1.000f,
                pitch = 1.000f,
                pitchCurve = new AnimationCurve(), // 0 keys - empty curve
                volumeCurve = new AnimationCurve(
                    new Keyframe(0.000f, 0.000f),
                    new Keyframe(1.000f, 1.000f)
                )
            };
            
                            // LocoS060 defaults
            defaults[new DefaultKey(TrainCarType.LocoS060, SoundType.Whistle)] = new AudioSettings()
            {
                clip = Resources.Load<AudioClip>("Whistle_08_Sine"),
                minPitch = 1.000f,
                maxPitch = 1.000f,
                pitch = 1.250f,
                pitchCurve = new AnimationCurve(
                    new Keyframe(-0.004f, 0.800f),
                    new Keyframe(0.496f, 0.851f),
                    new Keyframe(0.747f, 0.901f),
                    new Keyframe(0.963f, 0.949f),
                    new Keyframe(1.000f, 0.951f)
                ),
                volumeCurve = new AnimationCurve(
                    new Keyframe(0.000f, -0.498f),
                    new Keyframe(0.397f, 0.000f),
                    new Keyframe(0.700f, 0.647f),
                    new Keyframe(1.000f, 0.810f)
                )
            };
            
            defaults[new DefaultKey(TrainCarType.LocoS060, SoundType.Bell)] = new AudioSettings()
            {
                clip = Resources.Load<AudioClip>("Train_Bell-S060_01"),
                minPitch = 1.000f,
                maxPitch = 1.000f,
                pitch = 1.000f,
                pitchCurve = new AnimationCurve(), // 0 keys - empty curve
                volumeCurve = new AnimationCurve(
                    new Keyframe(0.000f, 0.000f),
                    new Keyframe(1.000f, 1.000f)
                )
            };
            
                            // LocoDM3 defaults
            defaults[new DefaultKey(TrainCarType.LocoDM3, SoundType.HornLoop)] = new AudioSettings()
            {
                clip = Resources.Load<AudioClip>("Horn_LocoDM3_01"),
                minPitch = 1.000f,
                maxPitch = 1.000f,
                pitch = 1.000f,
                pitchCurve = new AnimationCurve(new Keyframe(0.000f, 1.000f)),
                volumeCurve = new AnimationCurve(
                    new Keyframe(0.000f, 0.000f),
                    new Keyframe(0.550f, 0.000f),
                    new Keyframe(0.717f, 1.000f),
                    new Keyframe(1.002f, 1.000f)
                )
            };
            
            defaults[new DefaultKey(TrainCarType.LocoDM3, SoundType.EngineLoop)] = new AudioSettings()
            {
                clip = Resources.Load<AudioClip>("ICE_LocoDH4_01_Idle"),
                minPitch = 0.000f,
                maxPitch = 3.750f,
                pitch = 0.450f,
                pitchCurve = new AnimationCurve(
                    new Keyframe(0.000f, 1.000f),
                    new Keyframe(2.000f, 7.000f)
                ),
                volumeCurve = new AnimationCurve(
                    new Keyframe(0.000f, 0.000f),
                    new Keyframe(0.200f, 1.000f),
                    new Keyframe(0.500f, 0.000f)
                )
            };
            
            defaults[new DefaultKey(TrainCarType.LocoDM3, SoundType.EngineLoadLoop)] = new AudioSettings()
            {
                clip = Resources.Load<AudioClip>("ICE_LocoDM3_Exhaust_01"),
                minPitch = 1.000f,
                maxPitch = 3.000f,
                pitch = 0.500f,
                pitchCurve = new AnimationCurve(
                    new Keyframe(0.000f, 1.000f),
                    new Keyframe(2.000f, 7.000f)
                ),
                volumeCurve = new AnimationCurve(
                    new Keyframe(0.000f, 0.000f),
                    new Keyframe(1.000f, 0.600f)
                )
            };
            
                            // LocoDH4 defaults
            defaults[new DefaultKey(TrainCarType.LocoDH4, SoundType.HornLoop)] = new AudioSettings()
            {
                clip = Resources.Load<AudioClip>("horn_LocoDH4_01"),
                minPitch = 1.000f,
                maxPitch = 1.000f,
                pitch = 1.000f,
                pitchCurve = new AnimationCurve(), // 0 keys - empty curve
                volumeCurve = new AnimationCurve(
                    new Keyframe(0.000f, 0.000f),
                    new Keyframe(0.250f, 0.000f),
                    new Keyframe(0.617f, 0.653f),
                    new Keyframe(1.002f, 0.960f)
                )
            };
            
            defaults[new DefaultKey(TrainCarType.LocoDH4, SoundType.Bell)] = new AudioSettings()
            {
                clip = Resources.Load<AudioClip>("Train_Bell-DH4_01"),
                minPitch = 1.000f,
                maxPitch = 1.000f,
                pitch = 1.000f,
                pitchCurve = new AnimationCurve(), // 0 keys - empty curve
                volumeCurve = new AnimationCurve(
                    new Keyframe(0.000f, 0.000f),
                    new Keyframe(1.000f, 1.000f)
                )
            };
            
            defaults[new DefaultKey(TrainCarType.LocoDH4, SoundType.EngineLoop)] = new AudioSettings()
            {
                clip = Resources.Load<AudioClip>("ICE_LocoDE2_01_Idle"),
                minPitch = 0.000f,
                maxPitch = 3.333f,
                pitch = 0.500f,
                pitchCurve = new AnimationCurve(), // 0 keys - empty curve
                volumeCurve = new AnimationCurve(
                    new Keyframe(0.000f, 0.000f),
                    new Keyframe(0.150f, 0.350f),
                    new Keyframe(0.600f, 0.000f),
                    new Keyframe(1.000f, 0.000f)
                )
            };
            
            defaults[new DefaultKey(TrainCarType.LocoDH4, SoundType.EngineLoadLoop)] = new AudioSettings()
            {
                clip = Resources.Load<AudioClip>("ICE_LocoDH4_01_Exhaust"),
                minPitch = 1.000f,
                maxPitch = 3.000f,
                pitch = 0.450f,
                pitchCurve = new AnimationCurve(), // 0 keys - empty curve
                volumeCurve = new AnimationCurve(
                    new Keyframe(0.000f, 0.000f),
                    new Keyframe(0.030f, 0.001f),
                    new Keyframe(0.050f, 0.100f),
                    new Keyframe(1.000f, 0.800f)
                )
            };
            
                            // LocoDiesel defaults
            defaults[new DefaultKey(TrainCarType.LocoDiesel, SoundType.HornHit)] = new AudioSettings()
            {
                clips = new AudioClip[] { Resources.Load<AudioClip>("Horn_LocoDE6_01_Pulse") },
                minPitch = 1.000f,
                maxPitch = 1.000f,
                pitch = 1.000f
            };
            
            defaults[new DefaultKey(TrainCarType.LocoDiesel, SoundType.HornLoop)] = new AudioSettings()
            {
                clip = Resources.Load<AudioClip>("Horn_LocoDE6_01"),
                minPitch = 1.000f,
                maxPitch = 1.000f,
                pitch = 1.000f,
                pitchCurve = new AnimationCurve(), // 0 keys - empty curve
                volumeCurve = new AnimationCurve(
                    new Keyframe(0.000f, 0.000f),
                    new Keyframe(0.250f, 0.000f),
                    new Keyframe(0.617f, 0.653f),
                    new Keyframe(1.002f, 0.960f)
                )
            };
            
            defaults[new DefaultKey(TrainCarType.LocoDiesel, SoundType.Bell)] = new AudioSettings()
            {
                clip = Resources.Load<AudioClip>("Train_Bell-DE6_01"),
                minPitch = 1.000f,
                maxPitch = 1.000f,
                pitch = 1.000f,
                pitchCurve = new AnimationCurve(), // 0 keys - empty curve
                volumeCurve = new AnimationCurve(
                    new Keyframe(0.000f, 0.000f),
                    new Keyframe(1.000f, 1.000f)
                )
            };
            
            defaults[new DefaultKey(TrainCarType.LocoDiesel, SoundType.EngineLoop)] = new AudioSettings()
            {
                clip = Resources.Load<AudioClip>("ICE_LocoDE6_01"),
                minPitch = 1.000f,
                maxPitch = 5.000f,
                pitch = 1.000f,
                pitchCurve = new AnimationCurve(
                    new Keyframe(0.000f, 0.125f),
                    new Keyframe(1.000f, 1.750f)
                ),
                volumeCurve = new AnimationCurve(
                    new Keyframe(0.000f, 0.000f),
                    new Keyframe(0.066f, 0.030f),
                    new Keyframe(0.108f, 0.792f),
                    new Keyframe(0.400f, 0.000f),
                    new Keyframe(1.000f, 0.000f)
                )
            };
            
            defaults[new DefaultKey(TrainCarType.LocoDiesel, SoundType.EngineLoadLoop)] = new AudioSettings()
            {
                clip = Resources.Load<AudioClip>("ICE_LocoDE6_02_Engine"),
                minPitch = 1.000f,
                maxPitch = 1.000f,
                pitch = 0.750f,
                pitchCurve = new AnimationCurve(
                    new Keyframe(0.000f, 0.125f),
                    new Keyframe(1.000f, 1.750f)
                ),
                volumeCurve = new AnimationCurve(
                    new Keyframe(0.000f, 0.100f),
                    new Keyframe(0.199f, 0.788f),
                    new Keyframe(1.000f, 1.000f)
                )
            };
            
            defaults[new DefaultKey(TrainCarType.LocoDiesel, SoundType.TractionMotors)] = new AudioSettings()
            {
                clip = Resources.Load<AudioClip>("TM_DE6_01"),
                minPitch = 0.250f,
                maxPitch = 3.000f,
                pitch = 1.000f,
                pitchCurve = new AnimationCurve(), // 0 keys - empty curve
                volumeCurve = new AnimationCurve(
                    new Keyframe(0.000f, 0.000f),
                    new Keyframe(1.000f, 0.600f)
                )
            };
            
                            // LocoShunter defaults
            defaults[new DefaultKey(TrainCarType.LocoShunter, SoundType.HornHit)] = new AudioSettings()
            {
                clips = new AudioClip[] { Resources.Load<AudioClip>("Horn_LocoDE2_01_Pulse") },
                minPitch = 1.000f,
                maxPitch = 1.000f,
                pitch = 1.000f
            };
            
            defaults[new DefaultKey(TrainCarType.LocoShunter, SoundType.HornLoop)] = new AudioSettings()
            {
                clip = Resources.Load<AudioClip>("Horn_LocoDE2_01"),
                minPitch = 1.000f,
                maxPitch = 1.000f,
                pitch = 1.000f,
                pitchCurve = new AnimationCurve(), // 0 keys - empty curve
                volumeCurve = new AnimationCurve(
                    new Keyframe(0.000f, 0.000f),
                    new Keyframe(0.250f, 0.000f),
                    new Keyframe(0.617f, 0.653f),
                    new Keyframe(1.002f, 0.960f)
                )
            };
            
            defaults[new DefaultKey(TrainCarType.LocoShunter, SoundType.EngineLoop)] = new AudioSettings()
            {
                clip = Resources.Load<AudioClip>("ICE_LocoDE2_01_Idle"),
                minPitch = 1.000f,
                maxPitch = 4.000f,
                pitch = 0.760f,
                pitchCurve = new AnimationCurve(
                    new Keyframe(0.000f, 1.000f),
                    new Keyframe(0.350f, 1.400f),
                    new Keyframe(1.000f, 4.000f)
                ),
                volumeCurve = new AnimationCurve(
                    new Keyframe(0.000f, 0.000f),
                    new Keyframe(0.305f, 0.343f),
                    new Keyframe(0.450f, 0.000f),
                    new Keyframe(1.000f, 0.000f)
                )
            };
            
            defaults[new DefaultKey(TrainCarType.LocoShunter, SoundType.EngineLoadLoop)] = new AudioSettings()
            {
                clip = Resources.Load<AudioClip>("train_engine_layer_throttle"),
                minPitch = 1.000f,
                maxPitch = 4.000f,
                pitch = 0.250f,
                pitchCurve = new AnimationCurve(), // 0 keys - empty curve
                volumeCurve = new AnimationCurve(
                    new Keyframe(0.000f, 0.000f),
                    new Keyframe(0.200f, 0.325f),
                    new Keyframe(1.000f, 0.400f)
                )
            };
            
            defaults[new DefaultKey(TrainCarType.LocoShunter, SoundType.TractionMotors)] = new AudioSettings()
            {
                clip = Resources.Load<AudioClip>("TM_DE2_02"),
                minPitch = 0.400f,
                maxPitch = 3.000f,
                pitch = 1.000f,
                pitchCurve = new AnimationCurve(), // 0 keys - empty curve
                volumeCurve = new AnimationCurve(
                    new Keyframe(0.000f, 0.000f),
                    new Keyframe(1.000f, 1.000f)
                )
            };
            
            Main.mod?.Logger.Log($"CreateDefaults: Created {defaults.Count} default entries");
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"CreateDefaults: Exception occurred: {ex.Message}");
                Main.mod?.Logger.Error($"CreateDefaults: Stack trace: {ex.StackTrace}");
                // Return empty dictionary if there's an error
            }
            
            return defaults;
        }

        public static bool HasDefaults(DefaultKey key)
        {
            var defaults = CreateDefaults();
            return defaults.ContainsKey(key);
        }

        public static int GetDefaultsCount()
        {
            var defaults = CreateDefaults();
            return defaults.Count;
        }

        public static void ResetToDefault(TrainCarType carType, SoundType soundType, SoundSet soundSet, ref AudioClip clip)
        {
            var key = new DefaultKey(carType, soundType);
            var defaults = CreateDefaults();
            if (defaults.TryGetValue(key, out var defaultSettings) && defaultSettings.clip != null)
            {
                clip = defaultSettings.clip;
            }
        }

        public static void ResetToDefault(TrainCarType carType, SoundType soundType, SoundSet soundSet, ref AudioClip[] clips)
        {
            var key = new DefaultKey(carType, soundType);
            var defaults = CreateDefaults();
            if (defaults.TryGetValue(key, out var defaultSettings) && defaultSettings.clips != null)
            {
                clips = defaultSettings.clips;
            }
        }

        public static void ResetToDefault(TrainCarType carType, SoundType soundType, SoundSet soundSet, AudioSource source)
        {
            var key = new DefaultKey(carType, soundType);
            var defaults = CreateDefaults();
            if (defaults.TryGetValue(key, out var defaultSettings) && defaultSettings.clip != null)
            {
                source.clip = defaultSettings.clip;
                source.pitch = defaultSettings.pitch;
            }
        }

        public static void ResetToDefault(TrainCarType carType, SoundType soundType, SoundSet soundSet, LayeredAudio audio)
        {
            var key = new DefaultKey(carType, soundType);
            var defaults = CreateDefaults();
            
            if (defaults.TryGetValue(key, out var defaultSettings))
            {
                var mainLayer = audio.layers[0];
                
                // Stop any currently playing audio
                if (mainLayer.source.isPlaying)
                {
                    mainLayer.source.Stop();
                    audio.Set(0f);
                }
                
                // Reset all audio settings to defaults including the clip
                audio.minPitch = defaultSettings.minPitch;
                audio.maxPitch = defaultSettings.maxPitch;
                mainLayer.startPitch = defaultSettings.pitch;
                mainLayer.pitchCurve = defaultSettings.pitchCurve ?? new AnimationCurve();
                mainLayer.volumeCurve = defaultSettings.volumeCurve ?? new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));
                
                // Reset the clip to the game's original
                if (defaultSettings.clip != null)
                {
                    mainLayer.source.clip = defaultSettings.clip;
                }
                
                // Unmute all layers
                for (int i = 1; i < audio.layers.Length; i++)
                    audio.layers[i].source.mute = false;
            }
        }

        public static void Apply(TrainCarType carType, SoundType soundType, SoundSet soundSet, ref AudioClip clip)
        {
            var key = new DefaultKey(carType, soundType);
            var soundDefinition = soundSet[soundType];

            if (soundDefinition?.filename != null)
                clip = FileAudio.Load(soundDefinition.filename);
            else
            {
                var defaults = CreateDefaults();
                if (defaults.TryGetValue(key, out var defaultSettings) && defaultSettings.clip != null)
                    clip = defaultSettings.clip!;
            }
        }

        public static void Apply(TrainCarType carType, SoundType soundType, SoundSet soundSet, ref AudioClip[] clips)
        {
            var key = new DefaultKey(carType, soundType);
            var soundDefinition = soundSet[soundType];

            if (soundDefinition != null && (soundDefinition.filenames?.Length ?? 0) > 0)
                clips = soundDefinition.filenames.Select(FileAudio.Load).ToArray();
            else if (soundDefinition?.filename != null)
                clips = new AudioClip[] { FileAudio.Load(soundDefinition.filename) };
            else
            {
                var defaults = CreateDefaults();
                if (defaults.TryGetValue(key, out var defaultSettings) && defaultSettings.clips != null)
                    clips = defaultSettings.clips!;
            }
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
            var key = new DefaultKey(carType, soundType);
            var soundDefinition = soundSet[soundType];
            var mainLayer = audio.layers[0];

            // Get defaults for this key
            var defaults = CreateDefaults();
            bool hasDefaults = defaults.TryGetValue(key, out var defaultSettings);
            
            // Use default AudioSettings if no specific defaults found, with safe fallbacks
            var defaultAudioSettings = hasDefaults ? defaultSettings : new AudioSettings()
            {
                minPitch = 1.0f,
                maxPitch = 1.0f,
                pitch = 1.0f,
                pitchCurve = new AnimationCurve(),
                volumeCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f))
            };
            
            // For EngineLoop sounds, always stop the audio when applying new clips to prevent
            // unwanted playback when the engine should be off
            bool wasPlaying = mainLayer.source.isPlaying;
            AudioClip? newClip = soundDefinition?.filename.Map(FileAudio.Load) ?? defaultAudioSettings.clip;
            
            if (wasPlaying && (mainLayer.source.clip != newClip || soundType == SoundType.EngineLoop))
            {
                mainLayer.source.Stop();
                audio.Set(0f); // Ensure the LayeredAudio is properly stopped
            }
            
            if (soundDefinition == null)
            {
                audio.minPitch = defaultAudioSettings.minPitch;
                audio.maxPitch = defaultAudioSettings.maxPitch;
                mainLayer.source.clip = newClip;
                mainLayer.startPitch = defaultAudioSettings.pitch;
                mainLayer.pitchCurve = defaultAudioSettings.pitchCurve ?? AnimationCurve.Linear(0, 1, 1, 1);
                mainLayer.volumeCurve = defaultAudioSettings.volumeCurve ?? AnimationCurve.Linear(0, 1, 1, 1);
                for (int i = 1; i < audio.layers.Length; i++)
                    audio.layers[i].source.mute = false;
            }
            else
            {
                audio.minPitch = soundDefinition.minPitch ?? defaultAudioSettings.minPitch * defaultAudioSettings.pitch;
                audio.maxPitch = soundDefinition.maxPitch ?? defaultAudioSettings.maxPitch * defaultAudioSettings.pitch;
                mainLayer.source.clip = newClip;
                mainLayer.startPitch = 1f;
                
                // Ensure curves are never null before passing to MakeCurve
                var safePitchCurve = defaultAudioSettings.pitchCurve ?? new AnimationCurve();
                var safeVolumeCurve = defaultAudioSettings.volumeCurve ?? new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));
                
                mainLayer.pitchCurve = MakeCurve(safePitchCurve, soundDefinition.minPitch, soundDefinition.maxPitch);
                mainLayer.volumeCurve = MakeCurve(safeVolumeCurve, soundDefinition.minVolume, soundDefinition.maxVolume);

                for (int i = 1; i < audio.layers.Length; i++)
                    audio.layers[i].source.mute = true;
            }
            
            // For EngineLoop sounds, ensure they're stopped after applying to prevent unwanted playback
            if (soundType == SoundType.EngineLoop)
            {
                audio.Set(0f);
            }
        }

        public static void ResetAllToDefaults(TrainCar car)
        {
            ResetAllToDefaults(GetTrainAudio(car));
        }

        public static void ResetAllToDefaults(TrainAudio trainAudio)
        {
            var carType = trainAudio.car.carType;
            if (!AudioMapper.mappings.TryGetValue(carType, out var audioMapper))
            {
                return;
            }

            var defaults = CreateDefaults();

            // Create an empty soundSet for the reset
            var emptySoundSet = new SoundSet();

            // Reset all AudioClipPortReader types that actually exist in the AudioMapper
            foreach (var soundType in SoundTypes.audioClipsSoundTypes)
            {
                AudioClipPortReader? portReader = audioMapper.GetAudioClipPortReader(soundType, trainAudio);
                if (portReader != null)
                {
                    ResetToDefault(carType, soundType, emptySoundSet, ref portReader.clips);
                }
            }

            // Reset all LayeredAudio types that actually exist in the AudioMapper
            foreach (var soundType in SoundTypes.layeredAudioSoundTypes)
            {
                LayeredAudio? layeredAudio = audioMapper.GetLayeredAudio(soundType, trainAudio);
                if (layeredAudio != null)
                {
                    ResetToDefault(carType, soundType, emptySoundSet, layeredAudio);
                }
            }
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
            if (!AudioMapper.mappings.TryGetValue(carType, out var audioMapper))
                return;

            foreach (var soundType in SoundTypes.audioClipsSoundTypes)
            {
                AudioClipPortReader? portReader = audioMapper.GetAudioClipPortReader(soundType, trainAudio);
                if (portReader != null)
                    Apply(carType, soundType, soundSet, ref portReader.clips);
            }

            foreach (var soundType in SoundTypes.layeredAudioSoundTypes)
            {
                LayeredAudio? layeredAudio = audioMapper.GetLayeredAudio(soundType, trainAudio);
                if (layeredAudio != null)
                    Apply(carType, soundType, soundSet, layeredAudio);
            }
        }

        public static void ResetAndApply(TrainCar car, SoundType soundType, SoundSet soundSet)
        {
            ResetAndApply(GetTrainAudio(car), soundType, soundSet);
        }

        public static void ResetAndApply(TrainAudio trainAudio, SoundType soundType, SoundSet soundSet)
        {
            var carType = trainAudio.car.carType;
            if (!AudioMapper.mappings.TryGetValue(carType, out var audioMapper))
                return;

            var defaults = CreateDefaults();
            
            // First, remove the sound from the soundSet to ensure it uses defaults during reset
            var originalSound = soundSet[soundType];
            soundSet.Remove(soundType);

            // Reset and apply for AudioClipPortReader types
            if (SoundTypes.audioClipsSoundTypes.Contains(soundType))
            {
                AudioClipPortReader? portReader = audioMapper.GetAudioClipPortReader(soundType, trainAudio);
                if (portReader != null)
                {
                    // First reset to defaults (with sound removed from set)
                    ResetToDefault(carType, soundType, soundSet, ref portReader.clips);
                    
                    // Re-add the sound to the soundSet
                    if (originalSound != null)
                        originalSound.Apply(soundSet);
                    
                    // Then apply new sound
                    Apply(carType, soundType, soundSet, ref portReader.clips);
                }
            }

            // Reset and apply for LayeredAudio types
            if (SoundTypes.layeredAudioSoundTypes.Contains(soundType))
            {
                LayeredAudio? layeredAudio = audioMapper.GetLayeredAudio(soundType, trainAudio);
                if (layeredAudio != null)
                {
                    // First reset to defaults (with sound removed from set)
                    ResetToDefault(carType, soundType, soundSet, layeredAudio);
                    
                    // Re-add the sound to the soundSet
                    if (originalSound != null)
                        originalSound.Apply(soundSet);
                    
                    // Then apply new sound
                    Apply(carType, soundType, soundSet, layeredAudio);
                }
            }
        }
    }
}
