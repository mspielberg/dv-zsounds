using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DV.ThingTypes;

namespace DvMod.ZSounds
{
    /// <summary>
    /// Creates folder structure dynamically based on discovered sound mappings.
    /// New structure: Sounds/[SoundType]/ with configs in Sounds/Configs/[SoundType]/
    /// Preserves existing user sound files and only creates folders for new discoveries.
    /// </summary>
    public static class DynamicFolderCreator
    {
        private static readonly string[] SupportedAudioExtensions = { ".ogg", ".wav" };

        /// <summary>
        /// Creates folder structure based on discovered sounds in the cache.
        /// Only creates folders that don't already contain user sound files.
        /// </summary>
        public static void CreateFolderStructure(string basePath)
        {
            Main.mod?.Logger.Log("DynamicFolderCreator: Creating category-based folder structure from discovered sounds...");

            var baseSoundsPath = Path.Combine(basePath, "Sounds");
            var configsPath = Path.Combine(baseSoundsPath, "Configs");

            // Ensure base Sounds directory exists
            if (!Directory.Exists(baseSoundsPath))
            {
                Directory.CreateDirectory(baseSoundsPath);
                Main.mod?.Logger.Log($"DynamicFolderCreator: Created base Sounds directory: {baseSoundsPath}");
            }

            // Ensure Configs directory exists
            if (!Directory.Exists(configsPath))
            {
                Directory.CreateDirectory(configsPath);
                Main.mod?.Logger.Log($"DynamicFolderCreator: Created Configs directory: {configsPath}");
            }

            int createdFolders = 0;
            int skippedExisting = 0;
            int createdReadmes = 0;

            // Get all unique sound types across all discovered train types
            var allSoundTypes = new HashSet<SoundType>();

            if (Main.discoveryService != null)
            {
                var allTrainTypes = Enum.GetValues(typeof(TrainCarType)).Cast<TrainCarType>();
                foreach (var trainType in allTrainTypes)
                {
                    var supportedTypes = Main.discoveryService.GetSupportedSoundTypes(trainType);
                    foreach (var soundType in supportedTypes)
                    {
                        allSoundTypes.Add(soundType);
                    }
                }
            }

            Main.mod?.Logger.Log($"DynamicFolderCreator: Found {allSoundTypes.Count} unique sound types across all locomotives");

            // Create folders for each sound type
            foreach (var soundType in allSoundTypes)
            {
                var soundTypePath = Path.Combine(baseSoundsPath, soundType.ToString());
                var configPath = Path.Combine(configsPath, soundType.ToString());

                // Check if sound folder already exists with audio files
                bool soundFolderExists = Directory.Exists(soundTypePath);
                if (soundFolderExists)
                {
                    var existingFiles = GetAudioFilesInDirectory(soundTypePath);
                    if (existingFiles.Any())
                    {
                        Main.DebugLog(() => $"DynamicFolderCreator: Skipping {soundType} - already has {existingFiles.Length} sound file(s)");
                        skippedExisting++;
                        continue;
                    }
                }

                // Create the sound type folder
                if (!soundFolderExists)
                {
                    Directory.CreateDirectory(soundTypePath);
                    createdFolders++;
                    Main.DebugLog(() => $"DynamicFolderCreator: Created folder: {soundType}");
                }

                // Create config folder
                if (!Directory.Exists(configPath))
                {
                    Directory.CreateDirectory(configPath);
                    Main.DebugLog(() => $"DynamicFolderCreator: Created config folder: Configs/{soundType}");
                }
            }

            // Create "Other" folder for generic sounds
            var otherSoundPath = Path.Combine(baseSoundsPath, "Other");
            var otherConfigPath = Path.Combine(configsPath, "Other");

            if (!Directory.Exists(otherSoundPath))
            {
                Directory.CreateDirectory(otherSoundPath);
                createdFolders++;
                Main.DebugLog(() => $"DynamicFolderCreator: Created folder: Other");
            }

            if (!Directory.Exists(otherConfigPath))
            {
                Directory.CreateDirectory(otherConfigPath);
                Main.DebugLog(() => $"DynamicFolderCreator: Created config folder: Configs/Other");
            }

            Main.mod?.Logger.Log($"DynamicFolderCreator: Complete. Created {createdFolders} new folders, skipped {skippedExisting} existing, created {createdReadmes} readme files");
        }

        /// <summary>
        /// Gets all audio files in a directory.
        /// </summary>
        private static string[] GetAudioFilesInDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                return Array.Empty<string>();
            }

            var audioFiles = new List<string>();
            foreach (var extension in SupportedAudioExtensions)
            {
                audioFiles.AddRange(Directory.GetFiles(directoryPath, $"*{extension}"));
            }
            return audioFiles.ToArray();
        }

        /// <summary>
        /// Validates that a folder structure matches the discovered sounds.
        /// Logs warnings for folders that don't correspond to discovered sounds.
        /// </summary>
        public static void ValidateFolderStructure(string basePath)
        {
            var baseSoundsPath = Path.Combine(basePath, "Sounds");

            if (!Directory.Exists(baseSoundsPath))
            {
                return;
            }

            Main.DebugLog(() => "DynamicFolderCreator: Validating category-based folder structure...");

            var soundTypeFolders = Directory.GetDirectories(baseSoundsPath);

            foreach (var soundTypeFolder in soundTypeFolders)
            {
                var soundTypeName = Path.GetFileName(soundTypeFolder);

                // Skip Configs folder
                if (soundTypeName.Equals("Configs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Check for legacy locomotive-specific folders
                if (Enum.TryParse<TrainCarType>(soundTypeName, true, out _))
                {
                    Main.mod?.Logger.Warning($"DynamicFolderCreator: Found legacy locomotive-specific folder: {soundTypeName}. Consider running migration.");
                    continue;
                }

                // Validate sound type folders
                if (!Enum.TryParse<SoundType>(soundTypeName, true, out var soundType))
                {
                    Main.mod?.Logger.Warning($"DynamicFolderCreator: Unknown sound type folder: {soundTypeName}");
                    continue;
                }

                // Check if this sound type is supported by any locomotive
                bool isSupported = false;
                if (Main.discoveryService != null)
                {
                    var allTrainTypes = Enum.GetValues(typeof(TrainCarType)).Cast<TrainCarType>();
                    foreach (var trainType in allTrainTypes)
                    {
                        if (Main.discoveryService.IsSoundSupported(trainType, soundType))
                        {
                            isSupported = true;
                            break;
                        }
                    }
                }

                if (!isSupported)
                {
                    Main.mod?.Logger.Warning($"DynamicFolderCreator: Folder exists for sound type not supported by any locomotive: {soundType}");
                }
            }

            Main.DebugLog(() => "DynamicFolderCreator: Validation complete");
        }
    }
}

