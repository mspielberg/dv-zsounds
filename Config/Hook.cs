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
        public readonly int originIndex;

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
                    throw new ArgumentException($"Can not select subrule {position} of rule with {count} subrules: {rule}");
                return position >= 0 ? rules.ElementAt(position) : rules.ElementAt(count - position);
            }
            return rule switch
            {
                AllOfRule allOf => Get(allOf.rules, position),
                OneOfRule oneOf => Get(oneOf.rules, position),
                IfRule ifRule => Get(new IRule[] { ifRule.rule }, position),
                _ => throw new ArgumentException($"Can not select subrule of {rule}"),
            };
        }

        public Hook(string originPath, int originIndex, HookType type, string rulePath, IRule rule, float weight)
        {
            this.originPath = originPath;
            this.originIndex = originIndex;
            this.type = type;
            this.rulePath = rulePath;
            this.rule = rule;
            this.weight = weight;
        }

        public static Hook Parse(string originPath, int originIndex, JToken token)
        {
            return new Hook(
                originPath,
                originIndex,
                Util.ParseEnum<HookType>(token["type"]),
                token["path"].Value<string>(),
                Rule.Parse(token["rule"]),
                token["weight"]?.Value<float>() ?? 1);
        }

        public void Apply(Config config)
        {
            var pathComponents = rulePath.Split(new char[]{ '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
            var ruleName = pathComponents[0];
            var target = config.rules[ruleName];
            if (target == null)
                throw new ArgumentException($"Hook refers to nonexistent rule {ruleName}");
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
                throw new ArgumentException($"Cannot add to {rule}");
            }
        }
    }
}