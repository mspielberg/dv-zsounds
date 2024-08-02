using DV.ThingTypes;
using System.Collections.Generic;
using System.Linq;
using DvMod.ZSounds.Config;
using UnityEngine;

namespace DvMod.ZSounds
{
    public static class AudioUtils
    {
        private struct AudioSettings
        {
            public AudioClip clip;
            public AudioClip[] clips;
            public float pitch;
            public float minPitch;
            public float maxPitch;
            public AnimationCurve pitchCurve;
            public AnimationCurve volumeCurve;

            public override string ToString()
            {
                return $"clip={clip?.length},clips={clips?.Length},pitch={pitch},minPitch={minPitch},maxPitch={maxPitch}";
            }
        }

        private struct DefaultKey
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

        private static readonly Dictionary<DefaultKey, AudioSettings> Defaults = new Dictionary<DefaultKey, AudioSettings>();

        public static void Apply(TrainCarType carType, SoundType soundType, SoundSet soundSet, ref AudioClip clip)
        {
            var key = new DefaultKey(carType, soundType);
            var soundDefinition = soundSet[soundType];
            Main.DebugLog(() => $"Loading {key}: {soundDefinition}");
            if (!Defaults.ContainsKey(key))
                Defaults[key] = new AudioSettings() { clip = clip };

            if (soundDefinition?.filename != null)
                clip = FileAudio.Load(soundDefinition.filename);
            else
                clip = Defaults[key].clip!;
        }

        public static void Apply(TrainCarType carType, SoundType soundType, SoundSet soundSet, ref AudioClip[] clips)
        {
            var key = new DefaultKey(carType, soundType);
            var soundDefinition = soundSet[soundType];
            Main.DebugLog(() => $"Loading {key}: {soundDefinition}");
            if (!Defaults.ContainsKey(key))
                Defaults[key] = new AudioSettings() { clips = clips };

            if (soundDefinition != null && (soundDefinition.filenames?.Length ?? 0) > 0)
                clips = soundDefinition.filenames.Select(FileAudio.Load).ToArray();
            else if (soundDefinition?.filename != null)
                clips = new AudioClip[] { FileAudio.Load(soundDefinition.filename) };
            else
                clips = Defaults[key].clips!;
        }

        public static void Apply(TrainCarType carType, SoundType soundType, SoundSet soundSet, AudioSource source)
        {
            var key = new DefaultKey(carType, soundType);
            var soundDefinition = soundSet[soundType];
            Main.DebugLog(() => $"Loading {key}: {soundDefinition}");
            if (!Defaults.ContainsKey(key))
            {
                Defaults[key] = new AudioSettings()
                {
                    clip = source.clip,
                    pitch = source.pitch,
                };
            }

            var defaults = Defaults[key];
            if (soundDefinition == null)
            {
                source.clip = defaults.clip;
                source.pitch = defaults.pitch;
                return;
            }

            source.clip = soundDefinition.filename.Map(FileAudio.Load) ?? defaults.clip;
            source.pitch = soundDefinition.pitch ?? defaults.pitch;
        }

        private static AnimationCurve MakeCurve(AnimationCurve defaultCurve, float? newMin, float? newMax)
        {
            // Main.DebugLog(() => "Entering MakeCurve");
            // Main.DebugLog(() => $"newMin={newMin}, newMax={newMax}");
            // Main.DebugLog(() => $"defaultCurve={defaultCurve}");
            // Main.DebugLog(() => $"keys={string.Join(",", defaultCurve.keys)}");
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
            Main.DebugLog(() => $"Loading {key}: {soundDefinition}");
            var mainLayer = audio.layers[0];
            if (!Defaults.ContainsKey(key))
            {
                Defaults[key] = new AudioSettings()
                {
                    clip = mainLayer.source.clip,
                    pitch = mainLayer.startPitch,
                    minPitch = audio.minPitch,
                    maxPitch = audio.maxPitch,
                    pitchCurve = mainLayer.pitchCurve,
                    volumeCurve = mainLayer.volumeCurve,
                };
                Main.DebugLog(() => $"Saved default settings for {key}: {Defaults[key]}");
            }

            var defaults = Defaults[key];
            if (soundDefinition == null)
            {
                audio.minPitch = defaults.minPitch!;
                audio.maxPitch = defaults.maxPitch!;
                mainLayer.source.clip = defaults.clip;
                mainLayer.startPitch = defaults.pitch;
                mainLayer.pitchCurve = defaults.pitchCurve;
                mainLayer.volumeCurve = defaults.volumeCurve;
                for (int i = 1; i < audio.layers.Length; i++)
                    audio.layers[i].source.mute = false;
            }
            else
            {
                audio.minPitch = soundDefinition.minPitch ?? defaults.minPitch * defaults.pitch;
                audio.maxPitch = soundDefinition.maxPitch ?? defaults.maxPitch * defaults.pitch;
                mainLayer.source.clip = soundDefinition.filename.Map(FileAudio.Load) ?? defaults.clip;
                mainLayer.startPitch = 1f;
                mainLayer.pitchCurve = MakeCurve(defaults.pitchCurve, soundDefinition.minPitch, soundDefinition.maxPitch);
                mainLayer.volumeCurve = MakeCurve(defaults.volumeCurve, soundDefinition.minVolume, soundDefinition.maxVolume);

                for (int i = 1; i < audio.layers.Length; i++)
                    audio.layers[i].source.mute = true;
            }
        }
    }
}
