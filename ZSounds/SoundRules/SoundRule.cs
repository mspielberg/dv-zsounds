using System.Collections.Generic;

namespace DvMod.ZSounds.SoundRules
{
    public class SoundRule
    {
        public string pattern { get; set; } = "";
        public string matchField { get; set; } = "any";
        public string soundType { get; set; } = "";
        public int priority { get; set; } = 100;
        public string? carTypes { get; set; }
        public string? description { get; set; }
    }

    public class SoundRulesFile
    {
        public int version { get; set; } = 1;
        public List<SoundRule> rules { get; set; } = new();
    }
}
