using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace DvMod.ZSounds.Config
{
    public class SoundSet
    {
        public readonly Dictionary<SoundType, SoundDefinition> sounds = new Dictionary<SoundType, SoundDefinition>();

        public JObject ToJson()
        {
            var obj = new JObject();
            var serializer = new JsonSerializer();
            foreach (var (type, sound) in sounds)
            {
                var tokenWriter = new JTokenWriter();
                serializer.Serialize(tokenWriter, sound);
                obj.Add(type.ToString(), tokenWriter.Token);
            }
            return obj;
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
        EngineStartup,
        EngineShutdown,
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

        public void Validate(Config config)
        {
            var dir = Path.GetDirectoryName(config.path);
            void ValidateFile(string f) => FileAudio.Load(Path.Combine(dir, f));
            if (filename != null)
                ValidateFile(filename);
            foreach (var f in filenames ?? new string[0])
                ValidateFile(f);
        }

        public static SoundDefinition Parse(string name, JToken token)
        {
            return new SoundDefinition(name, (SoundType)Enum.Parse(typeof(SoundType), token["type"].Value<string>()))
            {
                filename = token["filename"]?.Value<string>(),
            };
        }

        public override string ToString()
        {
            return $"{name}: {filename}";
        }
    }
}