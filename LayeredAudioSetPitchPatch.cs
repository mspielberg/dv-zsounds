using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using DV.ThingTypes;

namespace DvMod.ZSounds
{
    // Harmony patch that intercepts LayeredAudio.SetPitch calls to apply custom pitch curves
    // from sound configurations. Needed because ChuffClipsSimReader continuously overwrites 
    // pitch values, preventing custom curves from being applied.
    [HarmonyPatch(typeof(LayeredAudio), "SetPitch")]
    public static class LayeredAudioSetPitchPatch
    {
        // Performance caches to avoid expensive repeated operations
        private static readonly Dictionary<int, (TrainCar trainCar, SoundType soundType)?> _trainInfoCache = new Dictionary<int, (TrainCar, SoundType)?>();
        private static readonly Dictionary<int, bool> _hasPitchCurveCache = new Dictionary<int, bool>();
        private static readonly Dictionary<string, TrainCar> _trainCarCache = new Dictionary<string, TrainCar>();
        
        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            Transform current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }
        
        public static void Prefix(LayeredAudio __instance, ref float __0)
        {
            if (__instance == null || Main.soundLoader == null)
            {
                return;
            }
            
            var instanceId = __instance.GetInstanceID();
            var audioName = __instance.name;
            var currentPitch = __0;  // Capture the value for logging
            
            // Remove verbose ALL CALLS logging since whistles don't use LayeredAudio
            // Main.DebugLog(() => $"LayeredAudioSetPitch: ALL CALLS - '{audioName}' with pitch {currentPitch:F3}");
            
            // Ultra-fast cache check - skip if no pitch curve
            if (_hasPitchCurveCache.TryGetValue(instanceId, out var hasCurve) && !hasCurve)
            {
                return;
            }

            // Early exit: Quick check if this looks like a sound we might handle
            if (!audioName.Contains("ChuffsPerSecond") && !audioName.Contains("Bell") && !audioName.Contains("Whistle") && !audioName.Contains("whistle"))
            {
                _hasPitchCurveCache[instanceId] = false; // Cache negative result
                return;
            }

            // Debug: Log when we're processing whistle sounds
            if (audioName.Contains("Whistle") || audioName.Contains("whistle"))
            {
                Main.DebugLog(() => $"LayeredAudioSetPitch: WHISTLE DETECTED - Processing '{audioName}' with pitch {currentPitch:F3}");
            }

            try
            {
                // Fast cached train info extraction
                if (!_trainInfoCache.TryGetValue(instanceId, out var trainInfo))
                {
                    trainInfo = ExtractTrainInfoFromPath(__instance);
                    _trainInfoCache[instanceId] = trainInfo;
                }

                if (trainInfo == null)
                {
                    _hasPitchCurveCache[instanceId] = false; // Cache negative result
                    return;
                }

                var (trainCar, soundType) = trainInfo.Value;

                // Early exit: Only proceed if this car has custom sounds applied
                if (!Registry.IsCustomized(trainCar))
                {
                    _hasPitchCurveCache[instanceId] = false; // Cache negative result
                    return;
                }
                
                // Try to get pitch curve from available sounds (CommsRadio approach)
                var availableSounds = Main.soundLoader?.GetAvailableSoundsForTrain(trainCar.carType);
                if (availableSounds != null && availableSounds.TryGetValue(soundType, out var soundsOfType))
                {
                    // Use the first available sound of this type that has a pitch curve
                    var soundWithCurve = soundsOfType.FirstOrDefault(s => s.pitchCurve != null);
                    if (soundWithCurve?.pitchCurve != null)
                    {
                        _hasPitchCurveCache[instanceId] = true; // Cache positive result
                        
                        // Apply the pitch curve
                        var normalizedInput = NormalizePitchInput(__0, soundType);
                        var curveMultiplier = soundWithCurve.pitchCurve.Evaluate(normalizedInput);
                        
                        // Modify the pitch with the curve
                        var originalPitch = __0;
                        var finalPitch = __0 *= curveMultiplier;
                        
                        // Enhanced debug logging for all sound types
                        Main.DebugLog(() => $"LayeredAudioSetPitchPatch: Applied pitch curve to {soundType} - original: {originalPitch:F2}, normalized: {normalizedInput:F2}, multiplier: {curveMultiplier:F2}, final: {finalPitch:F2}");
                        return;
                    }
                }

                // Fallback: try Registry approach
                var soundSet = Registry.Get(trainCar);
                var soundDefinition = soundSet?[soundType];
                
                if (soundDefinition?.pitchCurve != null)
                {
                    _hasPitchCurveCache[instanceId] = true; // Cache positive result
                    
                    // Apply the pitch curve from Registry
                    var normalizedInput = NormalizePitchInput(__0, soundType);
                    var curveMultiplier = soundDefinition.pitchCurve.Evaluate(normalizedInput);
                    
                    var originalPitch = __0;
                    var finalPitch = __0 *= curveMultiplier;
                    
                    Main.DebugLog(() => $"LayeredAudioSetPitchPatch: Applied Registry pitch curve to {soundType} - original: {originalPitch:F2}, normalized: {normalizedInput:F2}, multiplier: {curveMultiplier:F2}, final: {finalPitch:F2}");
                    return;
                }
                
                // No pitch curve available - cache negative result
                _hasPitchCurveCache[instanceId] = false;
            }
            catch (System.Exception ex)
            {
                Main.mod?.Logger?.Error($"LayeredAudioSetPitchPatch error: {ex}");
                _hasPitchCurveCache[instanceId] = false; // Cache negative result on error
            }
        }

