using DVCustomCarLoader;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityModManagerNet;

namespace DvMod.ZSounds.Config
{
    public enum RuleType
    {
        Unknown,
        AllOf,
        OneOf,
        If,
        Sound,
        Ref,
    }

    public interface IRule
    {
        public abstract void Apply(Config config, TrainCar car, SoundSet soundSet);
        public abstract void Validate(Config config);
    }

    public static class Rule
    {
        public static IRule Parse(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.String:
                    return new RefRule(token.Path, token.Value<string>());

                case JTokenType.Object:
                    RuleType type = (RuleType)Enum.Parse(
                        typeof(RuleType),
                        token["type"].Value<string>(),
                        ignoreCase: true);

                    return type switch
                    {
                        RuleType.AllOf => AllOfRule.Parse(token),
                        RuleType.OneOf => OneOfRule.Parse(token),
                        RuleType.If => IfRule.Parse(token),
                        RuleType.Sound => SoundRule.Parse(token),
                        RuleType.Ref => RefRule.Parse(token),

                        _ => throw new Exception($"Unknown rule type {type}"),
                    };

                default:
                    throw new ConfigException($"Found {token.Type} where a rule was expected");
            }
        }
    }

    public class AllOfRule : IRule
    {
        public readonly string path;
        public readonly List<IRule> rules;

        public AllOfRule(string path, List<IRule> rules)
        {
            this.path = path;
            this.rules = rules;
        }

        public static AllOfRule Parse(JToken token)
        {
            var rules = token["rules"]?.Select(Rule.Parse) ?? Enumerable.Empty<IRule>();
            var soundRules = token["sounds"]?.Select(t => new SoundRule(t.Path, t.Value<string>())) ?? Enumerable.Empty<SoundRule>();
            return new AllOfRule(token.Path, rules.Concat(soundRules).ToList());
        }

        public void Apply(Config config, TrainCar car, SoundSet soundSet)
        {
            foreach (var rule in rules)
                rule.Apply(config, car, soundSet);
        }

        public void Validate(Config config)
        {
            foreach (var rule in rules)
                rule.Validate(config);
        }

        public override string ToString()
        {
            return $"AllOf:\n{string.Join<IRule>("\n", rules).Indent(2)}";
        }
    }

    public class OneOfRule : IRule
    {
        public readonly string path;
        public readonly List<IRule> rules;
        public readonly List<float> weights;

        private float[]? thresholds;

        public OneOfRule(string path, List<IRule> rules, List<float>? weights)
        {
            if (rules == null || rules.Count == 0)
                throw new ConfigException("OneOf rule requires at least one sub-rule");
            if (weights == null)
                weights = Enumerable.Repeat(1f, rules.Count).ToList();
            if (weights.Count != rules.Count)
                throw new ConfigException($"Found {weights.Count} weights for {rules.Count} rules in OneOf rule");

            this.path = path;
            this.rules = rules;
            this.weights = weights;
        }

        public static OneOfRule Parse(JToken token)
        {
            return new OneOfRule(
                token.Path,
                token["rules"].Select(Rule.Parse).ToList(),
                token["weights"]?.Select(t => t.Value<float>())?.ToList());
        }

        private float[] Thresholds
        {
            get
            {
                if (thresholds == null)
                {
                    thresholds = new float[rules.Count];
                    var totalWeight = weights.Sum();

                    thresholds[0] = weights[0] / totalWeight;
                    for (int i = 1; i < rules.Count; i++)
                        thresholds[i] = thresholds[i - 1] + (weights[i] / totalWeight);
                }
                return thresholds;
            }
        }

        public void Apply(Config config, TrainCar car, SoundSet soundSet)
        {
            var r = UnityEngine.Random.value;
            var index = Array.FindIndex(Thresholds, t => r <= t);
            // Main.DebugLog(() => $"weights={string.Join(",",weights)},thresholds={string.Join(",",thresholds)},randomValue={r},index={index}");
            if (index >= 0)
                rules[index].Apply(config, car, soundSet);
        }

        public void Validate(Config config)
        {
            foreach (var rule in rules)
                rule.Validate(config);
        }

        public override string ToString()
        {
            var totalWeight = weights.Sum();
            return $"OneOf:\n{string.Join("\n", rules.Zip(weights, (r, w) => $"{w}/{totalWeight}: {r}")).Indent(2)}";
        }
    }

    public class IfRule : IRule
    {
        public enum IfRuleProperty
        {
            Unknown,
            CarType,
            SkinName,
        }

        public readonly string path;
        public readonly IfRuleProperty property;
        public readonly string value;
        public readonly IRule rule;

        public IfRule(string path, IfRuleProperty property, string value, IRule rule)
        {
            this.path = path;
            this.property = property;
            this.value = value;
            this.rule = rule;
        }

        private static readonly Dictionary<string, TrainCarType> trainCarTypes = new Dictionary<string, TrainCarType>();

        private static void AddCustomCars()
        {
            foreach (var customCar in CustomCarManager.CustomCarTypes)
                trainCarTypes[customCar.identifier] = customCar.CarType;
        }

        static IfRule()
        {
            foreach (TrainCarType trainCarType in Enum.GetValues(typeof(TrainCarType)))
                trainCarTypes[Enum.GetName(typeof(TrainCarType), trainCarType)] = trainCarType;

            if (UnityModManager.FindMod("DVCustomCarLoader")?.Loaded ?? false)
                AddCustomCars();

            Main.DebugLog(() => $"trainCarTypes:\n{string.Join("\n", trainCarTypes.Keys.Select(k => $"{k} -> {trainCarTypes[k]}"))}");
        }

        public static IfRule Parse(JToken token)
        {
            return new IfRule(
                token.Path,
                (IfRuleProperty)Enum.Parse(typeof(IfRuleProperty), token["property"].Value<string>(), ignoreCase: true),
                token["value"].Value<string>(),
                Rule.Parse(token["rule"])
            );
        }

        public void Apply(Config config, TrainCar car, SoundSet soundSet)
        {
            if (Applicable(car))
                rule.Apply(config, car, soundSet);
        }

        public void Validate(Config config)
        {
            switch (property)
            {
                case IfRuleProperty.CarType:
                    if (!trainCarTypes.ContainsKey(value))
                        Main.mod!.Logger.Log($"Unknown value '{value}' for CarType in rule {path}");
                    break;
                case IfRuleProperty.SkinName:
                    break;
            }
            rule.Validate(config);
        }

        private bool Applicable(TrainCar car)
        {
            return property switch
            {
                IfRuleProperty.CarType => trainCarTypes.TryGetValue(value, out var ruleCarType) && ruleCarType == car.carType,
                IfRuleProperty.SkinName => string.Equals(GetSkinName(car), value, StringComparison.OrdinalIgnoreCase),
                _ => false,
            };
        }

        private static Dictionary<string, string>? carSkins;
        private static Dictionary<string, string>? CarSkins
        {
            get
            {
                if (carSkins != null)
                    return carSkins;

                var mod = UnityModManager.FindMod("SkinManagerMod");
                if (mod == null)
                    return null;
                if (!mod.Active)
                    return null;

                var field = mod.Assembly
                    .GetType("SkinManagerMod.Main")
                    .GetField("trainCarState", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                carSkins = (Dictionary<string, string>)field.GetValue(null);
                return carSkins;
            }
        }

        private static string? GetSkinName(TrainCar car)
        {
            var skinManagerMod = UnityModManager.FindMod("SkinManagerMod");
            if (!(skinManagerMod?.Active ?? false))
                return null;

            if (skinManagerMod.Version >= new Version(2,5))
            {
                if (!skinManagerMod.Invoke(
                    "SkinManagerMod.SkinManager.GetCurrentCarSkin",
                    out var result,
                    new object[1]{ car },
                    new Type[1]{ typeof(TrainCar) }))
                {
                    Main.DebugLog(() => "Could not find GetCurrentCarSkin method");
                    return null;
                }
                Main.DebugLog(() => $"Result from GetCurrentCarSkin: {result}");
                var property = result.GetType().GetField("Name");
                var skinName = (string)property.GetValue(result);
                Main.DebugLog(() => $"skin name = {skinName}");
                return skinName;
            }

            if (skinManagerMod.Version < new Version(2, 5))
                return CarSkins?[car.CarGUID];

            return null;
        }

        public override string ToString()
        {
            return $"If {property} = {value}:\n{rule.ToString().Indent(2)}";
        }
    }

    public class RefRule : IRule
    {
        public readonly string path;
        public readonly string name;

        public RefRule(string path, string name)
        {
            this.path = path;
            this.name = name;
        }

        public void Apply(Config config, TrainCar car, SoundSet soundSet)
        {
            config.rules[name].Apply(config, car, soundSet);
        }

        public void Validate(Config config)
        {
            if (!config.rules.ContainsKey(name))
                throw new ConfigException($"Reference to unknown rule \"{name}\"");
        }

        public static RefRule Parse(JToken token)
        {
            return new RefRule(token.Path, token["name"].Value<string>());
        }

        public override string ToString()
        {
            return $"Ref \"{name}\"";
        }
    }

    public class SoundRule : IRule
    {
        public readonly string path;
        public readonly string name;

        public SoundRule(string path, string name)
        {
            this.path = path;
            this.name = name;
        }

        public void Apply(Config config, TrainCar car, SoundSet soundSet)
        {
            config.sounds[name].Apply(soundSet);
        }

        public void Validate(Config config)
        {
            if (!config.sounds.ContainsKey(name))
                throw new ConfigException($"Reference to unknown sound \"{name}\"");
        }

        public static SoundRule Parse(JToken token)
        {
            return new SoundRule(token.Path, token["name"].Value<string>());
        }

        public override string ToString()
        {
            return $"Sound \"{name}\"";
        }
    }
}