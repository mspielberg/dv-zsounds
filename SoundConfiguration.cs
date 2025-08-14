using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UnityEngine;

namespace DvMod.ZSounds
{
    public class SoundConfiguration
    {
        public float? pitch;
        public float? minPitch;
        public float? maxPitch;
        public float? minVolume;
        public float? maxVolume;
        public float? fadeStart;
        public float? fadeDuration;

        [JsonProperty("pitchCurve")]
        public KeyframeData[]? pitchCurveData;

        [JsonProperty("volumeCurve")]
        public KeyframeData[]? volumeCurveData;

        [JsonIgnore]
        public AnimationCurve? PitchCurve => CreateAnimationCurve(pitchCurveData);

        [JsonIgnore]
        public AnimationCurve? VolumeCurve => CreateAnimationCurve(volumeCurveData);

        private static AnimationCurve? CreateAnimationCurve(KeyframeData[]? keyframes)
        {
            if (keyframes == null || keyframes.Length == 0)
                return null;

            var curve = new AnimationCurve();
            foreach (var kf in keyframes)
            {
                curve.AddKey(new Keyframe(kf.time, kf.value, kf.inTangent ?? 0f, kf.outTangent ?? 0f));
            }
            return curve;
        }
    }

    public class KeyframeData
    {
        public float time;
        public float value;
        public float? inTangent;
        public float? outTangent;

        public KeyframeData() { }

        public KeyframeData(float time, float value, float? inTangent = null, float? outTangent = null)
        {
            this.time = time;
            this.value = value;
            this.inTangent = inTangent;
            this.outTangent = outTangent;
        }
    }

    public static class SoundConfigurationLoader
    {
        private const string CONFIG_FILENAME = "config.json";

        public static SoundConfiguration? LoadConfiguration(string soundTypeFolder)
        {
            var configPath = Path.Combine(soundTypeFolder, CONFIG_FILENAME);

            if (!File.Exists(configPath))
            {
                Main.DebugLog(() => $"No config.json found in {soundTypeFolder}");
                return null;
            }

            try
            {
                var jsonContent = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<SoundConfiguration>(jsonContent);

                Main.DebugLog(() => $"Loaded sound configuration from {configPath}");
                return config;
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to load sound configuration from {configPath}: {ex.Message}");
                return null;
            }
        }
    }
}