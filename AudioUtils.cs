using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        }

        private static readonly Dictionary<string, AudioSettings> Defaults = new Dictionary<string, AudioSettings>();
        public static void SetClip(string tag, LayeredAudio audio, string? name, bool enabled, float startPitch)
        {
            if (!Defaults.ContainsKey(tag))
            {
                Defaults[tag] = new AudioSettings()
                {
                    clip = audio.layers[0].source.clip,
                    pitch = audio.layers[0].startPitch,
                };
            }

            if (!enabled)
            {
                foreach (var layer in audio.layers)
                    layer.source.mute = true;
            }
            else if (name == null)
            {
                var defaults = Defaults[tag];
                audio.layers[0].source.clip = defaults.clip;
                audio.layers[0].source.mute = false;
                audio.layers[0].startPitch = defaults.pitch * startPitch;
                for (int i = 1; i < audio.layers.Length; i++)
                    audio.layers[i].source.mute = false;
            }
            else
            {
                audio.layers[0].source.clip = FileAudio.Load(Path.Combine(Main.mod!.Path, name));
                audio.layers[0].source.mute = false;
                audio.layers[0].startPitch = startPitch;
                for (int i = 1; i < audio.layers.Length; i++)
                    audio.layers[i].source.mute = true;
            }
        }

        public static void SetClip(string tag, ref AudioClip clip, string? name, bool enabled)
        {
            if (!Defaults.ContainsKey(tag))
                Defaults[tag] = new AudioSettings() { clip = clip };

            if (!enabled)
                clip = FileAudio.Silent;
            else if (name == null)
                clip = Defaults[tag].clip!;
            else
                clip = FileAudio.Load(Path.Combine(Main.mod!.Path, name));
        }

        public static void Apply(Config.SoundDefinition? soundDefinition, string tag, ref AudioClip clip)
        {
            if (!Defaults.ContainsKey(tag))
                Defaults[tag] = new AudioSettings() { clip = clip };

            if (soundDefinition == null)
                clip = Defaults[tag].clip!;
            else if (soundDefinition.filename != null)
                clip = FileAudio.Load(soundDefinition.filename);
            else
                clip = FileAudio.Silent;
        }

        public static void Apply(Config.SoundDefinition? soundDefinition, string tag, ref AudioClip[] clips)
        {
            if (!Defaults.ContainsKey(tag))
                Defaults[tag] = new AudioSettings() { clips = clips };

            if (soundDefinition == null)
                clips = Defaults[tag].clips!;
            else if ((soundDefinition.filenames?.Length ?? 0) > 0)
                clips = soundDefinition.filenames.Select(FileAudio.Load).ToArray();
            else if (soundDefinition.filename != null)
                clips = new AudioClip[] { FileAudio.Load(soundDefinition.filename) };
            else
                clips = new AudioClip[] { FileAudio.Silent };
        }

        public static void Apply(Config.SoundDefinition? soundDefinition, string tag, AudioSource source)
        {
            if (!Defaults.ContainsKey(tag))
            {
                Defaults[tag] = new AudioSettings()
                {
                    clip = source.clip,
                    pitch = source.pitch,
                };
            }

            var defaults = Defaults[tag];
            if (soundDefinition == null)
            {
                source.clip = defaults.clip;
                source.pitch = defaults.pitch;
                return;
            }

            source.clip = soundDefinition.filename.Map(FileAudio.Load) ?? FileAudio.Silent;
            source.pitch = soundDefinition.pitch ?? defaults.pitch;
        }

        public static void Apply(Config.SoundDefinition? soundDefinition, string tag, LayeredAudio audio)
        {
            Main.DebugLog(() => $"Loading {soundDefinition}");
            var mainLayer = audio.layers[0];
            if (!Defaults.ContainsKey(tag))
            {
                Defaults[tag] = new AudioSettings()
                {
                    clip = mainLayer.source.clip,
                    pitch = mainLayer.startPitch,
                    minPitch = audio.minPitch,
                    maxPitch = audio.maxPitch,
                };
            }

            var defaults = Defaults[tag];
            if (soundDefinition == null)
            {
                audio.minPitch = defaults.minPitch!;
                audio.maxPitch = defaults.maxPitch!;
                mainLayer.source.clip = defaults.clip;
                mainLayer.startPitch = defaults.pitch;
                for (int i = 1; i < audio.layers.Length; i++)
                    audio.layers[i].source.mute = false;
            }
            else
            {
                audio.minPitch = soundDefinition.minPitch ?? defaults.minPitch;
                audio.maxPitch = soundDefinition.maxPitch ?? defaults.maxPitch;
                mainLayer.source.clip = soundDefinition.filename.Map(FileAudio.Load) ?? defaults.clip;
                mainLayer.startPitch = soundDefinition.pitch ?? defaults.pitch;

                for (int i = 1; i < audio.layers.Length; i++)
                    audio.layers[i].source.mute = true;
            }
        }
    }
}