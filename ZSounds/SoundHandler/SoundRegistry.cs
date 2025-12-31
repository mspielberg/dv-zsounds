using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DV.ThingTypes;
using Newtonsoft.Json;

namespace DvMod.ZSounds.SoundHandler
{
    /// <summary>
    /// Service for managing locomotive sound state (runtime + persistent storage).
    /// Consolidates functionality from Registry.cs and SoundStateManager.cs.
    /// </summary>
    public class SoundRegistry
    {
        private const string STATE_FILENAME = "applied_sounds.json";

        private readonly string _stateFilePath;
        private readonly SoundLoader _soundLoader;
        private readonly SoundDiscovery _soundDiscovery;

        // Runtime state: TrainCar GUID -> SoundSet
        private readonly Dictionary<string, SoundSet> _soundSets = new();

        // Customization tracking: TrainCar GUID
        private readonly HashSet<string> _customizedCars = new();

        // Persistent state data
        private SoundStateData _stateData = new();

        public SoundRegistry(string modPath, SoundLoader soundLoader, SoundDiscovery soundDiscovery)
        {
            _stateFilePath = Path.Combine(modPath, STATE_FILENAME);
            _soundLoader = soundLoader;
            _soundDiscovery = soundDiscovery;
        }

        #region Public API - Sound Set Management

        /// <summary>
        /// Gets the sound set for a train car, creating one if it doesn't exist.
        /// </summary>
        public SoundSet GetSoundSet(TrainCar car)
        {
            var carGuid = car.logicCar.carGuid;

            if (!_soundSets.TryGetValue(carGuid, out var soundSet))
            {
                soundSet = _soundLoader.CreateSoundSetForTrain(car);
                _soundSets[carGuid] = soundSet;
            }

            return soundSet;
        }

        /// <summary>
        /// Sets the sound set for a train car.
        /// </summary>
        public void SetSoundSet(TrainCar car, SoundSet soundSet)
        {
            var carGuid = car.logicCar.carGuid;
            _soundSets[carGuid] = soundSet;
        }

        /// <summary>
        /// Clears the sound set for a train car.
        /// </summary>
        public void ClearSoundSet(TrainCar car)
        {
            var carGuid = car.logicCar.carGuid;
            _soundSets.Remove(carGuid);
        }

        #endregion

        #region Public API - Customization Tracking

        /// <summary>
        /// Marks a train car as having customized sounds.
        /// </summary>
        public void MarkAsCustomized(TrainCar car)
        {
            _customizedCars.Add(car.logicCar.carGuid);
        }

        /// <summary>
        /// Checks if a train car has customized sounds.
        /// </summary>
        public bool IsCustomized(TrainCar car)
        {
            return _customizedCars.Contains(car.logicCar.carGuid);
        }

        /// <summary>
        /// Clears the customization flag for a train car.
        /// </summary>
        public void ClearCustomization(TrainCar car)
        {
            _customizedCars.Remove(car.logicCar.carGuid);
        }

        #endregion

        #region Public API - Persistent State Management

        /// <summary>
        /// Loads all saved sound states from the persistent file.
        /// </summary>
        public void LoadAllStates()
        {
            if (!File.Exists(_stateFilePath))
            {
                Main.mod?.Logger.Log("No saved sound states found - starting fresh");
                _stateData = new SoundStateData();
                return;
            }

            try
            {
                var jsonContent = File.ReadAllText(_stateFilePath);
                var loaded = JsonConvert.DeserializeObject<SoundStateData>(jsonContent);
                _stateData = loaded ?? new SoundStateData();
                Main.mod?.Logger.Log($"Loaded {_stateData.soundStates.Count} saved locomotive sound states");
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to load sound states from {_stateFilePath}: {ex.Message}");
                _stateData = new SoundStateData();
            }
        }

