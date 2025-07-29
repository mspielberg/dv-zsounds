using DV.ThingTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DvMod.ZSounds
{
    public class FolderSoundLoader
    {
        private readonly string baseSoundsPath;
        private readonly Dictionary<string, SoundDefinition> loadedSounds;
        private readonly Dictionary<TrainCarType, Dictionary<SoundType, List<SoundDefinition>>> trainSounds;

        public FolderSoundLoader(string basePath)
        {
            baseSoundsPath = Path.Combine(basePath, "Sounds");
            loadedSounds = new Dictionary<string, SoundDefinition>();
            trainSounds = new Dictionary<TrainCarType, Dictionary<SoundType, List<SoundDefinition>>>();
        }

        public void LoadAllSounds()
        {
            Main.mod?.Logger.Log($"Loading sounds from folder structure: {baseSoundsPath}");
            
            if (!Directory.Exists(baseSoundsPath))
            {
                Main.mod?.Logger.Warning($"Sounds directory not found: {baseSoundsPath}");
                return;
            }

            loadedSounds.Clear();
            trainSounds.Clear();

            // Load train sounds
            var trainTypeFolders = Directory.GetDirectories(baseSoundsPath);

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

            Main.mod?.Logger.Log($"Loaded {loadedSounds.Count} sound definitions from folder structure");
        }

        private void LoadTrainSounds(TrainCarType trainType, string trainTypeFolder)
        {
            if (!trainSounds.ContainsKey(trainType))
            {
                trainSounds[trainType] = new Dictionary<SoundType, List<SoundDefinition>>();
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
            LoadSoundsFromFolder(trainType, soundType, folder, null);
        }
        
        private void LoadSoundsFromFolder(TrainCarType trainType, SoundType soundType, string folder, SoundConfiguration? globalConfig)
        {
            var isGeneric = trainType == TrainCarType.NotSet;
            Main.DebugLog(() => $"FolderSoundLoader: Loading sounds for {trainType}/{soundType} from {folder}");
            
            // Load local configuration for this sound type folder
            var localConfig = SoundConfigurationLoader.LoadConfiguration(folder);
            
            // Use global config as fallback
            var config = localConfig ?? globalConfig;
            if (config != null)
            {
                Main.DebugLog(() => $"FolderSoundLoader: Loaded configuration for {trainType}/{soundType}");
            }
            else
            {
                Main.DebugLog(() => $"FolderSoundLoader: No configuration found for {trainType}/{soundType}, using defaults");
            }
            
            var soundFiles = Directory.GetFiles(folder, "*.ogg")
                .Concat(Directory.GetFiles(folder, "*.wav"))
                .ToArray();

            if (soundFiles.Length == 0)
            {
                Main.mod?.Logger.Warning($"No sound files found in {folder}");
                return;
            }
            
            Main.DebugLog(() => $"FolderSoundLoader: Found {soundFiles.Length} sound files in {folder}");

            foreach (var soundFile in soundFiles)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(soundFile);
                    var soundName = $"{trainType}_{soundType}_{fileName}";
                    
                    // Create sound definition
                    var soundDef = new SoundDefinition(soundName, soundType)
                    {
                        filename = soundFile
                    };

                    // Apply default settings based on sound type
                    ApplyDefaultSettings(soundDef, soundType);
                    
                    // Apply configuration settings if available
                    if (config != null)
                    {
                        ApplyConfigurationSettings(soundDef, config);
                    }

                    // Validate the sound file can be loaded
                    try
                    {
                        soundDef.Validate();
                    }
                    catch (Exception validationEx)
                    {
                        Main.mod?.Logger.Warning($"Sound file validation failed for {soundFile}: {validationEx.Message}");
                        continue;
                    }

                    loadedSounds[soundName] = soundDef;

                    // Add to train-specific sounds
                    if (!trainSounds[trainType].ContainsKey(soundType))
                    {
                        trainSounds[trainType][soundType] = new List<SoundDefinition>();
                    }
                    trainSounds[trainType][soundType].Add(soundDef);
                    Main.DebugLog(() => $"Loaded train-specific sound: {soundName} from {soundFile}");
                }
                catch (Exception ex)
                {
                    Main.mod?.Logger.Error($"Failed to load sound file {soundFile}: {ex.Message}");
                }
            }
        }

        private void ApplyDefaultSettings(SoundDefinition soundDef, SoundType soundType)
        {
            // Apply default pitch and volume settings based on sound type
            switch (soundType)
            {
                case SoundType.HornLoop:
                case SoundType.Whistle:
                    soundDef.minPitch = 0.97f;
                    soundDef.maxPitch = 1.0f;
                    break;
                case SoundType.EngineStartup:
                    soundDef.fadeStart = 0.18f;
                    soundDef.fadeDuration = 2.0f;
                    break;
                case SoundType.EngineShutdown:
                    soundDef.fadeStart = 0.27f;
                    soundDef.fadeDuration = 1.0f;
                    break;
                case SoundType.Dynamo:
                    soundDef.minPitch = 0.8f;
                    soundDef.maxPitch = 2.0f;
                    soundDef.minVolume = 0.2f;
                    soundDef.maxVolume = 0.8f;
                    break;
                case SoundType.AirCompressor:
                    soundDef.minPitch = 0.9f;
                    soundDef.maxPitch = 1.1f;
                    soundDef.minVolume = 0.0f;
                    soundDef.maxVolume = 0.8f;
                    break;
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
            if (config.fadeStart.HasValue) soundDef.fadeStart = config.fadeStart.Value;
            if (config.fadeDuration.HasValue) soundDef.fadeDuration = config.fadeDuration.Value;
            
            // Apply animation curves
            if (config.PitchCurve != null) soundDef.pitchCurve = config.PitchCurve;
            if (config.VolumeCurve != null) soundDef.volumeCurve = config.VolumeCurve;
            
            Main.DebugLog(() => $"Applied configuration settings to {soundDef.name}: " +
                              $"Pitch={config.pitch}, MinPitch={config.minPitch}, MaxPitch={config.maxPitch}, " +
                              $"PitchCurve={config.PitchCurve != null}, VolumeCurve={config.VolumeCurve != null}");
        }

        public SoundSet CreateSoundSetForTrain(TrainCar car)
        {
            var soundSet = new SoundSet();
            var trainType = car.carType;

            // Always return empty sound set - sounds only applied when manually selected via CommsRadio
            // This ensures no automatic sound replacement occurs
            Main.DebugLog(() => $"Created empty sound set for train type: {trainType} - no automatic sound application");
            return soundSet;
        }

        public Dictionary<SoundType, List<SoundDefinition>> GetAvailableSoundsForTrain(TrainCarType trainType)
        {
            return trainSounds.TryGetValue(trainType, out var sounds) ? sounds : new Dictionary<SoundType, List<SoundDefinition>>();
        }

        public List<SoundDefinition> GetSoundsOfType(SoundType soundType)
        {
            return loadedSounds.Values.Where(s => s.type == soundType).ToList();
        }

        public SoundDefinition? GetSound(string soundName)
        {
            return loadedSounds.TryGetValue(soundName, out var sound) ? sound : null;
        }

        // Applies a specific sound to a train car using the modern sound replacement system.
        public void ApplySoundToTrain(TrainCar car, SoundType soundType, string? soundName = null)
        {
            var soundSet = Registry.Get(car);
            SoundDefinition? sound = null;

            if (soundName != null)
            {
                sound = GetSound(soundName);
            }
            else if (trainSounds.TryGetValue(car.carType, out var availableTrainSounds))
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
                
                // Apply sound changes using the registry system
                AudioUtils.Apply(car, soundSet);
                Main.DebugLog(() => $"Applied {soundType} sound: {sound.name} to {car.ID}");
            }
            else
            {
                Main.mod?.Logger.Warning($"Could not find sound for {car.carType} {soundType} (requested: {soundName})");
            }
        }
    }
}