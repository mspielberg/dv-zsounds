using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DV.ThingTypes;

namespace DvMod.ZSounds.SoundHandler
{
    /// <summary>
    /// Service for migrating sound files from old locomotive-specific folder structure
    /// to new category-based flat structure.
    ///
    /// Old: Sounds/[TrainCarType]/[SoundType]/*.ogg + config.json
    /// New: Sounds/[SoundType]/*.ogg + Sounds/Configs/[SoundType]/config.json
    /// </summary>
    public class SoundMigration
    {
        private readonly string _baseSoundsPath;
        private readonly string _configsPath;
        private static readonly string[] AudioExtensions = { ".ogg", ".wav" };
        private const string ConfigFileName = "config.json";

        public SoundMigration(string basePath)
        {
            _baseSoundsPath = Path.Combine(basePath, "Sounds");
            _configsPath = Path.Combine(_baseSoundsPath, "Configs");
        }

        /// <summary>
        /// Checks if the old folder structure exists and needs migration.
        /// </summary>
        public bool NeedsMigration()
        {
            if (!Directory.Exists(_baseSoundsPath))
                return false;

            // Check if any TrainCarType folders exist
            var directories = Directory.GetDirectories(_baseSoundsPath);
            foreach (var dir in directories)
            {
                var folderName = Path.GetFileName(dir);

                // Skip the Configs folder (new structure)
                if (folderName.Equals("Configs", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Check if this is a valid TrainCarType folder
                if (Enum.TryParse<TrainCarType>(folderName, true, out _))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Performs the migration from old to new structure.
        /// Creates a timestamped backup before migrating.
        /// </summary>
        public void Migrate()
        {
            Main.mod?.Logger.Log("=== Starting Sound Folder Migration ===");

            if (!Directory.Exists(_baseSoundsPath))
            {
                Main.mod?.Logger.Warning("Sounds folder does not exist, nothing to migrate");
                return;
            }

            try
            {
                // Create backup
                CreateBackup();

                // Perform migration
                var stats = MigrateStructure();

                Main.mod?.Logger.Log($"=== Migration Complete ===");
                Main.mod?.Logger.Log($"Migrated {stats.soundFilesMoved} sound files");
                Main.mod?.Logger.Log($"Migrated {stats.configFilesMoved} config files");
                Main.mod?.Logger.Log($"Resolved {stats.nameConflicts} name conflicts");
                Main.mod?.Logger.Log($"Removed {stats.oldFoldersRemoved} old folders");
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Migration failed: {ex.Message}");
                Main.mod?.Logger.Error($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private void CreateBackup()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupPath = Path.Combine(Path.GetDirectoryName(_baseSoundsPath)!, $"Sounds_backup_{timestamp}");

            Main.mod?.Logger.Log($"Creating backup at: {backupPath}");

            CopyDirectory(_baseSoundsPath, backupPath);

            Main.mod?.Logger.Log("Backup created successfully");
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(destDir, fileName);
                File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                var destSubDir = Path.Combine(destDir, dirName);
                CopyDirectory(dir, destSubDir);
            }
        }

        private MigrationStats MigrateStructure()
        {
            var stats = new MigrationStats();

            // Ensure Configs folder exists
            Directory.CreateDirectory(_configsPath);

            // Track name conflicts per SoundType
            var soundFileCounters = new Dictionary<string, Dictionary<string, int>>();
            var configFileCounters = new Dictionary<string, int>();

            // Get all TrainCarType folders
            var trainTypeFolders = Directory.GetDirectories(_baseSoundsPath)
                .Where(dir =>
                {
                    var folderName = Path.GetFileName(dir);
                    return !folderName.Equals("Configs", StringComparison.OrdinalIgnoreCase) &&
                           Enum.TryParse<TrainCarType>(folderName, true, out _);
                })
                .ToArray();

            Main.mod?.Logger.Log($"Found {trainTypeFolders.Length} locomotive type folders to migrate");

            foreach (var trainTypeFolder in trainTypeFolders)
            {
                var trainTypeName = Path.GetFileName(trainTypeFolder);
                Main.mod?.Logger.Log($"Migrating {trainTypeName}...");

                // Get all SoundType folders within this TrainCarType folder
                var soundTypeFolders = Directory.GetDirectories(trainTypeFolder);

                foreach (var soundTypeFolder in soundTypeFolders)
                {
                    var soundTypeName = Path.GetFileName(soundTypeFolder);

                    if (!Enum.TryParse<SoundType>(soundTypeName, true, out _))
                    {
                        Main.mod?.Logger.Warning($"Skipping unknown sound type folder: {soundTypeName}");
                        continue;
                    }

                    // Ensure target sound type folder exists
                    var targetSoundFolder = Path.Combine(_baseSoundsPath, soundTypeName);
                    Directory.CreateDirectory(targetSoundFolder);

                    // Initialize counters for this sound type if needed
                    if (!soundFileCounters.ContainsKey(soundTypeName))
                    {
                        soundFileCounters[soundTypeName] = new Dictionary<string, int>();
                    }

                    // Migrate sound files
                    var soundFiles = Directory.GetFiles(soundTypeFolder)
                        .Where(f => AudioExtensions.Contains(Path.GetExtension(f).ToLower()))
                        .ToArray();

                    foreach (var soundFile in soundFiles)
                    {
                        var fileName = Path.GetFileName(soundFile);
                        var targetPath = Path.Combine(targetSoundFolder, fileName);

                        // Handle name conflicts
                        if (File.Exists(targetPath))
                        {
                            // Get or initialize counter for this filename
                            if (!soundFileCounters[soundTypeName].ContainsKey(fileName))
                            {
                                soundFileCounters[soundTypeName][fileName] = 1;
                            }

                            var counter = ++soundFileCounters[soundTypeName][fileName];
                            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                            var extension = Path.GetExtension(fileName);
                            var newFileName = $"{nameWithoutExt}_{counter}{extension}";
                            targetPath = Path.Combine(targetSoundFolder, newFileName);

                            Main.mod?.Logger.Log($"  Conflict resolved: {fileName} -> {newFileName}");
                            stats.nameConflicts++;
                        }

                        File.Move(soundFile, targetPath);
                        stats.soundFilesMoved++;
                        Main.DebugLog(() => $"  Moved: {fileName} to {soundTypeName}/");
                    }

                    // Migrate config file if exists
                    var configFile = Path.Combine(soundTypeFolder, ConfigFileName);
                    if (File.Exists(configFile))
                    {
                        var targetConfigFolder = Path.Combine(_configsPath, soundTypeName);
                        Directory.CreateDirectory(targetConfigFolder);

                        var targetConfigPath = Path.Combine(targetConfigFolder, ConfigFileName);

                        // Handle config conflicts
                        if (File.Exists(targetConfigPath))
                        {
                            if (!configFileCounters.ContainsKey(soundTypeName))
                            {
                                configFileCounters[soundTypeName] = 1;
                            }

                            var counter = ++configFileCounters[soundTypeName];
                            var newConfigName = $"config_{counter}.json";
                            targetConfigPath = Path.Combine(targetConfigFolder, newConfigName);

                            Main.mod?.Logger.Log($"  Config conflict: Created {newConfigName} for {trainTypeName}/{soundTypeName}");
                            stats.nameConflicts++;
                        }

                        File.Move(configFile, targetConfigPath);
                        stats.configFilesMoved++;
                        Main.DebugLog(() => $"  Moved config: {soundTypeName}/config.json");
                    }

                    // Remove empty sound type folder
                    if (Directory.GetFiles(soundTypeFolder).Length == 0 &&
                        Directory.GetDirectories(soundTypeFolder).Length == 0)
                    {
                        Directory.Delete(soundTypeFolder);
                    }
                }

                // Remove empty train type folder
                if (Directory.GetFiles(trainTypeFolder).Length == 0 &&
                    Directory.GetDirectories(trainTypeFolder).Length == 0)
                {
                    Directory.Delete(trainTypeFolder);
                    stats.oldFoldersRemoved++;
                    Main.DebugLog(() => $"Removed empty folder: {trainTypeName}");
                }
            }

            return stats;
        }

        private class MigrationStats
        {
            public int soundFilesMoved;
            public int configFilesMoved;
            public int nameConflicts;
            public int oldFoldersRemoved;
        }
    }
}

