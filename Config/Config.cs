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
        public readonly Dictionary<string, IRule> rules = new Dictionary<string, IRule>();
        public readonly Dictionary<string, SoundDefinition> sounds = new Dictionary<string, SoundDefinition>();
        public readonly List<Hook> hooks = new List<Hook>();

        public static Config? Active { get; private set; }

        public void Load(string path)
        {
            Main.DebugLog(() => $"Loading config {path}");
            var configFile = ConfigFile.Parse(path);
            foreach (var (key, rule) in configFile.rules)
                rules.Add(key, rule);
            foreach (var (key, sound) in configFile.sounds)
                sounds.Add(key, sound);
            foreach (var hook in configFile.hooks)
                hooks.Add(hook);
        }

        public void Validate()
        {
            Main.DebugLog(() => "Running hooks");
            foreach (var hook in hooks)
                hook.Apply(this);

            foreach (var (name, rule) in rules)
            {
                try
                {
                    rule.Validate(this);
                }
                catch (Exception e)
                {
                    throw new ConfigException($"Problem in rule \"{name}\"", e);
                }
            }

            foreach (var (name, sound) in sounds)
            {
                try
                {
                    sound.Validate();
                }
                catch (Exception e)
                {
                    throw new ConfigException($"Problem in sound \"{name}\"", e);
                }
            }

            Main.DebugLog(() => ToString());
        }

        public static void LoadAll()
        {
            var config = new Config();
            var mainConfigPath = Path.Combine(Main.mod!.Path, "zsounds-config.json");

            if (!File.Exists(mainConfigPath) || Default.IsDefaultConfigFile(mainConfigPath))
                File.WriteAllText(mainConfigPath, Default.CurrentDefaultConfigFile);
            config.Load(mainConfigPath);

            var modsDir = Path.GetDirectoryName(Path.GetDirectoryName(Main.mod!.Path));
            var extraConfigs =
                Directory.GetFiles(modsDir, "zsounds-config.json", SearchOption.AllDirectories)
                    .Where(p => p != mainConfigPath);

            foreach (var configPath in extraConfigs)
                config.Load(configPath);

            config.Validate();
            Active = config;
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
            return $"Rules:\n{string.Join("\n", ruleStrings).Indent(2)}\nSounds:\n{string.Join("\n", sounds.Values).Indent(2)}";
        }
    }

    public class ConfigFile
    {
        public readonly string path;
        public readonly int version;
        public readonly Dictionary<string, IRule> rules;
        public readonly Dictionary<string, SoundDefinition> sounds;
        public readonly List<Hook> hooks;

        public ConfigFile(string path, int version, Dictionary<string, IRule> rules, Dictionary<string, SoundDefinition> sounds, List<Hook> hooks)
        {
            this.path = path;
            this.version = version;
            this.rules = rules;
            this.sounds = sounds;
            this.hooks = hooks;
        }

        public static ConfigFile Parse(string path)
        {
            using var reader = new JsonTextReader(new StreamReader(path));
            JToken token = JToken.ReadFrom(reader);
            return new ConfigFile(
                Path.GetFullPath(path),
                token["version"].Value<int>(),
                (token["rules"] ?? Enumerable.Empty<JToken>()).OfType<JProperty>()
                    .ToDictionary(prop => prop.Name, prop => Rule.Parse(prop.Value)),
                (token["sounds"] ?? Enumerable.Empty<JToken>()).OfType<JProperty>()
                    .ToDictionary(prop => prop.Name, prop => SoundDefinition.Parse(path, prop.Name, prop.Value)),
                (token["hooks"] ?? Enumerable.Empty<JToken>()).Select((token, index) => Hook.Parse(path, index, token)).ToList()
            );
        }
    }

    public class ConfigException : Exception
    {
        public ConfigException() : base()
        {
        }

        public ConfigException(string message) : base(message)
        {
        }

        public ConfigException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}