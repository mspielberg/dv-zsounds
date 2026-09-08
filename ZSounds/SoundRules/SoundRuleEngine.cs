using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace DvMod.ZSounds.SoundRules
{
    public class SoundRuleEngine
    {
        private readonly List<CompiledRule> _compiledRules = new();

        public int RuleCount => _compiledRules.Count;

        public void LoadRules(string modPath)
        {
            LoadRulesFromDirectory(modPath);

            var userRulesDir = Path.Combine(modPath, "Sounds", "rules");
            if (Directory.Exists(userRulesDir))
            {
                foreach (var file in Directory.GetFiles(userRulesDir, "*.json"))
                {
                    LoadRulesFromFile(file, userPriorityOffset: 1000);
                }
            }

            var customRulesPath = Path.Combine(modPath, "Sounds", "custom_rules.json");
            if (File.Exists(customRulesPath))
            {
                LoadRulesFromFile(customRulesPath, userPriorityOffset: 1000);
            }

            _compiledRules.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            Main.mod?.Logger.Log($"SoundRuleEngine: Loaded {_compiledRules.Count} rules");
        }

        private void LoadRulesFromDirectory(string modPath)
        {
            var rulesPath = Path.Combine(modPath, "sound_rules.json");
            if (!File.Exists(rulesPath))
            {
                Main.mod?.Logger.Warning("SoundRuleEngine: sound_rules.json not found, using legacy heuristic only");
                return;
            }
            LoadRulesFromFile(rulesPath);
        }

        private void LoadRulesFromFile(string filePath, int userPriorityOffset = 0)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var rulesFile = JsonConvert.DeserializeObject<SoundRulesFile>(json);
                if (rulesFile?.rules == null) return;

                foreach (var rule in rulesFile.rules)
                {
                    if (string.IsNullOrEmpty(rule.pattern) || string.IsNullOrEmpty(rule.soundType))
                        continue;

                    if (!Enum.TryParse<SoundType>(rule.soundType, out var soundType))
                    {
                        Main.mod?.Logger.Warning($"SoundRuleEngine: Unknown soundType '{rule.soundType}' in rule");
                        continue;
                    }

                    try
                    {
                        var regex = new Regex(rule.pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                        var matchField = ParseMatchField(rule.matchField);
                        _compiledRules.Add(new CompiledRule(regex, soundType, matchField, rule.priority + userPriorityOffset, rule.carTypes));
                    }
                    catch (Exception ex)
                    {
                        Main.mod?.Logger.Warning($"SoundRuleEngine: Invalid regex pattern '{rule.pattern}': {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"SoundRuleEngine: Failed to load rules from {filePath}: {ex.Message}");
            }
        }

        public SoundType? Evaluate(string gameObjectName, string hierarchyPath, string clipName)
        {
            foreach (var rule in _compiledRules)
            {
                if (rule.Matches(gameObjectName, hierarchyPath, clipName))
                    return rule.SoundType;
            }
            return null;
        }

        private static MatchField ParseMatchField(string field)
        {
            return field?.ToLower() switch
            {
                "name" => MatchField.Name,
                "path" => MatchField.Path,
                "clip" => MatchField.Clip,
                _ => MatchField.Any,
            };
        }
    }

    [Flags]
    public enum MatchField
    {
        Name = 1,
        Path = 2,
        Clip = 4,
        Any = Name | Path | Clip,
    }

    public class CompiledRule
    {
        public Regex Pattern { get; }
        public SoundType SoundType { get; }
        public MatchField MatchField { get; }
        public int Priority { get; }

        public CompiledRule(Regex pattern, SoundType soundType, MatchField matchField, int priority, string? carTypes)
        {
            Pattern = pattern;
            SoundType = soundType;
            MatchField = matchField;
            Priority = priority;
        }

        public bool Matches(string gameObjectName, string hierarchyPath, string clipName)
        {
            if ((MatchField & MatchField.Name) != 0 && Pattern.IsMatch(gameObjectName))
                return true;
            if ((MatchField & MatchField.Path) != 0 && Pattern.IsMatch(hierarchyPath))
                return true;
            if ((MatchField & MatchField.Clip) != 0 && Pattern.IsMatch(clipName))
                return true;
            return false;
        }
    }
}