        /// <summary>
        /// Saves the current sound state for a locomotive.
        /// </summary>
        public void SaveSoundState(TrainCar car, SoundSet soundSet)
        {
            try
            {
                var locoId = car.ID;
                var carType = car.carType;

                // Remove existing state for this loco if present
                _stateData.soundStates.RemoveAll(s => s.locoId == locoId);

                // Only save if there are custom sounds applied
                if (soundSet.sounds.Count > 0)
                {
                    var appliedSounds = new Dictionary<string, string>();
                    foreach (var kvp in soundSet.sounds)
                    {
                        appliedSounds[kvp.Key.ToString()] = kvp.Value.name;
                    }

                    var locoState = new LocoSoundState
                    {
                        locoId = locoId,
                        carType = carType.ToString(),
                        appliedSounds = appliedSounds
                    };

                    _stateData.soundStates.Add(locoState);
                    Main.DebugLog(() => $"Saved sound state for {locoId} with {appliedSounds.Count} custom sounds");
                }
                else
                {
                    Main.DebugLog(() => $"Removed sound state for {locoId} (no custom sounds)");
                }

                // Write to file
                SaveToFile();
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to save sound state for {car.ID}: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies saved sounds to all locomotives in the world.
        /// Called after world loads to restore previously applied sounds.
        /// </summary>
        public void ApplySavedSounds()
        {
            if (_stateData.soundStates.Count == 0)
            {
                Main.mod?.Logger.Log("No saved sound states to apply");
                return;
            }

            Main.mod?.Logger.Log($"Applying saved sounds to {_stateData.soundStates.Count} locomotives...");

            var appliedCount = 0;
            var notFoundCount = 0;
            var statesToRemove = new List<LocoSoundState>();

            foreach (var locoState in _stateData.soundStates.ToList())
            {
                try
                {
                    // Find the locomotive by ID
                    var car = FindLocomotiveById(locoState.locoId);

                    if (car == null)
                    {
                        Main.mod?.Logger.Warning($"Locomotive {locoState.locoId} not found in world - removing from saved states");
                        statesToRemove.Add(locoState);
                        notFoundCount++;
                        continue;
                    }

                    // Verify car type matches (for extra safety)
                    if (car.carType.ToString() != locoState.carType)
                    {
                        Main.mod?.Logger.Warning($"Locomotive {locoState.locoId} type mismatch: expected {locoState.carType}, found {car.carType} - skipping");
                        continue;
                    }

                    // Apply each saved sound
                    var soundsApplied = 0;
                    foreach (var soundEntry in locoState.appliedSounds)
                    {
                        if (Enum.TryParse<SoundType>(soundEntry.Key, out var soundType))
                        {
                            try
                            {
                                _soundLoader.ApplySoundToTrain(car, soundType, soundEntry.Value);
                                soundsApplied++;
                            }
                            catch (Exception ex)
                            {
                                Main.mod?.Logger.Warning($"Failed to apply {soundType} sound '{soundEntry.Value}' to {locoState.locoId}: {ex.Message}");
                            }
                        }
                        else
                        {
                            Main.mod?.Logger.Warning($"Unknown sound type '{soundEntry.Key}' for locomotive {locoState.locoId}");
                        }
                    }

                    if (soundsApplied > 0)
                    {
                        MarkAsCustomized(car);
                        Main.mod?.Logger.Log($"Applied {soundsApplied} saved sounds to {locoState.locoId}");
                        appliedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Main.mod?.Logger.Error($"Error applying saved sounds to {locoState.locoId}: {ex.Message}");
                }
            }

            // Remove locomotives that no longer exist
            if (statesToRemove.Count > 0)
            {
                foreach (var state in statesToRemove)
                {
                    _stateData.soundStates.Remove(state);
                }
                SaveToFile();
            }

            Main.mod?.Logger.Log($"Finished applying saved sounds: {appliedCount} applied, {notFoundCount} not found and removed");
        }

        /// <summary>
        /// Removes the saved state for a specific locomotive.
        /// </summary>
        public void RemoveSoundState(string locoId)
        {
            try
            {
                var removed = _stateData.soundStates.RemoveAll(s => s.locoId == locoId);
                if (removed > 0)
                {
                    SaveToFile();
                    Main.DebugLog(() => $"Removed saved sound state for {locoId}");
                }
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to remove sound state for {locoId}: {ex.Message}");
            }
        }

        #endregion

        #region Private Helpers

        private void SaveToFile()
        {
            try
            {
                var jsonContent = JsonConvert.SerializeObject(_stateData, Formatting.Indented);
                File.WriteAllText(_stateFilePath, jsonContent);
                Main.DebugLog(() => $"Saved sound states to {_stateFilePath}");
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"Failed to write sound states to {_stateFilePath}: {ex.Message}");
            }
        }

        private TrainCar? FindLocomotiveById(string locoId)
        {
            // Search through all loaded train cars
            var allCars = UnityEngine.Object.FindObjectsOfType<TrainCar>();
            return allCars.FirstOrDefault(car => car.ID == locoId);
        }

        #endregion

        #region Data Structures

        [Serializable]
        private class SoundStateData
        {
            public List<LocoSoundState> soundStates = new();
        }

        [Serializable]
        private class LocoSoundState
        {
            public string locoId = "";
            public string carType = "";
            public Dictionary<string, string> appliedSounds = new();
        }

        #endregion
    }
}
