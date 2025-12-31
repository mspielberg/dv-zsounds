using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DvMod.ZSounds
{
    public class SoundSet
    {
        public readonly Dictionary<SoundType, SoundDefinition> sounds = new Dictionary<SoundType, SoundDefinition>();

        // Storage for generic/unknown sounds by their clip name
        public readonly Dictionary<string, SoundDefinition> genericSounds = new Dictionary<string, SoundDefinition>();

        public SoundDefinition? this[SoundType type]
        {
            get => sounds.TryGetValue(type, out var soundDefinition) ? soundDefinition : null;
        }

        public SoundDefinition? GetGenericSound(string soundName)
        {
            return genericSounds.TryGetValue(soundName, out var soundDefinition) ? soundDefinition : null;
        }

        public void SetGenericSound(string soundName, SoundDefinition soundDefinition)
        {
            genericSounds[soundName] = soundDefinition;
        }

        public void Remove(SoundType type)
        {
            sounds.Remove(type);
        }

        public void RemoveGenericSound(string soundName)
        {
            genericSounds.Remove(soundName);
        }

        public override string ToString()
        {
            var normalSounds = string.Join("\n", sounds.Select(kv => $"{kv.Key}: {kv.Value}"));
            var genericSoundsList = string.Join("\n", genericSounds.Select(kv => $"Generic '{kv.Key}': {kv.Value}"));
            return normalSounds + (genericSounds.Count > 0 ? "\n" + genericSoundsList : "");
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
        TransmissionEngaged,
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
        SteamChuff8HzWater,
        SteamChuff16HzWater,
        // Ash chuffs
        SteamChuff2HzAsh,
        SteamChuff4HzAsh,
        SteamChuff8HzAsh,
        // Additional diesel/electric sounds
        EnginePiston,
        TMOverspeed,
        TMController,
        SandFlow,
        CabFan,
        ContactorOn,
        ContactorOff,
        DBBlower,
        TMBlow,
        FluidCoupler,
        HydroDynamicBrake,
        ActiveCooler,
        GearChange,
        GearGrind,
        JakeBrake,
        // Steam locomotive sounds
        Fire,
        WindFirebox,
        SteamRelease,
        SteamChestAdmission,
        WaterInFlow,
        DamagedMechanism,
        NoOilOilingPoints,
        CrownSheetBoiling,
        BellPump,
        Lubricator,
        PrimingCrank,
        SteamCylinderCrack,
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
            SoundType.TransmissionEngaged,
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
            // Additional diesel/electric sounds
            SoundType.EnginePiston,
            SoundType.TMOverspeed,
            SoundType.TMController,
            SoundType.SandFlow,
            SoundType.CabFan,
            SoundType.DBBlower,
            SoundType.FluidCoupler,
            SoundType.HydroDynamicBrake,
            SoundType.ActiveCooler,
            SoundType.JakeBrake,
            // Steam locomotive sounds
            SoundType.Fire,
            SoundType.WindFirebox,
            SoundType.SteamRelease,
            SoundType.SteamChestAdmission,
            SoundType.WaterInFlow,
            SoundType.DamagedMechanism,
            SoundType.NoOilOilingPoints,
            SoundType.CrownSheetBoiling,
            SoundType.BellPump,
            SoundType.Lubricator,
            SoundType.PrimingCrank,
        ];

        public static readonly SoundType[] audioClipsSoundTypes =
        [
            SoundType.HornHit,
            SoundType.EngineShutdown,
            SoundType.ContactorOn,
            SoundType.ContactorOff,
            SoundType.TMBlow,
            SoundType.GearChange,
            SoundType.GearGrind,
            SoundType.SteamCylinderCrack,
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
        public bool? randomizeStartTime;

        // Animation curve properties
        public AnimationCurve? pitchCurve;
        public AnimationCurve? volumeCurve;

        // Path to the configuration file for this sound
        public string? configPath;

        public SoundDefinition(string name, SoundType type)
        {
            this.name = name;
            this.type = type;
        }

        public void Apply(SoundSet soundSet)
        {
            soundSet.sounds[type] = this;
        }

        public override string ToString()
        {
            return $"{name} ({filename})";
        }
    }
}
