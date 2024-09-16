using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DvMod.ZSounds.Config
{
    public class Hook
    {
        public enum HookType
        {
            Unknown,
            AddRule
        }

        public readonly string originPath;
        public readonly JToken token;

        public readonly HookType type;
        public readonly string rulePath;
        public readonly IRule rule;
        public readonly float weight;

        private static IRule GetSubrule(IRule rule, int position)
        {
            IRule Get(IEnumerable<IRule> rules, int position)
            {
                var count = rules.Count();
                if (position >= count || position < -count)
                    throw new ConfigException($"Can not select subrule {position} of rule with {count} subrules: {rule}");
                return position >= 0 ? rules.ElementAt(position) : rules.ElementAt(count - position);
            }
            return rule switch
            {
                AllOfRule allOf => Get(allOf.rules, position),
                OneOfRule oneOf => Get(oneOf.rules, position),
                IfRule ifRule => Get([ ifRule.rule ], position),
                _ => throw new ConfigException($"Can not select subrule of {rule}"),
            };
        }

        public Hook(string originPath, JToken token, HookType type, string rulePath, IRule rule, float weight)
        {
            this.originPath = originPath;
            this.token = token;
            this.type = type;
            this.rulePath = rulePath;
            this.rule = rule;
            this.weight = weight;
        }

        public static Hook Parse(string originPath, JObject jObject)
        {
            IJsonLineInfo lineInfo = jObject;
            var hookType = jObject.ExtractChild<HookType>("type");
            var rule = jObject["rule"].Map(Rule.Parse);
            if (rule == null)
                throw new ConfigException($"Hook in {originPath} {jObject.Path} does not define a rule");

            return new Hook(
                originPath,
                jObject,
                hookType,
                jObject.ExtractChild<string>("path"),
                rule,
                jObject["weight"]?.StrictValue<float>() ?? 1);
        }

        public void Apply(Config config)
        {
            var pathComponents = rulePath.Split(new char[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
            var ruleName = pathComponents[0];
            var foundRule = config.rules.TryGetValue(ruleName, out var target);
            if (!foundRule)
                throw new ConfigException($"Hook refers to nonexistent rule {ruleName}");
            foreach (var position in pathComponents.Skip(1).Select(int.Parse))
                target = GetSubrule(target, position);

            if (target is AllOfRule allOf)
            {
                allOf.rules.Add(this.rule);
            }
            else if (target is OneOfRule oneOf)
            {
                oneOf.rules.Add(this.rule);
                oneOf.weights.Add(this.weight);
            }
            else
            {
                throw new ConfigException($"Cannot add to {rule}");
            }
        }
    }
}