        // Normalizes input values for pitch curve evaluation based on sound type
        private static float NormalizePitchInput(float inputValue, SoundType soundType)
        {
            // For steam chuff sounds, input is frequency, normalize based on typical locomotive speed ranges
            if (soundType.ToString().Contains("SteamChuff"))
            {
                return NormalizeChuffFrequency(inputValue, soundType);
            }
            
            // For other sounds like Whistle, Bell, Horn - input is already a pitch value (typically 0.5-2.0)
            // Normalize to 0-1 range assuming typical pitch range of 0.5 to 2.0
            const float minPitch = 0.5f;
            const float maxPitch = 2.0f;
            
            var normalized = Mathf.Clamp01((inputValue - minPitch) / (maxPitch - minPitch));
            
            Main.DebugLog(() => $"LayeredAudioSetPitchPatch: Normalized pitch {inputValue:F2} to {normalized:F3} for {soundType}");
            
            return normalized;
        }

        // Normalizes chuff frequency to 0-1 range for pitch curve evaluation
        private static float NormalizeChuffFrequency(float chuffFrequency, SoundType soundType)
        {
            // For steam chuff sounds, normalize the frequency based on typical locomotive speed ranges
            const float minFreq = 2.0f;   // Minimum chuff frequency (idle)
            const float maxFreq = 16.0f;  // Maximum chuff frequency (high speed)
            
            // Clamp and normalize to 0-1 range
            var normalized = Mathf.Clamp01((chuffFrequency - minFreq) / (maxFreq - minFreq));
            
            Main.DebugLog(() => $"LayeredAudioSetPitchPatch: Normalized frequency {chuffFrequency:F2} Hz to {normalized:F3} for {soundType}");
            
            return normalized;
        }

