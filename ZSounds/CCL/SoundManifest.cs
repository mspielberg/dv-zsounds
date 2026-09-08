using System.Collections.Generic;

namespace DvMod.ZSounds.CCL
{
    public class SoundManifest
    {
        public string version { get; set; } = "1";
        public string liveryId { get; set; } = "";
        public string? audioPrefabPattern { get; set; }
        public List<SoundManifestEntry> sounds { get; set; } = new();
    }

    public class SoundManifestEntry
    {
        public string soundType { get; set; } = "";
        public string? gameObjectName { get; set; }
        public string? hierarchyPath { get; set; }
        public string? clipNamePattern { get; set; }
        public string componentType { get; set; } = "LayeredAudio";
    }
}
