using DV.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DvMod.ZSounds.Config
{
    public class SoundSet
    {
        public readonly Dictionary<SoundType, SoundDefinition> sounds = new Dictionary<SoundType, SoundDefinition>();

        public JObject ToJson()
        {
            var obj = new JObject();
            foreach (var (type, sound) in sounds)
                obj.Add(type.ToString(), sound.name);
            return obj;
        }

        public SoundDefinition? this[SoundType type]
        {
            get => sounds.TryGetValue(type, out var soundDefinition) ? soundDefinition : null;
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

        // generic sounds

        Collision,
        JunctionJoint,
        RollingAudioDetailed,
        RollingAudioSimple,
        SquealAudioDetailed,
        SquealAudioSimple,
        AirflowAudio,
        Coupling,
        Uncoupling,
        Wind,
        DerailHit,
        Switch,
        SwitchForced,
        CargoLoadUnload,
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
            SoundType.TractionMotors,
        ];

        public static readonly SoundType[] audioClipsSoundTypes =
        [
            SoundType.HornHit,
        ];
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class SoundDefinition
    {
        public string name;
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
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

        public bool IsGeneric => type >= SoundType.Collision;

        public static SoundDefinition Parse(string confilePath, string name, JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    return Parse(confilePath, name, (JObject) token);

                default:
                    throw new ConfigException($"Found {token.Type} where a sound definition object was expected");
            }
        }

        public static SoundDefinition Parse(string configFilePath, string name, JObject jObject)
        {
            try
            {
                var root = Path.GetDirectoryName(configFilePath);
                return new SoundDefinition(name, (SoundType)Enum.Parse(typeof(SoundType), jObject.ExtractChild<string>("type")))
                {
                    filename = jObject["filename"].Map(fn => fn.StrictValue<string>().Length == 0 ? "" : Path.Combine(root, fn.Value<string>())),
                    filenames = jObject["filenames"].Map(jArray => jArray.Select(fn => Path.Combine(root, fn.Value<string>())).ToArray()),
                    pitch = jObject["pitch"].MapS(n => n.Value<float>()),
                    minPitch = jObject["minPitch"].MapS(n => n.Value<float>()),
                    maxPitch = jObject["maxPitch"].MapS(n => n.Value<float>()),
                    minVolume = jObject["minVolume"].MapS(n => n.Value<float>()),
                    maxVolume = jObject["maxVolume"].MapS(n => n.Value<float>()),
                    fadeStart = jObject["fadeStart"].MapS(n => n.Value<float>()),
                    fadeDuration = jObject["fadeDuration"].MapS(n => n.Value<float>()),
                };
            }
            catch (Exception e)
            {
                throw new ConfigException($"Could not parse SoundDefinition: {jObject}", e);
            }
        }

        public override string ToString()
        {
            return $"{name} ({filename})";
        }
    }
}