        // Fast extraction of train info from known path patterns without expensive searches.
        // Uses aggressive caching to avoid repeated expensive operations.
        private static (TrainCar trainCar, SoundType soundType)? ExtractTrainInfoFromPath(LayeredAudio layeredAudio)
        {
            try
            {
                // Fast TrainCar lookup using GetComponentInParent first (most efficient)
                var trainCar = layeredAudio.GetComponentInParent<TrainCar>();
                
                // If GetComponentInParent fails, check cache first, then fall back to search
                if (trainCar == null)
                {
                    var hierarchyPath = GetHierarchyPath(layeredAudio.transform);
                    
                    // Extract the base locomotive name from path for caching
                    string? cacheKey = null;
                    if (hierarchyPath.Contains("LocoS282A")) cacheKey = "LocoS282A";
                    else if (hierarchyPath.Contains("LocoS060")) cacheKey = "LocoS060";
                    else if (hierarchyPath.Contains("LocoDH4")) cacheKey = "LocoDH4";
                    else if (hierarchyPath.Contains("LocoDM3")) cacheKey = "LocoDM3";
                    else if (hierarchyPath.Contains("LocoDM1U")) cacheKey = "LocoDM1U";
                    else if (hierarchyPath.Contains("LocoShunter")) cacheKey = "LocoShunter";
                    else if (hierarchyPath.Contains("LocoMicroshunter")) cacheKey = "LocoMicroshunter";
                    
                    if (cacheKey != null)
                    {
                        // Check cache first
                        if (_trainCarCache.TryGetValue(cacheKey, out trainCar) && trainCar != null)
                        {
                            // Verify cached trainCar is still valid
                            if (trainCar.gameObject == null)
                            {
                                _trainCarCache.Remove(cacheKey);
                                trainCar = null;
                            }
                        }
                        
                        // If not in cache or invalid, do targeted search
                        if (trainCar == null)
                        {
                            var carType = GetCarTypeFromPattern(cacheKey);
                            var allTrainCars = UnityEngine.Object.FindObjectsOfType<TrainCar>();
                            foreach (var candidate in allTrainCars)
                            {
                                if (candidate.name.Contains(cacheKey) && 
                                    !candidate.name.Contains("[interior]") && 
                                    !candidate.name.Contains("Audio") &&
                                    candidate.carType == carType)
                                {
                                    trainCar = candidate;
                                    _trainCarCache[cacheKey] = trainCar; // Cache the result
                                    break;
                                }
                            }
                        }
                    }
                }

                if (trainCar == null)
                {
                    return null;
                }

                // Determine sound type from the audio name (no need for full path analysis)
                var soundType = DetermineSoundTypeFromPath(layeredAudio.name, "");
                if (soundType == SoundType.Unknown)
                {
                    return null;
                }

                return (trainCar, soundType);
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"LayeredAudioSetPitchPatch: Error extracting train info: {ex}");
                return null;
            }
        }

        // Get TrainCarType from locomotive pattern - avoiding string analysis
        private static TrainCarType GetCarTypeFromPattern(string pattern)
        {
            switch (pattern)
            {
                case "LocoS282A": return TrainCarType.LocoSteamHeavy;
                case "LocoS060": return TrainCarType.LocoS060;
                case "LocoDH4": return TrainCarType.LocoDH4;
                case "LocoDM3": return TrainCarType.LocoDM3;
                case "LocoDM1U": return TrainCarType.LocoDM1U;
                case "LocoShunter": return TrainCarType.LocoShunter;
                case "LocoMicroshunter": return TrainCarType.LocoMicroshunter;
                default: return TrainCarType.NotSet;
            }
        }

        // Determine sound type from path and audio name - more efficient than the old method
        private static SoundType DetermineSoundTypeFromPath(string audioName, string hierarchyPath)
        {
            // Steam chuff sound detection - check for chuff frequency patterns
            if (audioName.Contains("ChuffsPerSecond") || hierarchyPath.Contains("ChuffsPerSecond"))
            {
                // Extract frequency from name patterns like "2.67ChuffsPerSecond", "4ChuffsPerSecond", etc.
                if (audioName.Contains("2.67ChuffsPerSecond")) return SoundType.SteamChuff2_67Hz;
                if (audioName.Contains("3ChuffsPerSecond")) return SoundType.SteamChuff3Hz;
                if (audioName.Contains("4WaterChuffsPerSecond")) return SoundType.SteamChuff4HzWater;
                if (audioName.Contains("4ChuffsPerSecond")) return SoundType.SteamChuff4Hz;
                if (audioName.Contains("5.33ChuffsPerSecond")) return SoundType.SteamChuff5_33Hz;
                if (audioName.Contains("8WaterChuffsPerSecond")) return SoundType.SteamChuff8HzWater;
                if (audioName.Contains("8ChuffsPerSecond")) return SoundType.SteamChuff8Hz;
                if (audioName.Contains("10.67ChuffsPerSecond")) return SoundType.SteamChuff10_67Hz;
                if (audioName.Contains("16WaterChuffsPerSecond")) return SoundType.SteamChuff16HzWater;
                if (audioName.Contains("16ChuffsPerSecond")) return SoundType.SteamChuff16Hz;
                if (audioName.Contains("2AshChuffsPerSecond")) return SoundType.SteamChuff2HzAsh;
                if (audioName.Contains("4AshChuffsPerSecond")) return SoundType.SteamChuff4HzAsh;
                if (audioName.Contains("8AshChuffsPerSecond")) return SoundType.SteamChuff8HzAsh;
            }

            // Other layered audio types
            if (audioName.Contains("Bell") || audioName.Contains("bell")) return SoundType.Bell;
            if (audioName.Contains("Whistle") || audioName.Contains("whistle")) return SoundType.Whistle;
            if (audioName.Contains("Horn") || audioName.Contains("horn")) return SoundType.HornLoop;

            return SoundType.Unknown;
        }

        // Clear all performance caches - call when trains are destroyed or level changes
        public static void ClearCaches()
        {
            _trainInfoCache.Clear();
            _hasPitchCurveCache.Clear();
            _trainCarCache.Clear();
        }
    }
}
