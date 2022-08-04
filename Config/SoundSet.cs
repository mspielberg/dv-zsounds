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

        public static SoundDefinition Parse(string configFilePath, string name, JToken token)
        {
            try
            {
                var root = Path.GetDirectoryName(configFilePath);
                return new SoundDefinition(name, (SoundType)Enum.Parse(typeof(SoundType), token["type"].Value<string>()))
                {
                    filename = token["filename"].Map(fn => fn.Value<string>().Length == 0 ? "" : Path.Combine(root, fn.Value<string>())),
                    filenames = token["filenames"].Map(jArray => jArray.Select(fn => Path.Combine(root, fn.Value<string>())).ToArray()),
                    pitch = token["pitch"].MapS(n => n.Value<float>()),
                    minPitch = token["minPitch"].MapS(n => n.Value<float>()),
                    maxPitch = token["maxPitch"].MapS(n => n.Value<float>()),
                    minVolume = token["minVolume"].MapS(n => n.Value<float>()),
                    maxVolume = token["maxVolume"].MapS(n => n.Value<float>()),
                    fadeStart = token["fadeStart"].MapS(n => n.Value<float>()),
                    fadeDuration = token["fadeDuration"].MapS(n => n.Value<float>()),
                };
            }
            catch (Exception e)
            {
                throw new ConfigException($"Could not parse SoundDefinition: {token}", e);
            }
        }

        public override string ToString()
        {
            return $"{name} ({filename})";
        }
    }
}