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

            // Load generic sounds first
            LoadGenericSounds();

            // Load train-specific sounds
            var trainTypeFolders = Directory.GetDirectories(baseSoundsPath)
                .Where(dir => !Path.GetFileName(dir).Equals("Generic", StringComparison.OrdinalIgnoreCase))
                .ToArray();

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

        private void LoadGenericSounds()
        {
            var genericPath = Path.Combine(baseSoundsPath, "Generic");
            if (!Directory.Exists(genericPath))
            {
                return;
            }

            var soundTypeFolders = Directory.GetDirectories(genericPath);
            foreach (var soundTypeFolder in soundTypeFolders)
            {
                var soundTypeName = Path.GetFileName(soundTypeFolder);
                if (Enum.TryParse<SoundType>(soundTypeName, true, out var soundType))
                {
                    LoadSoundsFromFolder(TrainCarType.NotSet, soundType, soundTypeFolder);
                }
            }
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
            var soundFiles = Directory.GetFiles(folder, "*.ogg")
                .Concat(Directory.GetFiles(folder, "*.wav"))
                .ToArray();

            if (soundFiles.Length == 0)
            {
                Main.mod?.Logger.Warning($"No sound files found in {folder}");
                return;
            }

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
                    if (trainType != TrainCarType.NotSet)
                    {
                        if (!trainSounds[trainType].ContainsKey(soundType))
                        {
                            trainSounds[trainType][soundType] = new List<SoundDefinition>();
                        }
                        trainSounds[trainType][soundType].Add(soundDef);
                    }

                    Main.DebugLog(() => $"Loaded sound: {soundName} from {soundFile}");
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
            }
        }

        public SoundSet CreateSoundSetForTrain(TrainCar car)
        {
            var soundSet = new SoundSet();
            var trainType = car.carType;

            if (!trainSounds.ContainsKey(trainType))
            {
                Main.DebugLog(() => $"No custom sounds found for train type: {trainType}");
                return soundSet;
            }

            // Return an empty sound set - sounds will be applied individually when selected
            // This prevents automatic application of all available sounds
            Main.DebugLog(() => $"Created empty sound set for train type: {trainType} (sounds available but not auto-applied)");
            return soundSet;
        }

        public SoundSet CreateGenericSoundSet()
        {
            var soundSet = new SoundSet();
            foreach (var sound in loadedSounds.Values.Where(s => s.IsGeneric))
            {
                sound.Apply(soundSet);
            }
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

        public IEnumerable<SoundDefinition> GetAllSounds()
        {
            return loadedSounds.Values;
        }

        /// <summary>
        /// Applies a specific sound to a train car. Useful for manual sound management.
        /// </summary>
        /// <param name="car">The train car to apply the sound to</param>
        /// <param name="soundType">The type of sound to apply</param>
        /// <param name="soundName">The name of the specific sound to apply (or null for random)</param>
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
                AudioUtils.Apply(car, soundSet);
                Main.DebugLog(() => $"Manually applied {soundType} sound: {sound.name} to {car.ID}");
            }
            else
            {
                Main.mod?.Logger.Warning($"Could not find sound for {car.carType} {soundType} (requested: {soundName})");
            }
        }
    }
}