using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DvMod.ZSounds
{
    public static class LayeredAudioUtils
    {
        private struct AudioSettings
        {
            public AudioClip clip;
            public float[] startPitches;
        }

        private static readonly Dictionary<LayeredAudio, AudioSettings> Defaults = new Dictionary<LayeredAudio, AudioSettings>();
        public static void SetClip(LayeredAudio audio, string? name, float startPitch)
        {
            if (!Defaults.ContainsKey(audio))
            {
                Defaults[audio] = new AudioSettings()
                {
                    clip = audio.layers[0].source.clip,
                    startPitches = audio.layers.Select(x => x.startPitch).ToArray(),
                };
            }

            if (name == null)
            {
                var defaults = Defaults[audio];
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
    }
}