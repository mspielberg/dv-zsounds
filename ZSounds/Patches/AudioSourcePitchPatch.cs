using System;
using System.Collections.Generic;
using DV.ThingTypes;
using HarmonyLib;
using UnityEngine;

namespace DvMod.ZSounds.Patches
{
    // Harmony patch that intercepts AudioSource.pitch property sets to apply custom pitch curves
    // for whistle sounds. Needed because whistles use AudioSource directly instead of LayeredAudio.
    [HarmonyPatch(typeof(AudioSource))]
    [HarmonyPatch("pitch", MethodType.Setter)]
    public static class AudioSourcePitchPatch
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

        public static void Prefix(AudioSource __instance, ref float value)
        {
            var instanceId = __instance?.GetInstanceID() ?? 0;
            var audioName = __instance?.name ?? "null";
            var currentPitch = value;  // Capture the value for logging

            // Early null checks
            if (__instance == null)
            {
                return;
            }

            // Ultra-fast cache check - if we've already determined this audio has no pitch curve, skip entirely
            if (_hasPitchCurveCache.TryGetValue(instanceId, out var hasCurve) && !hasCurve)
            {
                return;
            }

            // Early exit: Quick check if this looks like a sound we might handle
            // Handle all non-speed-related sounds that could benefit from pitch curves
            if (!IsNonSpeedRelatedSound(audioName))
            {
                _hasPitchCurveCache[instanceId] = false; // Cache negative result
                return;
            }

            // Debug: Log when we're processing supported sounds
            bool isTargetSound = audioName.Contains("whistle") || audioName.Contains("Whistle") ||
                                audioName.Contains("bell") || audioName.Contains("Bell");
            if (isTargetSound)
            {
                Main.DebugLog(() => $"AudioSourcePitchPatch: PROCESSING {audioName} - checking for curves");
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
                    if (isTargetSound)
                    {
                        Main.DebugLog(() => $"AudioSourcePitchPatch: FAIL - {audioName} - could not extract train info");
                    }
                    return;
                }

                var (trainCar, soundType) = trainInfo.Value;

                // ONLY apply pitch curves from the locomotive's SoundSet (actively selected sounds)
                // DO NOT use available sounds - that would apply configs to vanilla sounds!
                var soundSet = Main.registryService?.GetSoundSet(trainCar);
                var soundDefinition = soundSet?[soundType];

                if (soundDefinition?.pitchCurve != null)
                {
                    _hasPitchCurveCache[instanceId] = true; // Cache positive result

                    // Apply the pitch curve
                    var normalizedInput = NormalizePitchInput(value, soundType);
                    var curvePitchValue = soundDefinition.pitchCurve.Evaluate(normalizedInput);

                    // Replace the pitch with the curve value (not multiply)
                    var originalPitch = value;
                    var finalPitch = value = curvePitchValue;

                    // Log detailed debugging for curve evaluation
                    Main.DebugLog(() => $"AudioSourcePitchPatch: Applied pitch curve to {soundType} - original: {originalPitch:F2} → normalized: {normalizedInput:F3} → curve: {curvePitchValue:F2} → final: {finalPitch:F2}");
                    return;
                }

                // No pitch curve in SoundSet, use defaults
                _hasPitchCurveCache[instanceId] = false;
                if (isTargetSound)
                {
                    Main.DebugLog(() => $"AudioSourcePitchPatch: No pitch curve for {soundType} in SoundSet, using defaults");
                }
            }
            catch (System.Exception ex)
            {
                Main.mod?.Logger?.Error($"AudioSourcePitchPatch error: {ex}");
                _hasPitchCurveCache[instanceId] = false; // Cache negative result on error
            }
        }

        // Determines if a sound is non-speed-related and eligible for pitch curve processing
        // Only handles sound types supported by the mod's SoundType enum
        private static bool IsNonSpeedRelatedSound(string audioName)
        {
            // Convert to lowercase for case-insensitive matching
            var name = audioName.ToLower();

            // Only handle supported sound types that are non-speed-related
            // Horn and whistle sounds
            if (name.Contains("horn") || name.Contains("whistle")) return true;

            // Bell sounds
            if (name.Contains("bell")) return true;

            // Air compressor sounds
            if (name.Contains("compressor")) return true;

            // Engine startup/shutdown sounds (non-speed-related)
            if (name.Contains("enginestartup") || name.Contains("engineignition")) return true;
            if (name.Contains("engineshutdown")) return true;

            // Dynamo/electrical sounds
            if (name.Contains("dynamo")) return true;

            return false;
        }

        // Normalizes input values for pitch curve evaluation based on sound type
        private static float NormalizePitchInput(float inputValue, SoundType soundType)
        {
            float normalized;

            if (soundType == SoundType.Whistle)
            {
                // For whistles, based on observed values: 0.5 to 1.80
                // This gives us the full curve range for whistle input
                const float minPitch = 0.5f;
                const float maxPitch = 1.8f;
                normalized = Mathf.Clamp01((inputValue - minPitch) / (maxPitch - minPitch));
            }
            else
            {
                // For other sounds - use broader range
                const float minPitch = 0.5f;
                const float maxPitch = 2.0f;
                normalized = Mathf.Clamp01((inputValue - minPitch) / (maxPitch - minPitch));
            }

            return normalized;
        }

        // Fast extraction of train info from known path patterns without expensive searches.
        // Uses aggressive caching to avoid repeated expensive operations.
        private static (TrainCar trainCar, SoundType soundType)? ExtractTrainInfoFromPath(AudioSource audioSource)
        {
            try
            {
                // Fast TrainCar lookup using GetComponentInParent first (most efficient)
                var trainCar = audioSource.GetComponentInParent<TrainCar>();

                // If GetComponentInParent fails, check cache first, then fall back to search
                if (trainCar == null)
                {
                    var hierarchyPath = GetHierarchyPath(audioSource.transform);

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

                // Determine sound type from the audio name
                var soundType = DetermineSoundTypeFromPath(audioSource.name, "");
                if (soundType == SoundType.Unknown)
                {
                    return null; // Don't handle unsupported sound types
                }

                return (trainCar, soundType);
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"AudioSourcePitchPatch: Error extracting train info: {ex}");
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

        // Determine sound type from audio name - only maps to supported SoundType enum values
        private static SoundType DetermineSoundTypeFromPath(string audioName, string hierarchyPath)
        {
            var name = audioName.ToLower();

            // Map to existing supported SoundType enum values only
            if (name.Contains("bell")) return SoundType.Bell;
            if (name.Contains("whistle")) return SoundType.Whistle;
            if (name.Contains("horn")) return SoundType.HornLoop;
            if (name.Contains("compressor")) return SoundType.AirCompressor;
            if (name.Contains("enginestartup") || name.Contains("engineignition")) return SoundType.EngineStartup;
            if (name.Contains("engineshutdown")) return SoundType.EngineShutdown;
            if (name.Contains("dynamo")) return SoundType.Dynamo;

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
