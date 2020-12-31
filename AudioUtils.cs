using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DvMod.ZSounds
{
    public static class AudioUtils
    {
        private struct AudioSettings
        {
            public AudioClip clip;
            public float[] startPitches;
        }

        private static readonly Dictionary<string, AudioSettings> Defaults = new Dictionary<string, AudioSettings>();
        public static void SetClip(string tag, LayeredAudio audio, string? name, float startPitch)
        {
            if (!Defaults.ContainsKey(tag))
            {
                Defaults[tag] = new AudioSettings()
                {
                    clip = audio.layers[0].source.clip,
                    startPitches = audio.layers.Select(x => x.startPitch).ToArray(),
                };
            }

            if (name == null)
            {
                var defaults = Defaults[tag];
                audio.layers[0].source.clip = defaults.clip;
                audio.layers[0].startPitch = defaults.startPitches[0] * startPitch;
                for (int i = 1; i < audio.layers.Length; i++)
                {
                    audio.layers[i].source.mute = false;
                    audio.layers[i].startPitch = defaults.startPitches[i] * startPitch;
                }
            }
            else
            {
                audio.layers[0].source.clip = FileAudio.Load(name);
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
                clip = Defaults[tag].clip;
            else
                clip = FileAudio.Load(name);
        }
    }
}