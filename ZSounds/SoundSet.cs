using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace DvMod.ZSounds
{
    public class SoundSet
    {
        public readonly Dictionary<SoundType, SoundDefinition> sounds = new Dictionary<SoundType, SoundDefinition>();

        public SoundDefinition? this[SoundType type]
        {
            get => sounds.TryGetValue(type, out var soundDefinition) ? soundDefinition : null;
        }

        public void Remove(SoundType type)
        {
            sounds.Remove(type);
        }

        public override string ToString()
        {
            return string.Join("\n", sounds.Select(kv => $"{kv.Key}: {kv.Value}"));
        }
    }

    public enum SoundType
    {
        Unknown = 0,
        HornHit,
        HornLoop,
        Whistle,
        Bell,
        EngineLoop,
        EngineLoadLoop,
        EngineStartup,
        EngineShutdown,
        TractionMotors,
        SteamCylinderChuffs,
        SteamStackChuffs,
        SteamValveGear,
        SteamChuffLoop,
        Dynamo,
        AirCompressor,
        // Steam chuff frequencies
        SteamChuff2_67Hz,
        SteamChuff3Hz,
        SteamChuff4Hz,
        SteamChuff5_33Hz,
        SteamChuff8Hz,
        SteamChuff10_67Hz,
        SteamChuff16Hz,
        // Water chuffs
        SteamChuff4HzWater,
        SteamChuff16HzWater,
        // Ash chuffs
        SteamChuff2HzAsh,
        SteamChuff4HzAsh,
        SteamChuff8HzAsh,
        SteamChuff8HzWater,
    }

    public static class SoundTypes
    {
        public static readonly SoundType[] layeredAudioSoundTypes =
        [
            SoundType.HornLoop,
            SoundType.Whistle,
            SoundType.Bell,
            SoundType.EngineLoop,
            SoundType.EngineLoadLoop,
            SoundType.EngineStartup,
            SoundType.TractionMotors,
            SoundType.SteamCylinderChuffs,
            SoundType.SteamStackChuffs,
            SoundType.SteamValveGear,
            SoundType.SteamChuffLoop,
            SoundType.Dynamo,
            SoundType.AirCompressor,
            // Steam chuff frequencies
            SoundType.SteamChuff2_67Hz,
            SoundType.SteamChuff3Hz,
            SoundType.SteamChuff4Hz,
            SoundType.SteamChuff5_33Hz,
            SoundType.SteamChuff8Hz,
            SoundType.SteamChuff10_67Hz,
            SoundType.SteamChuff16Hz,
            // Water chuffs
            SoundType.SteamChuff4HzWater,
            SoundType.SteamChuff16HzWater,
            SoundType.SteamChuff8HzWater,
            // Ash chuffs
            SoundType.SteamChuff2HzAsh,
            SoundType.SteamChuff4HzAsh,
            SoundType.SteamChuff8HzAsh,
        ];

        public static readonly SoundType[] audioClipsSoundTypes =
        [
            SoundType.HornHit,
            SoundType.EngineShutdown,
        ];
    }

    public class SoundDefinition
    {
        public string name;
        public SoundType type;
        public string? filename;
        public string[]? filenames;
        public float? pitch;
        public float? minPitch;
        public float? maxPitch;
        public float? minVolume;
        public float? maxVolume;
        public float? fadeStart;
        public float? fadeDuration;

        // Animation curve properties
        public AnimationCurve? pitchCurve;
        public AnimationCurve? volumeCurve;

        public SoundDefinition(string name, SoundType type)
        {
            this.name = name;
            this.type = type;
        }

        public void Apply(SoundSet soundSet)
        {
            soundSet.sounds[type] = this;
        }

        public void Validate()
        {
            static void ValidateFile(string f) => FileAudio.Load(f);
            if (filename != null)
                ValidateFile(filename);
            foreach (var f in filenames ?? new string[0])
                ValidateFile(f);
        }

        public override string ToString()
        {
            return $"{name} ({filename})";
        }
    }
}