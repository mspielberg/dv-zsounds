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

        private static Dictionary<DefaultKey, AudioSettings> CreateDefaults()
        {
            var defaults = new Dictionary<DefaultKey, AudioSettings>();

            try
            {
                // LocoMicroshunter defaults
                defaults[new DefaultKey(TrainCarType.LocoMicroshunter, SoundType.HornLoop)] = CreateAudioSettings(
                    Resources.Load<AudioClip>("Horn_Microshunter_01"),
                    minPitch: 1.000f, maxPitch: 1.000f, pitch: 1.000f,
                    pitchCurve: new AnimationCurve(),
                    volumeCurve: new AnimationCurve(
                        new Keyframe(0.000f, 0.000f),
                        new Keyframe(0.395f, 0.000f),
                        new Keyframe(0.500f, 0.499f),
                        new Keyframe(1.000f, 0.499f)
                    )
                );

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

                // Dynamo defaults for diesel/electric locomotives
                defaults[new DefaultKey(TrainCarType.LocoShunter, SoundType.Dynamo)] = new AudioSettings()
                {
                    clip = Resources.Load<AudioClip>("Dynamo_DE2_01") ?? Resources.Load<AudioClip>("TM_DE2_02"), // Fallback to traction motor sound
                    minPitch = 0.800f,
                    maxPitch = 2.000f,
                    pitch = 1.000f,
                    pitchCurve = new AnimationCurve(),
                    volumeCurve = new AnimationCurve(
                        new Keyframe(0.000f, 0.200f),
                        new Keyframe(0.500f, 0.600f),
                        new Keyframe(1.000f, 0.800f)
                    )
                };

                defaults[new DefaultKey(TrainCarType.LocoDiesel, SoundType.Dynamo)] = new AudioSettings()
                {
                    clip = Resources.Load<AudioClip>("Dynamo_DE6_01") ?? Resources.Load<AudioClip>("TM_DE6_02"),
                    minPitch = 0.800f,
                    maxPitch = 2.000f,
                    pitch = 1.000f,
                    pitchCurve = new AnimationCurve(),
                    volumeCurve = new AnimationCurve(
                        new Keyframe(0.000f, 0.200f),
                        new Keyframe(0.500f, 0.600f),
                        new Keyframe(1.000f, 0.800f)
                    )
                };

                defaults[new DefaultKey(TrainCarType.LocoMicroshunter, SoundType.Dynamo)] = new AudioSettings()
                {
                    clip = Resources.Load<AudioClip>("Dynamo_E2_01") ?? Resources.Load<AudioClip>("TM_E2_01"),
                    minPitch = 0.800f,
                    maxPitch = 2.000f,
                    pitch = 1.000f,
                    pitchCurve = new AnimationCurve(),
                    volumeCurve = new AnimationCurve(
                        new Keyframe(0.000f, 0.200f),
                        new Keyframe(0.500f, 0.600f),
                        new Keyframe(1.000f, 0.800f)
                    )
                };

                // Steam locomotive dynamo defaults
                defaults[new DefaultKey(TrainCarType.LocoS060, SoundType.Dynamo)] = new AudioSettings()
                {
                    clip = Resources.Load<AudioClip>("Dynamo_Steam_01") ?? Resources.Load<AudioClip>("Dynamo_E2_01"),
                    minPitch = 0.800f,
                    maxPitch = 1.800f,
                    pitch = 1.000f,
                    pitchCurve = new AnimationCurve(),
                    volumeCurve = new AnimationCurve(
                        new Keyframe(0.000f, 0.100f),
                        new Keyframe(0.400f, 0.500f),
                        new Keyframe(1.000f, 0.700f)
                    )
                };

                defaults[new DefaultKey(TrainCarType.LocoSteamHeavy, SoundType.Dynamo)] = new AudioSettings()
                {
                    clip = Resources.Load<AudioClip>("Dynamo_Steam_Heavy_01") ?? Resources.Load<AudioClip>("Dynamo_Steam_01"),
                    minPitch = 0.800f,
                    maxPitch = 1.800f,
                    pitch = 1.000f,
                    pitchCurve = new AnimationCurve(),
                    volumeCurve = new AnimationCurve(
                        new Keyframe(0.000f, 0.100f),
                        new Keyframe(0.400f, 0.500f),
                        new Keyframe(1.000f, 0.700f)
                    )
                };

                // Air Compressor defaults for all locomotives
                defaults[new DefaultKey(TrainCarType.LocoShunter, SoundType.AirCompressor)] = new AudioSettings()
                {
                    clip = Resources.Load<AudioClip>("AirCompressor_01") ?? Resources.Load<AudioClip>("TM_DE2_02"), // Fallback
                    minPitch = 0.900f,
                    maxPitch = 1.100f,
                    pitch = 1.000f,
                    pitchCurve = new AnimationCurve(),
                    volumeCurve = new AnimationCurve(
                        new Keyframe(0.000f, 0.000f),
                        new Keyframe(0.100f, 0.800f),
                        new Keyframe(0.900f, 0.800f),
                        new Keyframe(1.000f, 0.000f)
                    )
                };

                defaults[new DefaultKey(TrainCarType.LocoDiesel, SoundType.AirCompressor)] = new AudioSettings()
                {
                    clip = Resources.Load<AudioClip>("AirCompressor_01") ?? Resources.Load<AudioClip>("TM_DE6_02"),
                    minPitch = 0.900f,
                    maxPitch = 1.100f,
                    pitch = 1.000f,
                    pitchCurve = new AnimationCurve(),
                    volumeCurve = new AnimationCurve(
                        new Keyframe(0.000f, 0.000f),
                        new Keyframe(0.100f, 0.800f),
                        new Keyframe(0.900f, 0.800f),
                        new Keyframe(1.000f, 0.000f)
                    )
                };

                defaults[new DefaultKey(TrainCarType.LocoMicroshunter, SoundType.AirCompressor)] = new AudioSettings()
                {
                    clip = Resources.Load<AudioClip>("AirCompressor_01") ?? Resources.Load<AudioClip>("TM_E2_01"),
                    minPitch = 0.900f,
                    maxPitch = 1.100f,
                    pitch = 1.000f,
                    pitchCurve = new AnimationCurve(),
                    volumeCurve = new AnimationCurve(
                        new Keyframe(0.000f, 0.000f),
                        new Keyframe(0.100f, 0.800f),
                        new Keyframe(0.900f, 0.800f),
                        new Keyframe(1.000f, 0.000f)
                    )
                };

                defaults[new DefaultKey(TrainCarType.LocoS060, SoundType.AirCompressor)] = new AudioSettings()
                {
                    clip = Resources.Load<AudioClip>("AirCompressor_Steam_01") ?? Resources.Load<AudioClip>("AirCompressor_01"),
                    minPitch = 0.900f,
                    maxPitch = 1.100f,
                    pitch = 1.000f,
                    pitchCurve = new AnimationCurve(),
                    volumeCurve = new AnimationCurve(
                        new Keyframe(0.000f, 0.000f),
                        new Keyframe(0.100f, 0.800f),
                        new Keyframe(0.900f, 0.800f),
                        new Keyframe(1.000f, 0.000f)
                    )
                };

                defaults[new DefaultKey(TrainCarType.LocoSteamHeavy, SoundType.AirCompressor)] = new AudioSettings()
                {
                    clip = Resources.Load<AudioClip>("AirCompressor_Steam_01") ?? Resources.Load<AudioClip>("AirCompressor_01"),
                    minPitch = 0.900f,
                    maxPitch = 1.100f,
                    pitch = 1.000f,
                    pitchCurve = new AnimationCurve(),
                    volumeCurve = new AnimationCurve(
                        new Keyframe(0.000f, 0.000f),
                        new Keyframe(0.100f, 0.800f),
                        new Keyframe(0.900f, 0.800f),
                        new Keyframe(1.000f, 0.000f)
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

        public static Dictionary<DefaultKey, AudioSettings> GetDefaults()
        {
            return CreateDefaults();
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
            Main.DebugLog(() => $"AudioUtils.Apply: Processing single AudioClip for {carType}/{soundType}");

            var key = new DefaultKey(carType, soundType);
            var soundDefinition = soundSet[soundType];

            if (soundDefinition?.filename != null)
            {
                Main.DebugLog(() => $"AudioUtils.Apply: Using custom filename for {soundType}: {soundDefinition.filename}");
                clip = FileAudio.Load(soundDefinition.filename);
            }
            else
            {
                Main.DebugLog(() => $"AudioUtils.Apply: No custom sound found for {soundType}, checking defaults...");
                var defaults = CreateDefaults();
                if (defaults.TryGetValue(key, out var defaultSettings) && defaultSettings.clip != null)
                {
                    Main.DebugLog(() => $"AudioUtils.Apply: Using default clip for {soundType}");
                    clip = defaultSettings.clip!;
                }
                else
                {
                    Main.DebugLog(() => $"AudioUtils.Apply: No defaults found for {soundType}, keeping original clip");
                }
            }

            var clipName = clip?.name ?? "null";
            Main.DebugLog(() => $"AudioUtils.Apply: Final clip for {soundType}: {clipName}");
        }

        public static void Apply(TrainCarType carType, SoundType soundType, SoundSet soundSet, ref AudioClip[] clips)
        {
            Main.mod?.Logger.Log($"AudioUtils.Apply: *** PROCESSING AUDIOCLIP[] *** for {carType}/{soundType}");

            var key = new DefaultKey(carType, soundType);
            var soundDefinition = soundSet[soundType];

            Main.mod?.Logger.Log($"AudioUtils.Apply: Sound definition for {soundType}: {soundDefinition?.name ?? "NULL"}");

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
                Main.DebugLog(() => $"AudioUtils.Apply: No custom sounds found for {soundType}, checking defaults...");
                var defaults = CreateDefaults();
                if (defaults.TryGetValue(key, out var defaultSettings) && defaultSettings.clips != null)
                {
                    Main.DebugLog(() => $"AudioUtils.Apply: Using default clips for {soundType}");
                    clips = defaultSettings.clips!;
                }
                else
                {
                    Main.DebugLog(() => $"AudioUtils.Apply: No defaults found for {soundType}, keeping original clips");
                }
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

            var key = new DefaultKey(carType, soundType);
            var soundDefinition = soundSet[soundType];
            var mainLayer = audio.layers[0];

            Main.DebugLog(() => $"AudioUtils.Apply: Sound definition found for {soundType}: {soundDefinition != null}");

            // Get defaults for this key
            var defaults = CreateDefaults();
            bool hasDefaults = defaults.TryGetValue(key, out var defaultSettings);

            Main.DebugLog(() => $"AudioUtils.Apply: Has defaults for {soundType}: {hasDefaults}");

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
            AudioClip? newClip = soundDefinition?.filename != null ? FileAudio.Load(soundDefinition.filename) : defaultAudioSettings.clip;

            var newClipName = newClip?.name ?? "null";
            Main.DebugLog(() => $"AudioUtils.Apply: New clip for {soundType}: {newClipName}");

            if (wasPlaying && (mainLayer.source.clip != newClip || soundType == SoundType.EngineLoop))
            {
                mainLayer.source.Stop();
                audio.Set(0f); // Ensure the LayeredAudio is properly stopped
            }

            if (soundDefinition == null)
            {
                Main.DebugLog(() => $"AudioUtils.Apply: Applying defaults for {soundType}");
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
                Main.DebugLog(() => $"AudioUtils.Apply: Applying custom sound definition for {soundType}");

                // Apply pitch - use custom pitch if specified, otherwise use default
                var basePitch = soundDefinition.pitch ?? defaultAudioSettings.pitch;

                audio.minPitch = soundDefinition.minPitch ?? defaultAudioSettings.minPitch * basePitch;
                audio.maxPitch = soundDefinition.maxPitch ?? defaultAudioSettings.maxPitch * basePitch;
                mainLayer.source.clip = newClip;
                mainLayer.startPitch = basePitch;

                Main.DebugLog(() => $"AudioUtils.Apply: Applied pitch settings for {soundType} - startPitch: {basePitch}, minPitch: {audio.minPitch}, maxPitch: {audio.maxPitch}");

                // Use custom curves if available, otherwise use defaults
                var basePitchCurve = soundDefinition.pitchCurve ?? defaultAudioSettings.pitchCurve ?? new AnimationCurve();
                var baseVolumeCurve = soundDefinition.volumeCurve ?? defaultAudioSettings.volumeCurve ?? new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));

                mainLayer.pitchCurve = MakeCurve(basePitchCurve, soundDefinition.minPitch, soundDefinition.maxPitch);
                mainLayer.volumeCurve = MakeCurve(baseVolumeCurve, soundDefinition.minVolume, soundDefinition.maxVolume);

                Main.DebugLog(() => $"AudioUtils.Apply: Applied custom curves for {soundType} - PitchCurve: {(soundDefinition.pitchCurve != null ? "Custom" : "Default")}, VolumeCurve: {(soundDefinition.volumeCurve != null ? "Custom" : "Default")}");

                for (int i = 1; i < audio.layers.Length; i++)
                    audio.layers[i].source.mute = true;
            }

            // For EngineLoop sounds, ensure they're stopped after applying to prevent unwanted playback
            if (soundType == SoundType.EngineLoop)
            {
                audio.Set(0f);
            }

            var finalClipName = mainLayer.source.clip?.name ?? "null";
            Main.DebugLog(() => $"AudioUtils.Apply: LayeredAudio application completed for {soundType}. Final clip: {finalClipName}");
        }

        public static void ResetAllToDefaults(TrainCar car)
        {
            ResetAllToDefaults(GetTrainAudio(car));
        }

        public static void ResetAllToDefaults(TrainAudio trainAudio)
        {
            var carType = trainAudio.car.carType;
            if (!AudioMapper.Mappers.TryGetValue(carType, out var audioMapper))
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
            if (!AudioMapper.Mappers.TryGetValue(carType, out var audioMapper))
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
            if (!AudioMapper.Mappers.TryGetValue(carType, out var audioMapper))
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