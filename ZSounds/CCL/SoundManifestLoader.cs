using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace DvMod.ZSounds.CCL
{
    public class SoundManifestLoader
    {
        private readonly Dictionary<string, SoundManifest> _manifests = new();

        public IReadOnlyDictionary<string, SoundManifest> Manifests => _manifests;

        public void DiscoverManifests(string modsDirectory)
        {
            if (!Directory.Exists(modsDirectory))
                return;

            foreach (var modDir in Directory.GetDirectories(modsDirectory))
            {
                var manifestPath = Path.Combine(modDir, "sound_manifest.json");
                if (File.Exists(manifestPath))
                {
                    LoadManifest(manifestPath);
                }
            }
        }

        public void LoadManifest(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var manifest = JsonConvert.DeserializeObject<SoundManifest>(json);
                if (manifest == null || string.IsNullOrEmpty(manifest.liveryId))
                {
                    Main.mod?.Logger.Warning($"SoundManifestLoader: Invalid manifest at {filePath} - missing liveryId");
                    return;
                }

                _manifests[manifest.liveryId] = manifest;
                Main.mod?.Logger.Log($"SoundManifestLoader: Loaded manifest for '{manifest.liveryId}' with {manifest.sounds.Count} sound entries");
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"SoundManifestLoader: Failed to load manifest from {filePath}: {ex.Message}");
            }
        }

        public SoundManifest? GetManifest(string liveryId)
        {
            return _manifests.TryGetValue(liveryId, out var manifest) ? manifest : null;
        }

        public void Clear()
        {
            _manifests.Clear();
        }
    }
}
