using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DV.ThingTypes;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace DvMod.ZSounds.SoundHandler
{
    /// <summary>
    /// Service for loading sound files from disk and managing sound configurations.
    /// Consolidates functionality from FolderSoundLoader and FileAudio.
    /// </summary>
    public class SoundLoader
    {
        private const string CONFIG_FILENAME = "config.json";

        private readonly string _baseSoundsPath;

        // AudioClip cache: file path -> loaded AudioClip
        private readonly Dictionary<string, AudioClip> _audioClipCache = new();

        // Sound definition cache: sound name -> SoundDefinition
        private readonly Dictionary<string, SoundDefinition> _loadedSounds = new();

        // Organized sounds: TrainCarType -> SoundType -> List of SoundDefinitions
        private readonly Dictionary<TrainCarType, Dictionary<SoundType, List<SoundDefinition>>> _trainSounds = new();

        // Organized sounds by string identifier (livery ID): string -> SoundType -> List of SoundDefinitions
        // This supports custom locomotives that don't have standard TrainCarType enum values
        private readonly Dictionary<string, Dictionary<SoundType, List<SoundDefinition>>> _trainSoundsByIdentifier = new();

        // Supported audio file extensions
        private static readonly Dictionary<string, AudioType> AudioTypes = new()
        {
            {".ogg", AudioType.OGGVORBIS},
            {".wav", AudioType.WAV},
        };

        // Silent clip for fallback
        public static readonly AudioClip SilentClip = AudioClip.Create("silent", 1, 1, 44100, false);

        public SoundLoader(string basePath)
        {
            _baseSoundsPath = Path.Combine(basePath, "Sounds");
        }

        #region Public API - Sound Discovery & Loading

        /// <summary>
        /// Reloads all sound files from the folder structure.
        /// Call this after adding new sound files to make them available.
        /// </summary>
        public void ReloadAllSounds()
        {
            Main.mod?.Logger.Log("Reloading all sounds from disk...");
            LoadAllSounds();
            Main.mod?.Logger.Log($"Reload complete: {_loadedSounds.Count} sounds available");
        }

        /// <summary>
        /// Loads all sound files from the folder structure.
        /// New structure: Sounds/[SoundType]/*.ogg|*.wav with configs in Sounds/Configs/[SoundType]/config.json
        /// Legacy structure: Sounds/[TrainCarType]/[SoundType]/*.ogg|*.wav (for backward compatibility)
        /// </summary>
        public void LoadAllSounds()
        {
            Main.mod?.Logger.Log($"Loading sounds from folder structure: {_baseSoundsPath}");

            if (!Directory.Exists(_baseSoundsPath))
            {
                Main.mod?.Logger.Warning($"Sounds directory not found: {_baseSoundsPath}");
                return;
            }

            _loadedSounds.Clear();
            _trainSounds.Clear();
            _trainSoundsByIdentifier.Clear(); // Clear identifier cache to prevent duplicates on reload

            // Determine if we're using new or legacy structure
            bool hasNewStructure = HasNewStructure();
            bool hasLegacyStructure = HasLegacyStructure();

            if (hasNewStructure)
            {
                Main.mod?.Logger.Log("Using new category-based sound structure");
                LoadNewStructure();
            }
            else if (hasLegacyStructure)
            {
                Main.mod?.Logger.Log("Using legacy locomotive-specific sound structure");
                LoadLegacyStructure();
            }
            else
            {
                Main.mod?.Logger.Warning("No sound folders found in either new or legacy structure");
            }

            Main.mod?.Logger.Log($"Loaded {_loadedSounds.Count} sound definitions from folder structure");
        }

        /// <summary>
        /// Checks if the new category-based structure exists.
        /// </summary>
        private bool HasNewStructure()
        {
            var soundTypeFolders = Directory.GetDirectories(_baseSoundsPath);
            foreach (var folder in soundTypeFolders)
            {
                var folderName = Path.GetFileName(folder);
                // Skip Configs folder
                if (folderName.Equals("Configs", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Check if this is "Other" folder or a valid SoundType folder
                if (folderName.Equals("Other", StringComparison.OrdinalIgnoreCase) ||
                    Enum.TryParse<SoundType>(folderName, true, out _))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the legacy locomotive-specific structure exists.
        /// </summary>
        private bool HasLegacyStructure()
        {
            var folders = Directory.GetDirectories(_baseSoundsPath);
            foreach (var folder in folders)
            {
                var folderName = Path.GetFileName(folder);
                // Check if this is a valid TrainCarType folder
                if (Enum.TryParse<TrainCarType>(folderName, true, out _))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Loads sounds from new category-based structure: Sounds/[SoundType]/*.ogg
        /// </summary>
        private void LoadNewStructure()
        {
            var soundTypeFolders = Directory.GetDirectories(_baseSoundsPath);

            foreach (var soundTypeFolder in soundTypeFolders)
            {
                var soundTypeName = Path.GetFileName(soundTypeFolder);

                // Skip the Configs folder
                if (soundTypeName.Equals("Configs", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Handle "Other" folder as Unknown type for generic sounds
                if (soundTypeName.Equals("Other", StringComparison.OrdinalIgnoreCase))
                {
                    LoadSoundsForCategory(SoundType.Unknown, soundTypeFolder);
                }
                else if (Enum.TryParse<SoundType>(soundTypeName, true, out var soundType))
                {
                    LoadSoundsForCategory(soundType, soundTypeFolder);
                }
                else
                {
                    Main.mod?.Logger.Warning($"Unknown sound type folder: {soundTypeName}");
                }
            }
        }

        /// <summary>
        /// Loads sounds from legacy structure: Sounds/[TrainCarType]/[SoundType]/*.ogg
        /// </summary>
        private void LoadLegacyStructure()
        {
            var trainTypeFolders = Directory.GetDirectories(_baseSoundsPath);

            foreach (var trainTypeFolder in trainTypeFolders)
            {
                var trainTypeName = Path.GetFileName(trainTypeFolder);
                if (Enum.TryParse<TrainCarType>(trainTypeName, true, out var trainType))
                {
                    LoadTrainSounds(trainType, trainTypeFolder);
                }
                else
                {
                    Main.mod?.Logger.Warning($"Unknown train type folder: {trainTypeName}");
                }
            }
        }

        /// <summary>
        /// Creates an empty sound set for a train car.
        /// No automatic sound replacement occurs - sounds are only applied when manually selected.
        /// </summary>
        public SoundSet CreateSoundSetForTrain(TrainCar car)
        {
            var soundSet = new SoundSet();
            Main.DebugLog(() => $"Created empty sound set for train type: {car.carType} - no automatic sound application");
            return soundSet;
        }

        /// <summary>
        /// Applies a specific sound to a train car using the modern sound replacement system.
        /// </summary>
        public void ApplySoundToTrain(TrainCar car, SoundType soundType, string? soundName = null)
        {
            if (Main.registryService == null || Main.applicatorService == null)
            {
                Main.mod?.Logger.Error("Cannot apply sound - services not initialized");
                return;
            }

            var soundSet = Main.registryService.GetSoundSet(car);
            SoundDefinition? sound = null;

            if (soundName != null)
            {
                sound = GetSound(soundName);
            }
            else if (_trainSounds.TryGetValue(car.carType, out var availableTrainSounds))
            {
                if (availableTrainSounds.TryGetValue(soundType, out var soundList) && soundList.Count > 0)
                {
                    var random = new System.Random();
                    sound = soundList[random.Next(soundList.Count)];
                }
            }

            if (sound != null)
            {
                sound.Apply(soundSet);

                // Apply sound changes using the applicator service
                Main.applicatorService.ApplySoundSet(car, soundSet);

                // Save the updated sound state persistently
                Main.registryService.SaveSoundState(car, soundSet);
            }
            else
            {
                Main.mod?.Logger.Warning($"Could not find sound for {car.carType} {soundType} (requested: {soundName})");
            }
        }

        #endregion

        #region Public API - Queries

        /// <summary>
        /// Gets all available sounds for a specific train type.
        /// </summary>
        public Dictionary<SoundType, List<SoundDefinition>> GetAvailableSoundsForTrain(TrainCarType trainType)
        {
            return _trainSounds.TryGetValue(trainType, out var trainSounds)
                ? trainSounds
                : new Dictionary<SoundType, List<SoundDefinition>>();
        }

        /// <summary>
        /// Gets all available sounds for a specific train identifier (livery ID or TrainCarType string).
        /// </summary>
        public Dictionary<SoundType, List<SoundDefinition>> GetAvailableSoundsForTrain(string trainIdentifier)
        {
            return _trainSoundsByIdentifier.TryGetValue(trainIdentifier, out var trainSounds)
                ? trainSounds
                : new Dictionary<SoundType, List<SoundDefinition>>();
        }

        /// <summary>
        /// Gets all available sounds for a specific train car instance.
        /// </summary>
        public Dictionary<SoundType, List<SoundDefinition>> GetAvailableSoundsForTrain(TrainCar car)
        {
            var identifier = SoundDiscovery.GetTrainIdentifier(car);

            // Try string identifier first (includes custom locomotives)
            var sounds = GetAvailableSoundsForTrain(identifier);
            if (sounds.Count > 0)
                return sounds;

            // Fallback to TrainCarType for standard locomotives
            return GetAvailableSoundsForTrain(car.carType);
        }

        /// <summary>
        /// Gets all loaded sounds of a specific type.
        /// </summary>
        public List<SoundDefinition> GetSoundsOfType(SoundType soundType)
        {
            return _loadedSounds.Values.Where(s => s.type == soundType).ToList();
        }

        /// <summary>
        /// Gets a specific sound definition by name.
        /// </summary>
        public SoundDefinition? GetSound(string soundName)
        {
            return _loadedSounds.TryGetValue(soundName, out var sound) ? sound : null;
        }

        /// <summary>
        /// Gets all loaded sound definitions.
        /// </summary>
        public IReadOnlyDictionary<string, SoundDefinition> GetAllSounds()
        {
            return _loadedSounds;
        }

        #endregion

        #region Public API - Audio Clip Loading

        /// <summary>
        /// Loads an audio clip from a file path. Returns cached clip if already loaded.
        /// </summary>
        public AudioClip LoadAudioClip(string path)
        {
            if (string.IsNullOrEmpty(path))
                return SilentClip;

            if (_audioClipCache.TryGetValue(path, out var clip))
                return clip;

            Main.DebugLog(() => $"Loading audio file: {path}");

            var extension = Path.GetExtension(path);
            if (!AudioTypes.ContainsKey(extension))
                throw new ConfigException($"Unsupported file extension for sound file: \"{path}\"");

            if (!File.Exists(path))
                throw new ConfigException($"Sound file not found: \"{path}\"");

            var audioType = AudioTypes[extension];
            var webRequest = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, audioType);
            var async = webRequest.SendWebRequest();

            while (!async.isDone)
            {
                // Wait for request to complete
            }

            if (webRequest.isNetworkError || webRequest.isHttpError)
            {
                var error = $"Failed to load audio file \"{path}\": {webRequest.error}";
                webRequest.Dispose();
                throw new ConfigException(error);
            }

            clip = DownloadHandlerAudioClip.GetContent(webRequest);
            webRequest.Dispose();

            if (clip == null)
                throw new ConfigException($"Failed to extract audio clip from file: \"{path}\"");

            clip.name = Path.GetFileNameWithoutExtension(path);

            _audioClipCache[path] = clip;
            return clip;
        }

        /// <summary>
        /// Clears the audio clip cache.
        /// </summary>
        public void ClearAudioCache()
        {
            _audioClipCache.Clear();
            Main.mod?.Logger.Log("SoundLoader: Cleared audio clip cache");
        }

        #endregion

        #region Private - Sound Loading

        /// <summary>
        /// Loads sounds for a specific SoundType category from new structure.
        /// Populates sounds for all locomotives that support this sound type.
        /// </summary>
        private void LoadSoundsForCategory(SoundType soundType, string folder)
        {
            Main.DebugLog(() => $"SoundLoader: Loading sounds for category {soundType} from {folder}");

            // Load configuration for this sound type category
            var config = LoadConfigurationFromNewStructure(soundType);
            if (config != null)
            {
                Main.DebugLog(() => $"SoundLoader: Loaded configuration for {soundType}");
            }

            // Load sound files
            var soundFiles = Directory.GetFiles(folder, "*.ogg")
                .Concat(Directory.GetFiles(folder, "*.wav"))
                .ToArray();

            if (soundFiles.Length == 0)
            {
                Main.mod?.Logger.Warning($"No sound files found in {folder}");
                return;
            }

            Main.DebugLog(() => $"SoundLoader: Found {soundFiles.Length} sound files in {folder}");

            // Get all train types that support this sound type
            var supportedTrainTypes = GetTrainTypesSupportingSoundType(soundType);

            foreach (var soundFile in soundFiles)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(soundFile);
                    var soundName = $"{soundType}_{fileName}";

                    // Generate config file path for this specific sound
                    var configPath = GetConfigPathForSound(soundFile, soundType);

                    // DON'T auto-generate configs - only load if exists
                    // User can create configs via the editor UI when needed

                    // Load per-sound configuration (only if exists)
                    var soundConfig = LoadSoundConfiguration(configPath);

                    // Create sound definition
                    var soundDef = new SoundDefinition(soundName, soundType)
                    {
                        filename = soundFile,
                        configPath = configPath
                    };

                    // Apply per-sound configuration if available and enabled
                    if (soundConfig != null)
                    {
                        var isEnabled = soundConfig.enabled == null || soundConfig.enabled.Value;
                        Main.mod?.Logger.Log($"[NEW STRUCT] {soundName}: enabled={soundConfig.enabled?.ToString() ?? "null"}, will apply={isEnabled}");

                        if (isEnabled)
                        {
                            ApplyConfigurationSettings(soundDef, soundConfig);
                            Main.mod?.Logger.Log($"[NEW STRUCT] Applied config to {soundName}");
                        }
                        else
                        {
                            Main.mod?.Logger.Log($"[NEW STRUCT] Skipped disabled config for {soundName}");
                        }
                    }

                    // Validate the sound file can be loaded
                    try
                    {
                        ValidateSoundDefinition(soundDef);
                    }
                    catch (Exception validationEx)
                    {
                        Main.mod?.Logger.Warning($"Sound file validation failed for {soundFile}: {validationEx.Message}");
                        continue;
                    }

                    _loadedSounds[soundName] = soundDef;

                    // Add to all compatible train-specific sounds (by TrainCarType)
                    foreach (var trainType in supportedTrainTypes)
                    {
                        if (!_trainSounds.ContainsKey(trainType))
                        {
                            _trainSounds[trainType] = new Dictionary<SoundType, List<SoundDefinition>>();
                        }

                        if (!_trainSounds[trainType].ContainsKey(soundType))
                        {
                            _trainSounds[trainType][soundType] = new List<SoundDefinition>();
                        }

                        _trainSounds[trainType][soundType].Add(soundDef);
                        Main.DebugLog(() => $"Added {soundName} to {trainType}/{soundType} (now {_trainSounds[trainType][soundType].Count} sounds)");
                    }

                    // Also add to train sounds by identifier (includes custom locomotives)
                    var trainIdentifiers = GetTrainIdentifiersSupportingSoundType(soundType);
                    foreach (var identifier in trainIdentifiers)
                    {
                        if (!_trainSoundsByIdentifier.ContainsKey(identifier))
                        {
                            _trainSoundsByIdentifier[identifier] = new Dictionary<SoundType, List<SoundDefinition>>();
                        }

                        if (!_trainSoundsByIdentifier[identifier].ContainsKey(soundType))
                        {
                            _trainSoundsByIdentifier[identifier][soundType] = new List<SoundDefinition>();
                        }

                        _trainSoundsByIdentifier[identifier][soundType].Add(soundDef);
                        Main.DebugLog(() => $"Added {soundName} to {identifier}/{soundType} via identifier");
                    }

                    Main.DebugLog(() => $"Loaded category sound: {soundName} for {supportedTrainTypes.Count} train types");
                }
                catch (Exception ex)
                {
                    Main.mod?.Logger.Error($"Failed to load sound file {soundFile}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Gets all train types that support a specific sound type by querying SoundDiscovery.
        /// </summary>
        private List<TrainCarType> GetTrainTypesSupportingSoundType(SoundType soundType)
        {
            var supportedTypes = new List<TrainCarType>();

            if (Main.discoveryService == null)
            {
                Main.mod?.Logger.Warning($"SoundDiscovery not available, cannot determine train compatibility for {soundType}");
                return supportedTypes;
            }

            var allTrainTypes = Enum.GetValues(typeof(TrainCarType)).Cast<TrainCarType>();

            foreach (var trainType in allTrainTypes)
            {
                if (Main.discoveryService.IsSoundSupported(trainType, soundType))
                {
                    supportedTypes.Add(trainType);
                }
            }

            return supportedTypes;
        }

        /// <summary>
        /// Gets all train identifiers (including custom locomotives) that support a specific sound type.
        /// </summary>
        private List<string> GetTrainIdentifiersSupportingSoundType(SoundType soundType)
        {
            var supportedIdentifiers = new List<string>();

            if (Main.discoveryService == null)
            {
                Main.mod?.Logger.Warning($"SoundDiscovery not available, cannot determine train compatibility for {soundType}");
                return supportedIdentifiers;
            }

            // Get all discovered train identifiers (includes custom locomotives)
            var allIdentifiers = Main.discoveryService.GetAllTrainIdentifiers();

            foreach (var identifier in allIdentifiers)
            {
                // Check if this identifier supports the sound type
                if (Main.discoveryService.IsSoundSupported(identifier, soundType))
                {
                    supportedIdentifiers.Add(identifier);
                }
            }

            return supportedIdentifiers;
        }

        /// <summary>
        /// Loads configuration from new structure: Sounds/Configs/[SoundType]/config.json
        /// </summary>
        private SoundConfiguration? LoadConfigurationFromNewStructure(SoundType soundType)
        {
            var configsBasePath = Path.Combine(_baseSoundsPath, "Configs");
            var soundTypeConfigPath = Path.Combine(configsBasePath, soundType.ToString());
            var configPath = Path.Combine(soundTypeConfigPath, CONFIG_FILENAME);

            if (!File.Exists(configPath))
            {
                Main.DebugLog(() => $"No config.json found for {soundType} in new structure");
                return null;
            }

            try
            {
                var jsonContent = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<SoundConfiguration>(jsonContent);

                Main.DebugLog(() => $"Loaded sound configuration from {configPath}");
                return config;
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to load sound configuration from {configPath}: {ex.Message}");
                return null;
            }
        }

        private void LoadTrainSounds(TrainCarType trainType, string trainTypeFolder)
        {
            if (!_trainSounds.ContainsKey(trainType))
            {
                _trainSounds[trainType] = new Dictionary<SoundType, List<SoundDefinition>>();
            }

            var soundTypeFolders = Directory.GetDirectories(trainTypeFolder);
            foreach (var soundTypeFolder in soundTypeFolders)
            {
                var soundTypeName = Path.GetFileName(soundTypeFolder);
                if (Enum.TryParse<SoundType>(soundTypeName, true, out var soundType))
                {
                    LoadSoundsFromFolder(trainType, soundType, soundTypeFolder);
                }
                else
                {
                    Main.mod?.Logger.Warning($"Unknown sound type folder: {soundTypeName} in {trainType}");
                }
            }
        }

        private void LoadSoundsFromFolder(TrainCarType trainType, SoundType soundType, string folder)
        {
            Main.DebugLog(() => $"SoundLoader: Loading sounds for {trainType}/{soundType} from {folder}");

            // Load local configuration for this sound type folder
            var config = LoadConfiguration(folder);
            if (config != null)
            {
                Main.DebugLog(() => $"SoundLoader: Loaded configuration for {trainType}/{soundType}");
            }
            else
            {
                Main.DebugLog(() => $"SoundLoader: No configuration found for {trainType}/{soundType}, using defaults");
            }

            // Load sound files
            var soundFiles = Directory.GetFiles(folder, "*.ogg")
                .Concat(Directory.GetFiles(folder, "*.wav"))
                .ToArray();

            if (soundFiles.Length == 0)
            {
                Main.mod?.Logger.Warning($"No sound files found in {folder}");
                return;
            }

            Main.DebugLog(() => $"SoundLoader: Found {soundFiles.Length} sound files in {folder}");

            foreach (var soundFile in soundFiles)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(soundFile);
                    var soundName = $"{trainType}_{soundType}_{fileName}";

                    // Generate config file path for this specific sound
                    var configPath = GetConfigPathForSound(soundFile, soundType);

                    // DON'T auto-generate configs - only load if exists
                    // User can create configs via the editor UI when needed

                    // Load per-sound configuration (only if exists)
                    var soundConfig = LoadSoundConfiguration(configPath);

                    // Create sound definition
                    var soundDef = new SoundDefinition(soundName, soundType)
                    {
                        filename = soundFile,
                        configPath = configPath
                    };

                    // Apply per-sound configuration if available and enabled
                    if (soundConfig != null)
                    {
                        var isEnabled = soundConfig.enabled == null || soundConfig.enabled.Value;
                        Main.mod?.Logger.Log($"[LEGACY] {soundName}: enabled={soundConfig.enabled?.ToString() ?? "null"}, will apply={isEnabled}");

                        if (isEnabled)
                        {
                            ApplyConfigurationSettings(soundDef, soundConfig);
                            Main.mod?.Logger.Log($"[LEGACY] Applied config to {soundName}");
                        }
                        else
                        {
                            Main.mod?.Logger.Log($"[LEGACY] Skipped disabled config for {soundName}");
                        }
                    }

                    // Validate the sound file can be loaded
                    try
                    {
                        ValidateSoundDefinition(soundDef);
                    }
                    catch (Exception validationEx)
                    {
                        Main.mod?.Logger.Warning($"Sound file validation failed for {soundFile}: {validationEx.Message}");
                        continue;
                    }

                    _loadedSounds[soundName] = soundDef;

                    // Add to train-specific sounds
                    if (!_trainSounds[trainType].ContainsKey(soundType))
                    {
                        _trainSounds[trainType][soundType] = new List<SoundDefinition>();
                    }
                    _trainSounds[trainType][soundType].Add(soundDef);
                    Main.DebugLog(() => $"Loaded train-specific sound: {soundName} from {soundFile}");
                }
                catch (Exception ex)
                {
                    Main.mod?.Logger.Error($"Failed to load sound file {soundFile}: {ex.Message}");
                }
            }
        }

        private void ApplyConfigurationSettings(SoundDefinition soundDef, SoundConfiguration config)
        {
            // Apply configuration values, overriding defaults where specified
            if (config.pitch.HasValue) soundDef.pitch = config.pitch.Value;
            if (config.minPitch.HasValue) soundDef.minPitch = config.minPitch.Value;
            if (config.maxPitch.HasValue) soundDef.maxPitch = config.maxPitch.Value;
            if (config.minVolume.HasValue) soundDef.minVolume = config.minVolume.Value;
            if (config.maxVolume.HasValue) soundDef.maxVolume = config.maxVolume.Value;

            // Apply animation curves
            if (config.PitchCurve != null) soundDef.pitchCurve = config.PitchCurve;
            if (config.VolumeCurve != null) soundDef.volumeCurve = config.VolumeCurve;

            // Apply randomizeStartTime setting
            if (config.randomizeStartTime.HasValue) soundDef.randomizeStartTime = config.randomizeStartTime.Value;
        }

        private void ValidateSoundDefinition(SoundDefinition soundDef)
        {
            void ValidateFile(string f) => LoadAudioClip(f);

            if (soundDef.filename != null)
                ValidateFile(soundDef.filename);

            foreach (var f in soundDef.filenames ?? Array.Empty<string>())
                ValidateFile(f);
        }

        #endregion

        #region Private - Configuration Loading

        private SoundConfiguration? LoadConfiguration(string soundTypeFolder)
        {
            var configPath = Path.Combine(soundTypeFolder, CONFIG_FILENAME);

            if (!File.Exists(configPath))
            {
                Main.DebugLog(() => $"No config.json found in {soundTypeFolder}");
                return null;
            }

            try
            {
                var jsonContent = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<SoundConfiguration>(jsonContent);

                Main.DebugLog(() => $"Loaded sound configuration from {configPath}");
                return config;
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to load sound configuration from {configPath}: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Public API - Per-Sound Configuration Management

        /// <summary>
        /// Gets the configuration file path for a specific sound file.
        /// Format: Sounds/Configs/[SoundType]/config_[soundFileName].json
        /// </summary>
        public string GetConfigPathForSound(string soundFilePath, SoundType soundType)
        {
            var soundFileName = Path.GetFileNameWithoutExtension(soundFilePath);
            var configsBasePath = Path.Combine(_baseSoundsPath, "Configs");
            var soundTypeConfigPath = Path.Combine(configsBasePath, soundType.ToString());

            // Ensure the directory exists
            Directory.CreateDirectory(soundTypeConfigPath);

            return Path.Combine(soundTypeConfigPath, $"config_{soundFileName}.json");
        }

        /// <summary>
        /// Loads configuration for a specific sound file.
        /// </summary>
        public SoundConfiguration? LoadSoundConfiguration(string configPath)
        {
            if (!File.Exists(configPath))
            {
                Main.DebugLog(() => $"No config found at {configPath}");
                return null;
            }

            try
            {
                var jsonContent = File.ReadAllText(configPath);
                Main.DebugLog(() => $"Reading config from {configPath}:\n{jsonContent}");

                var config = JsonConvert.DeserializeObject<SoundConfiguration>(jsonContent);

                if (config != null)
                {
                    Main.mod?.Logger.Log($"Loaded config from {Path.GetFileName(configPath)}: enabled={config.enabled}, pitch={config.pitch}");
                }
                else
                {
                    Main.mod?.Logger.Warning($"Config deserialized to null from {configPath}");
                }

                return config;
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to load sound configuration from {configPath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves configuration for a specific sound file.
        /// </summary>
        public void SaveSoundConfiguration(string configPath, SoundConfiguration config)
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var jsonContent = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configPath, jsonContent);

                Main.DebugLog(() => $"Saved sound configuration to {configPath}");
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to save sound configuration to {configPath}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Generates a default configuration file for a sound if it doesn't exist.
        /// </summary>
        public void EnsureDefaultConfig(string soundFilePath, SoundType soundType)
        {
            var configPath = GetConfigPathForSound(soundFilePath, soundType);

            if (File.Exists(configPath))
            {
                return; // Config already exists
            }

            // Create default configuration
            var defaultConfig = new SoundConfiguration
            {
                enabled = true,
                pitch = 1.0f,
                minPitch = null,
                maxPitch = null,
                minVolume = 0.0f,
                maxVolume = 1.0f,
                randomizeStartTime = false,
                pitchCurveData = new[]
                {
                    new KeyframeData(0.0f, 1.0f),
                    new KeyframeData(1.0f, 1.0f)
                },
                volumeCurveData = new[]
                {
                    new KeyframeData(0.0f, 1.0f),
                    new KeyframeData(1.0f, 1.0f)
                }
            };

            SaveSoundConfiguration(configPath, defaultConfig);
            Main.DebugLog(() => $"Created default config for {Path.GetFileName(soundFilePath)}");
        }

        #endregion
    }

    /// <summary>
    /// Exception thrown when there's a problem with sound configuration or loading.
    /// </summary>
    public class ConfigException : Exception
    {
        public ConfigException(string message) : base(message) { }
    }
}

