using Newtonsoft.Json;
using UnityEngine;

namespace DvMod.ZSounds
{
    public class SoundConfiguration
    {
        // If true (or null for backward compatibility), this config is applied to the sound
        // If false, the config file exists but is ignored (uses defaults)
        public bool? enabled;

        public float? pitch;
        public float? minPitch;
        public float? maxPitch;
        public float? minVolume;
        public float? maxVolume;
        public bool? randomizeStartTime;

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
                curve.AddKey(new Keyframe(kf.time, kf.value));
            }
            return curve;
        }
    }

    public class KeyframeData
    {
        public float time;
        public float value;

        public KeyframeData() { }

        public KeyframeData(float time, float value, float? inTangent = null, float? outTangent = null)
        {
            this.time = time;
            this.value = value;
        }
    }
}
