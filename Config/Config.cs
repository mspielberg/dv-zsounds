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

        public readonly Dictionary<SoundType, List<SoundDefinition>> soundTypes =
            new Dictionary<SoundType, List<SoundDefinition>>();

        public static Config? Active { get; private set; }

        public void Load(string path)
        {
            Main.mod?.Logger.Log($"Loading config {path}");
            try
            {
                var configFile = ConfigFile.Parse(path);
                foreach (var (key, rule) in configFile.rules)
                    rules.Add(key, rule);

                foreach (var (key, sound) in configFile.sounds)
                {
                    sounds.Add(key, sound);
                    AddSoundToTypeMap(sound);
                }

                foreach (var hook in configFile.hooks)
                    hooks.Add(hook);
            }
            catch (Exception e)
            {
                throw new ConfigException($"Problem parsing config file {path}", e);
            }
        }

        private void AddSoundToTypeMap(SoundDefinition sound)
        {
            if (soundTypes.TryGetValue(sound.type, out var list))
            {
                list.Add(sound);
                return;
            }

            var newList = new List<SoundDefinition>
            {
                sound
            };
            soundTypes.Add(sound.type, newList);
        }

        public void Validate()
        {
            Main.DebugLog(() => $"Config before hooks:\n{ToString()}");
            Main.DebugLog(() => "Running hooks");
            foreach (var hook in hooks)
            {
                try
                {
                    hook.Apply(this);
                }
                catch (Exception e)
                {
                    throw new ConfigException($"Problem executing hook {hook.originPath}:{hook.token.Path}", e);
                }
            }

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

            try
            {
                foreach (var configPath in extraConfigs)
                    config.Load(configPath);
            }
            catch (ConfigException e)
            {
                Main.mod.Logger.LogException("Problem loading config files", e);
                throw e;
            }

            try
            {
                config.Validate();
            }
            catch (ConfigException e)
            {
                Main.mod.Logger.LogException("Problem validating config", e);
                throw e;
            }

            Active = config;
        }

        public SoundSet Apply(TrainCar car)
        {
            var soundSet = new SoundSet();
            rules["root"].Apply(this, car, soundSet);
            return soundSet;
        }

        public SoundSet GenericSoundSet()
        {
            var soundSet = new SoundSet();
            foreach (var sound in sounds.Values)
            {
                if (sound.IsGeneric)
                    sound.Apply(soundSet);
            }
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
            JObject rootObject = JToken.ReadFrom(reader).EnsureJObject();

            var fullPath = Path.GetFullPath(path);
            var version = rootObject.ExtractChild<int>("version");
            var rules = rootObject.ExtractChildOrEmpty<JObject>("rules").Properties()
                .ToDictionary(prop => prop.Name, prop => Rule.Parse(prop.Value));
            var sounds = rootObject.ExtractChildOrEmpty<JObject>("sounds").Properties()
                .ToDictionary(prop => prop.Name, prop => SoundDefinition.Parse(path, prop.Name, prop.Value));
            var hooks = rootObject.ExtractChildOrEmpty<JArray>("hooks")
                .Select(token => Hook.Parse(path, token.EnsureJObject())).ToList();

            return new ConfigFile(fullPath, version, rules, sounds, hooks);
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
