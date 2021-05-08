using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DvMod.ZSounds.Config
{
    public class Config
    {
        public readonly string path;
        public readonly int version;
        public readonly Dictionary<string, IRule> rules;
        public readonly Dictionary<string, SoundDefinition> sounds = new Dictionary<string, SoundDefinition>();

        public Config(string path, int version, Dictionary<string, IRule> rules, Dictionary<string, SoundDefinition> sounds)
        {
            this.path = path;
            this.version = version;
            this.rules = rules;
            this.sounds = sounds;
        }

        public static Config Parse(string path)
        {
            using var reader = new JsonTextReader(new StreamReader(path));
            JToken token = JToken.ReadFrom(reader);
            return new Config(
                Path.GetFullPath(path),
                token["version"].Value<int>(),
                token["rules"].OfType<JProperty>().ToDictionary(prop => prop.Name, prop => Rule.Parse(prop.Value)),
                token["sounds"].OfType<JProperty>().ToDictionary(prop => prop.Name, prop => SoundDefinition.Parse(prop.Name, prop.Value))
            );
        }

        public void Validate()
        {
            foreach (var (name, rule) in rules)
            {
                try
                {
                    rule.Validate(this);
                }
                catch (Exception e)
                {
                    throw new ArgumentException($"Problem in rule \"{name}\"", e);
                }
            }

            foreach (var (name, sound) in sounds)
            {
                try
                {
                    sound.Validate(this);
                }
                catch (Exception e)
                {
                    throw new ArgumentException($"Problem in sound \"{name}\"", e);
                }
            }
        }

        public SoundSet Apply(TrainCar car)
        {
            var soundSet = new SoundSet();
            rules["root"].Apply(this, car, soundSet);
            return soundSet;
        }

        public override string ToString()
        {
            var ruleStrings = rules.Select(kv => $"{kv.Key}:\n{kv.Value.ToString().Indent(2)}");
            return $"Config v{version}\nRules:\n{string.Join("\n", ruleStrings).Indent(2)}\nSounds:\n{string.Join("\n", sounds.Values).Indent(2)}";
        }
    }
}