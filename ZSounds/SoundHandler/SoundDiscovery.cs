using System;
using System.Collections.Generic;
using System.Linq;
using DV.ModularAudioCar;
using DV.Simulation.Controllers;
using DV.Simulation.Ports;
using DV.ThingTypes;
using UnityEngine;

namespace DvMod.ZSounds.SoundHandler
{
    /// <summary>
    /// Service for discovering and accessing audio components from locomotive prefabs.
    /// Consolidates functionality from AudioPrefabScanner, DiscoveredSoundCache, AudioMapper, and all locomotive-specific mappers.
    /// </summary>
    public class SoundDiscovery
    {
        private const string ENGINE_HIERARCHY_NAME = "[sim] Engine";

        // Cache: TrainCarType -> SoundType -> clip name(s)
        private readonly Dictionary<TrainCarType, Dictionary<SoundType, string>> _soundMappings = new();

        // Cache: TrainCarType -> SoundType -> GameObject path in hierarchy
        private readonly Dictionary<TrainCarType, Dictionary<SoundType, string>> _soundPaths = new();

        // Cache for Unknown/generic sounds: TrainCarType -> clip name -> GameObject path
        private readonly Dictionary<TrainCarType, Dictionary<string, string>> _genericSoundMappings = new();

        // Cache: TrainCarType -> Audio Prefab GameObject
        private readonly Dictionary<TrainCarType, GameObject> _audioPrefabCache = new();

        // Cache: Car GUID -> SoundType -> AudioClipPortReader (to keep references stable even if clips are swapped)
        private readonly Dictionary<string, Dictionary<SoundType, AudioClipPortReader>> _readerCache = new();

        // Cache: Car GUID -> SoundType -> LayeredAudio (to keep references stable even if clips are swapped)
        private readonly Dictionary<string, Dictionary<SoundType, LayeredAudio>> _layeredAudioCache = new();

        // Additional cache for custom locomotives: train identifier string -> TrainCarType
        private readonly Dictionary<string, TrainCarType> _identifierToCarType = new();

        /// <summary>
        /// Gets a string identifier for a train car.
        /// For custom locomotives: uses livery ID if available
        /// For standard locomotives: uses TrainCarType.ToString() or numeric value as string
        /// </summary>
        public static string GetTrainIdentifier(TrainCar car)
        {
            // Try to get livery ID first (works for custom locomotives)
            if (car.carLivery != null && !string.IsNullOrEmpty(car.carLivery.id))
            {
                return car.carLivery.id;
            }

            // For standard locomotives, use TrainCarType name if it's a valid enum value
            if (Enum.IsDefined(typeof(TrainCarType), car.carType))
            {
                return car.carType.ToString();
            }

            // For custom locomotives without livery, use numeric value
            return ((int)car.carType).ToString();
        }

        #region Initialization

        /// <summary>
        /// Scans all locomotive types and populates the internal caches.
        /// Called after world loading finishes when all base prefabs are loaded.
        /// </summary>
        public void ScanAllLocomotives()
        {
            Main.mod?.Logger.Log("SoundDiscovery: Starting scan of all locomotive audio prefabs...");
            Clear();

            int prefabsFound = 0;
            int scannedCount = 0;
            int failedCount = 0;

            // First, try to scan from spawned cars in the world (they have complete audio data)
            var spawnedCars = UnityEngine.Object.FindObjectsOfType<TrainCar>();
            var scannedTypes = new HashSet<TrainCarType>();

            if (spawnedCars != null && spawnedCars.Length > 0)
            {
                Main.mod?.Logger.Log($"SoundDiscovery: Found {spawnedCars.Length} spawned cars, attempting to scan from them first...");

                foreach (var car in spawnedCars)
                {
                    if (car != null && !scannedTypes.Contains(car.carType))
                    {
                        try
                        {
                            if (ScanSpawnedCar(car))
                            {
                                scannedTypes.Add(car.carType);
                                scannedCount++;
                                Main.mod?.Logger.Log($"SoundDiscovery: Scanned {car.carType} from spawned car instance");
                            }
                        }
                        catch (Exception ex)
                        {
                            Main.mod?.Logger.Error($"SoundDiscovery: Error scanning spawned car {car.carType}: {ex.Message}");
                        }
                    }
                }
            }

            // Then scan remaining types from prefabs
            var allTrainCarTypes = Enum.GetValues(typeof(TrainCarType)).Cast<TrainCarType>();

            foreach (var carType in allTrainCarTypes)
            {
                if (scannedTypes.Contains(carType))
                    continue; // Already scanned from spawned car

                try
                {
                    var result = ScanLocomotiveType(carType);
                    if (result == ScanResult.Success)
                    {
                        prefabsFound++;
                        scannedCount++;
                    }
                    else if (result == ScanResult.PrefabFoundButNoSounds)
                    {
                        prefabsFound++;
                        failedCount++;
                    }
                    else // ScanResult.NoPrefab
                    {
                        failedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Main.mod?.Logger.Error($"SoundDiscovery: Error scanning {carType}: {ex.Message}");
                    failedCount++;
                }
            }

            Main.mod?.Logger.Log($"SoundDiscovery: Scan complete. Prefabs found: {prefabsFound}, Sounds discovered: {scannedCount}, Failed: {failedCount}");
            Main.mod?.Logger.Log($"SoundDiscovery: {GetCacheStats()}");
        }

        private enum ScanResult
        {
            Success,
            PrefabFoundButNoSounds,
            NoPrefab
        }

        /// <summary>
        /// Clears all cached mappings. Used for testing or reloading.
        /// </summary>
        public void Clear()
        {
            _soundMappings.Clear();
            _soundPaths.Clear();
            _genericSoundMappings.Clear();
            _audioPrefabCache.Clear();
            _readerCache.Clear();
            _layeredAudioCache.Clear();
            Main.mod?.Logger.Log("SoundDiscovery: Cleared all cached mappings");
        }

        #endregion

        #region Prefab Access

        /// <summary>
        /// Gets the audio prefab GameObject for a specific train car type.
        /// </summary>
        public GameObject? GetAudioPrefab(TrainCarType carType)
        {
            // Check cache first
            if (_audioPrefabCache.TryGetValue(carType, out var cachedPrefab))
                return cachedPrefab;

            // Discover and cache
            var prefab = DiscoverAudioPrefabForType(carType);
            if (prefab != null)
            {
                _audioPrefabCache[carType] = prefab;
            }
            return prefab;
        }

        #endregion

        #region Component Discovery (replaces AudioMapper)

        /// <summary>
        /// Gets LayeredAudio component for a specific sound type on a train car.
        /// </summary>
        public LayeredAudio? GetLayeredAudio(TrainCar car, SoundType soundType)
        {
            var trainAudio = car.interior?.GetComponentInChildren<TrainAudio>();
            if (trainAudio == null)
                return null;

            return GetLayeredAudio(trainAudio, soundType);
        }

        /// <summary>
        /// Gets LayeredAudio component for a specific sound type from TrainAudio.
        /// </summary>
        public LayeredAudio? GetLayeredAudio(TrainAudio trainAudio, SoundType soundType)
        {
            // Check cache first for spawned cars
            var carGuid = trainAudio.car.logicCar?.carGuid;
            var hasGuid = !string.IsNullOrEmpty(carGuid);
            var carGuidNonNull = carGuid ?? string.Empty;

            if (hasGuid && _layeredAudioCache.TryGetValue(carGuidNonNull, out var cache) && cache.TryGetValue(soundType, out var cachedAudio))
            {
                if (cachedAudio != null)
                {
                    Main.DebugLog(() => $"GetLayeredAudio: Using cached LayeredAudio for {soundType}");
                    return cachedAudio;
                }
                // Clean up null refs
                cache.Remove(soundType);
            }

            if (!_soundMappings.TryGetValue(trainAudio.car.carType, out var mapping))
                return null;

            if (!mapping.TryGetValue(soundType, out var path))
                return null;

            var modularAudio = trainAudio as CarModularAudio;
            if (modularAudio == null)
                return null;

            var simAudio = modularAudio.audioModules.OfType<SimAudioModule>().FirstOrDefault();
            if (simAudio == null)
                return null;

            // First try direct LayeredAudioPortReader entries
            var portReaders = simAudio.layeredAudioSimReadersController.entries.OfType<LayeredAudioPortReader>();

            // Match by reader name first, then by clip name (for legacy/generic names)
            var match = portReaders.FirstOrDefault(entry =>
                string.Equals(entry.name, path, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(entry.gameObject.name, path, StringComparison.OrdinalIgnoreCase) ||
                (entry.layeredAudio?.layers != null &&
                 entry.layeredAudio.layers.Any(layer =>
                    layer?.source?.clip != null &&
                    string.Equals(layer.source.clip.name, path, StringComparison.OrdinalIgnoreCase)))
            )?.layeredAudio;

            if (match != null)
            {
                // Cache the result for spawned cars
                if (hasGuid)
                {
                    if (!_layeredAudioCache.TryGetValue(carGuidNonNull, out var dict))
                    {
                        dict = new Dictionary<SoundType, LayeredAudio>();
                        _layeredAudioCache[carGuidNonNull] = dict;
                    }
                    dict[soundType] = match;
                }
                return match;
            }

            // For steam chuff sounds, search in ChuffClipsSimReader
            if (IsChuffSoundType(soundType))
            {
                match = FindChuffLayeredAudioInChuffReader(trainAudio.car, simAudio, soundType, path);
            }

            if (match == null)
                Main.DebugLog(() => $"Could not find LayeredAudio: carType={trainAudio.car.carType}, soundType={soundType}, path={path}");
            else if (hasGuid)
            {
                // Cache the result for spawned cars
                if (!_layeredAudioCache.TryGetValue(carGuidNonNull, out var dict))
                {
                    dict = new Dictionary<SoundType, LayeredAudio>();
                    _layeredAudioCache[carGuidNonNull] = dict;
                }
                dict[soundType] = match;
            }

            return match;
        }

        /// <summary>
        /// Gets AudioClipPortReader component for a specific sound type on a train car.
        /// </summary>
        public AudioClipPortReader? GetAudioClipPortReader(TrainCar car, SoundType soundType)
        {
            var trainAudio = car.interior?.GetComponentInChildren<TrainAudio>();
            if (trainAudio == null)
                return null;

            return GetAudioClipPortReader(trainAudio, soundType);
        }

        /// <summary>
        /// Gets AudioClipPortReader component for a specific sound type from TrainAudio.
        /// </summary>
        public AudioClipPortReader? GetAudioClipPortReader(TrainAudio trainAudio, SoundType soundType)
        {
            if (!_soundMappings.TryGetValue(trainAudio.car.carType, out var mapping))
                return null;

            if (!mapping.TryGetValue(soundType, out var path))
                return null;

            var modularAudio = trainAudio as CarModularAudio;
            if (modularAudio == null)
                return null;

            var simAudio = modularAudio.audioModules.OfType<SimAudioModule>().FirstOrDefault();
            if (simAudio == null)
                return null;

            // Check cache first
            var carGuid = trainAudio.car.logicCar?.carGuid;
            var hasGuid = !string.IsNullOrEmpty(carGuid);
            var carGuidNonNull = carGuid ?? string.Empty;

            if (hasGuid && _readerCache.TryGetValue(carGuidNonNull, out var map) && map.TryGetValue(soundType, out var cached))
            {
                if (cached != null)
                    return cached;
                // Clean up null refs
                map.Remove(soundType);
            }

            // Check if the SimAudioModule is fully initialized
            if (simAudio.audioClipSimReadersController?.entries == null)
            {
                Main.DebugLog(() => $"SimAudioModule not fully initialized for {trainAudio.car.carType}, skipping validation");
                return null;
            }

            var portReaders = simAudio.audioClipSimReadersController.entries.OfType<AudioClipPortReader>();

            // Prefer matching by a stable identifier (object name),
            // but also support legacy matching by clip name for older mappings.
            var match = portReaders.FirstOrDefault(portReader =>
                string.Equals(portReader.name, path, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(portReader.gameObject.name, path, StringComparison.OrdinalIgnoreCase) ||
                (portReader.clips != null && portReader.clips.Any(clip => clip != null && string.Equals(clip.name, path, StringComparison.OrdinalIgnoreCase)))
            );

            if (match == null)
                Main.DebugLog(() => $"Could not find AudioClipPortReader: carType={trainAudio.car.carType}, soundType={soundType}, path={path}");
            else if (hasGuid)
            {
                if (!_readerCache.TryGetValue(carGuidNonNull, out var dict))
                {
                    dict = new Dictionary<SoundType, AudioClipPortReader>();
                    _readerCache[carGuidNonNull] = dict;
                }
                dict[soundType] = match;
            }

            return match;
        }

        /// <summary>
        /// Gets AudioSource array for a specific sound type on a train car.
        /// </summary>
        public AudioSource[]? GetAudioSources(TrainCar car, SoundType soundType)
        {
            var layeredAudio = GetLayeredAudio(car, soundType);
            if (layeredAudio == null)
                return null;

            return layeredAudio.layers?.Select(layer => layer?.source).Where(s => s != null).ToArray()!;
        }

        /// <summary>
        /// Gets a single AudioClip for a specific sound type on a train car.
        /// </summary>
        public AudioClip? GetAudioClip(TrainCar car, SoundType soundType)
        {
            var portReader = GetAudioClipPortReader(car, soundType);
            if (portReader?.clips != null && portReader.clips.Length > 0)
                return portReader.clips[0];

            return null;
        }

        /// <summary>
        /// Gets AudioClip array for a specific sound type on a train car.
        /// </summary>
        public AudioClip[]? GetAudioClips(TrainCar car, SoundType soundType)
        {
            var portReader = GetAudioClipPortReader(car, soundType);
            return portReader?.clips;
        }

        #endregion

        #region Cache Queries

        /// <summary>
        /// Gets the discovered sound mapping for a specific train car type.
        /// </summary>
        public Dictionary<SoundType, string>? GetMapping(TrainCarType carType)
        {
            return _soundMappings.TryGetValue(carType, out var mapping)
                ? new Dictionary<SoundType, string>(mapping)
                : null;
        }

        /// <summary>
        /// Gets the clip name for a specific train car type and sound type.
        /// </summary>
        public string? GetClipName(TrainCarType carType, SoundType soundType)
        {
            if (_soundMappings.TryGetValue(carType, out var mapping))
            {
                return mapping.TryGetValue(soundType, out var clipName) ? clipName : null;
            }
            return null;
        }

        /// <summary>
        /// Gets the GameObject hierarchy path for a specific train car type and sound type.
        /// </summary>
        public string? GetHierarchyPath(TrainCarType carType, SoundType soundType)
        {
            if (_soundPaths.TryGetValue(carType, out var paths))
            {
                return paths.TryGetValue(soundType, out var path) ? path : null;
            }
            return null;
        }

        /// <summary>
        /// Checks if a sound type is supported by a specific train car type.
        /// </summary>
        public bool IsSoundSupported(TrainCarType carType, SoundType soundType)
        {
            return _soundMappings.TryGetValue(carType, out var mapping) && mapping.ContainsKey(soundType);
        }

        /// <summary>
        /// Checks if a sound type is supported by a specific train identifier string.
        /// </summary>
        public bool IsSoundSupported(string trainIdentifier, SoundType soundType)
        {
            // Try to parse as TrainCarType first
            if (Enum.TryParse<TrainCarType>(trainIdentifier, true, out var carType))
            {
                return IsSoundSupported(carType, soundType);
            }

            // For custom locomotives, look up the TrainCarType from the identifier
            if (_identifierToCarType.TryGetValue(trainIdentifier, out var customCarType))
            {
                return IsSoundSupported(customCarType, soundType);
            }

            return false;
        }

        /// <summary>
        /// Gets all supported sound types for a specific train car type.
        /// </summary>
        public HashSet<SoundType> GetSupportedSoundTypes(TrainCarType carType)
        {
            if (_soundMappings.TryGetValue(carType, out var mapping))
            {
                return new HashSet<SoundType>(mapping.Keys);
            }
            return new HashSet<SoundType>();
        }

        /// <summary>
        /// Gets all generic/unknown sound names for a specific train car type.
        /// </summary>
        public HashSet<string> GetGenericSoundNames(TrainCarType carType)
        {
            if (_genericSoundMappings.TryGetValue(carType, out var mapping))
            {
                return new HashSet<string>(mapping.Keys);
            }
            return new HashSet<string>();
        }

        /// <summary>
        /// Gets all discovered train types (standard TrainCarType enum values).
        /// </summary>
        public IEnumerable<TrainCarType> GetAllDiscoveredTrainTypes()
        {
            return _soundMappings.Keys;
        }

        /// <summary>
        /// Gets all train identifiers (string-based) for all discovered trains.
        /// Includes both standard and custom locomotives.
        /// </summary>
        public IEnumerable<string> GetAllTrainIdentifiers()
        {
            // Return all identifiers from the cache
            var identifiers = new HashSet<string>();

            // Add all registered identifiers from spawned cars
            foreach (var identifier in _identifierToCarType.Keys)
            {
                identifiers.Add(identifier);
            }

            // Also add standard locomotive types that have been discovered
            foreach (var carType in _soundMappings.Keys)
            {
                // For standard enums, use the string name
                if (Enum.IsDefined(typeof(TrainCarType), carType))
                {
                    identifiers.Add(carType.ToString());
                }
            }

            return identifiers;
        }

        /// <summary>
        /// Returns statistics about the cache for debugging.
        /// </summary>
        public string GetCacheStats()
        {
            var totalSounds = _soundMappings.Values.Sum(m => m.Count);
            var totalGenericSounds = _genericSoundMappings.Values.Sum(m => m.Count);
            var trainTypes = _soundMappings.Keys.Count;
            return $"Cache: {trainTypes} train types, {totalSounds} known sounds, {totalGenericSounds} generic sounds";
        }

        #endregion

        #region Private: Scanning Logic

        private bool ScanSpawnedCar(TrainCar car)
        {
            var carType = car.carType;
            Main.DebugLog(() => $"SoundDiscovery: Scanning spawned {carType}...");

            // Register identifier for custom locomotives (non-enum values)
            var identifier = GetTrainIdentifier(car);
            if (!string.IsNullOrEmpty(identifier))
            {
                _identifierToCarType[identifier] = carType;
                Main.DebugLog(() => $"SoundDiscovery: Registered train identifier '{identifier}' for {carType}");
            }

            var trainAudio = car.interior?.GetComponentInChildren<TrainAudio>();
            if (trainAudio == null)
            {
                Main.DebugLog(() => $"SoundDiscovery: No TrainAudio found on spawned {carType}");
                return false;
            }

            var modularAudio = trainAudio as CarModularAudio;
            if (modularAudio == null)
            {
                Main.DebugLog(() => $"SoundDiscovery: TrainAudio is not CarModularAudio for {carType}");
                return false;
            }

            var simAudio = modularAudio.audioModules?.OfType<SimAudioModule>().FirstOrDefault();
            if (simAudio == null)
            {
                Main.DebugLog(() => $"SoundDiscovery: No SimAudioModule found for {carType}");
                return false;
            }

            int discoveredCount = 0;

            // Scan LayeredAudioPortReaders from the controller (deduplicate by instance)
            if (simAudio.layeredAudioSimReadersController?.entries != null)
            {
                var processedReaders = new HashSet<LayeredAudioPortReader>();
                Main.DebugLog(() => $"SoundDiscovery: Scanning {simAudio.layeredAudioSimReadersController.entries.Length} LayeredAudioPortReader entries from spawned car");

                foreach (var entry in simAudio.layeredAudioSimReadersController.entries)
                {
                    if (entry is LayeredAudioPortReader reader && !processedReaders.Contains(reader))
                    {
                        processedReaders.Add(reader);
                        discoveredCount += ScanLayeredAudioPortReader(carType, reader);
                    }
                }
            }

            // Scan AudioClipPortReaders from the controller (deduplicate by instance)
            if (simAudio.audioClipSimReadersController?.entries != null)
            {
                var processedReaders = new HashSet<AudioClipPortReader>();
                Main.DebugLog(() => $"SoundDiscovery: Scanning {simAudio.audioClipSimReadersController.entries.Length} AudioClipPortReader entries from spawned car");

                foreach (var entry in simAudio.audioClipSimReadersController.entries)
                {
                    if (entry is AudioClipPortReader reader && !processedReaders.Contains(reader))
                    {
                        processedReaders.Add(reader);
                        discoveredCount += ScanAudioClipPortReader(carType, reader);
                    }
                }
            }

            if (discoveredCount > 0)
            {
                Main.mod?.Logger.Log($"SoundDiscovery: Discovered {discoveredCount} sounds for {carType} from spawned car");
                return true;
            }
            else
            {
                Main.mod?.Logger.Warning($"SoundDiscovery: No sounds discovered for {carType} from spawned car");
                return false;
            }
        }

        private ScanResult ScanLocomotiveType(TrainCarType carType)
        {
            Main.DebugLog(() => $"SoundDiscovery: Scanning {carType}...");

            var audioPrefab = GetAudioPrefab(carType);
            if (audioPrefab == null)
            {
                Main.DebugLog(() => $"SoundDiscovery: No audio prefab found for {carType}");
                return ScanResult.NoPrefab;
            }

            Main.DebugLog(() => $"SoundDiscovery: Found audio prefab: {audioPrefab.name}");

            var engineObject = FindEngineHierarchy(audioPrefab.transform);
            if (engineObject == null)
            {
                Main.mod?.Logger.Warning($"SoundDiscovery: Could not find '{ENGINE_HIERARCHY_NAME}' in {carType} audio prefab");
                return ScanResult.PrefabFoundButNoSounds;
            }

            Main.DebugLog(() => $"SoundDiscovery: Found engine hierarchy at: {GetTransformPath(engineObject)}");

            int discoveredCount = ScanEngineHierarchy(carType, engineObject);

            if (discoveredCount > 0)
            {
                Main.mod?.Logger.Log($"SoundDiscovery: Discovered {discoveredCount} sounds for {carType}");
                return ScanResult.Success;
            }
            else
            {
                Main.mod?.Logger.Warning($"SoundDiscovery: No sounds discovered for {carType}");
                return ScanResult.PrefabFoundButNoSounds;
            }
        }

        private GameObject? DiscoverAudioPrefabForType(TrainCarType carType)
        {
            try
            {
                var allLiveries = Resources.FindObjectsOfTypeAll<TrainCarLivery>();
                var expectedPrefabNamePattern = GetExpectedAudioPrefabName(carType);

                foreach (var livery in allLiveries)
                {
                    if (livery.parentType != null && livery.parentType.audioPrefab != null)
                    {
                        var prefabName = livery.parentType.audioPrefab.name;

                        if (prefabName.IndexOf(expectedPrefabNamePattern, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Main.DebugLog(() => $"SoundDiscovery: Found audio prefab {prefabName} for {carType}");
                            return livery.parentType.audioPrefab;
                        }
                    }
                }

                Main.DebugLog(() => $"SoundDiscovery: Could not find audio prefab via livery system for {carType}");
                return null;
            }
            catch (Exception ex)
            {
                Main.mod?.Logger.Error($"SoundDiscovery: Error getting audio prefab for {carType}: {ex.Message}");
                return null;
            }
        }

        private string GetExpectedAudioPrefabName(TrainCarType carType)
        {
            return carType switch
            {
                TrainCarType.LocoShunter => "LocoDE2",
                TrainCarType.LocoDiesel => "LocoDE6",
                TrainCarType.LocoSteamHeavy => "LocoS282",
                TrainCarType.LocoS060 => "LocoS060",
                TrainCarType.LocoDH4 => "LocoDH4",
                TrainCarType.LocoDM3 => "LocoDM3",
                TrainCarType.LocoDM1U => "LocoDM1U",
                TrainCarType.LocoMicroshunter => "LocoMicroshunter",
                _ => carType.ToString()
            };
        }

        private Transform? FindEngineHierarchy(Transform root)
        {
            if (root.name == ENGINE_HIERARCHY_NAME)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                var found = FindEngineHierarchy(child);
                if (found != null)
                    return found;
            }

            return null;
        }

        private int ScanEngineHierarchy(TrainCarType carType, Transform engineRoot)
        {
            int discoveredCount = 0;

            // Try to find CarModularAudio or TrainAudio in the prefab
            var trainAudio = engineRoot.GetComponentInParent<TrainAudio>();
            if (trainAudio != null)
            {
                Main.DebugLog(() => $"SoundDiscovery: Found TrainAudio component on prefab");

                var modularAudio = trainAudio as CarModularAudio;
                if (modularAudio != null)
                {
                    Main.DebugLog(() => $"SoundDiscovery: Found CarModularAudio, scanning audio modules...");

                    // Scan SimAudioModule entries
                    var simAudio = modularAudio.audioModules?.OfType<SimAudioModule>().FirstOrDefault();
                    if (simAudio != null)
                    {
                        Main.DebugLog(() => $"SoundDiscovery: Found SimAudioModule");

                        // Scan LayeredAudioPortReaders
                        if (simAudio.layeredAudioSimReadersController?.entries != null)
                        {
                            Main.DebugLog(() => $"SoundDiscovery: Scanning {simAudio.layeredAudioSimReadersController.entries.Length} LayeredAudioPortReader entries");
                            foreach (var entry in simAudio.layeredAudioSimReadersController.entries)
                            {
                                if (entry is LayeredAudioPortReader reader)
                                {
                                    discoveredCount += ScanLayeredAudioPortReader(carType, reader);
                                }
                            }
                        }

                        // Scan AudioClipPortReaders
                        if (simAudio.audioClipSimReadersController?.entries != null)
                        {
                            Main.DebugLog(() => $"SoundDiscovery: Scanning {simAudio.audioClipSimReadersController.entries.Length} AudioClipPortReader entries");
                            foreach (var entry in simAudio.audioClipSimReadersController.entries)
                            {
                                if (entry is AudioClipPortReader reader)
                                {
                                    discoveredCount += ScanAudioClipPortReader(carType, reader);
                                }
                            }
                        }
                    }
                }
            }

            // Also do the old recursive scan as fallback
            discoveredCount += ScanGameObjectForAudio(carType, engineRoot);

            return discoveredCount;
        }

        private int ScanLayeredAudioPortReader(TrainCarType carType, LayeredAudioPortReader reader)
        {
            var readerName = reader.name;
            var layeredAudio = reader.layeredAudio;
            var hierarchyPath = GetTransformPath(reader.transform);

            var clipNames = new List<string>();
            if (layeredAudio != null && layeredAudio.layers != null)
            {
                foreach (var layer in layeredAudio.layers)
                {
                    if (layer?.source?.clip != null)
                    {
                        clipNames.Add(layer.source.clip.name);
                    }
                }
            }

            if (clipNames.Count > 0)
            {
                var clipNamesStr = string.Join(", ", clipNames);
                var firstClipName = clipNames[0];

                var soundType = DetermineSoundType(readerName, hierarchyPath, firstClipName);

                var identifier = readerName;

                if (readerName.Equals("Clip", StringComparison.OrdinalIgnoreCase) ||
                    readerName.Equals("clip", StringComparison.OrdinalIgnoreCase))
                {
                    identifier = firstClipName;
                    Main.DebugLog(() => $"SoundDiscovery: Using clip name '{firstClipName}' as identifier (GameObject name is generic: '{readerName}')");
                }

                if (soundType != SoundType.Unknown)
                {
                    AddMapping(carType, soundType, identifier, hierarchyPath);
                    Main.DebugLog(() => $"SoundDiscovery: Mapped {soundType} -> {identifier} (LayeredAudioPortReader '{readerName}', clips: {clipNamesStr})");
                    return 1;
                }
                else
                {
                    // Unknown sound type - treat as generic sound
                    AddMapping(carType, soundType, identifier, hierarchyPath);
                    Main.DebugLog(() => $"SoundDiscovery: Mapped generic sound -> {identifier} (LayeredAudioPortReader '{readerName}', clips: {clipNamesStr})");
                    return 1;
                }
            }

            return 0;
        }

        private int ScanAudioClipPortReader(TrainCarType carType, AudioClipPortReader reader)
        {
            var readerName = reader.name;
            var hierarchyPath = GetTransformPath(reader.transform);
            var processedSoundTypes = new HashSet<SoundType>();
            var processedClipNames = new HashSet<string>();
            int count = 0;

            if (reader.clips != null)
            {
                foreach (var clip in reader.clips)
                {
                    if (clip != null)
                    {
                        var clipName = clip.name;
                        var soundType = DetermineSoundType(readerName, hierarchyPath, clipName);

                        if (soundType != SoundType.Unknown)
                        {
                            if (!processedSoundTypes.Contains(soundType))
                            {
                                var identifier = clipName;
                                AddMapping(carType, soundType, identifier, hierarchyPath);
                                processedSoundTypes.Add(soundType);
                                count++;
                                Main.DebugLog(() => $"SoundDiscovery: Mapped {soundType} -> {identifier} (AudioClipPortReader '{readerName}', clip: {clipName})");
                            }
                        }
                        else
                        {
                            if (!processedClipNames.Contains(clipName))
                            {
                                var identifier = clipName;
                                AddMapping(carType, soundType, identifier, hierarchyPath);
                                processedClipNames.Add(clipName);
                                count++;
                                Main.DebugLog(() => $"SoundDiscovery: Mapped generic sound -> {identifier} (AudioClipPortReader '{readerName}', clip: {clipName})");
                            }
                        }
                    }
                }
            }

            return count;
        }

        private int ScanGameObjectForAudio(TrainCarType carType, Transform transform)
        {
            int discoveredCount = 0;
            var gameObject = transform.gameObject;
            var hierarchyPath = GetTransformPath(transform);

            // Check for LayeredAudioPortReader component
            var layeredAudioPortReader = gameObject.GetComponent<LayeredAudioPortReader>();
            if (layeredAudioPortReader != null)
            {
                var readerName = layeredAudioPortReader.name;
                var layeredAudio = layeredAudioPortReader.layeredAudio;

                // In prefabs, layeredAudio might be null or empty, so we scan based on the component name
                var clipNames = new List<string>();
                if (layeredAudio != null && layeredAudio.layers != null)
                {
                    foreach (var layer in layeredAudio.layers)
                    {
                        if (layer?.source?.clip != null)
                        {
                            clipNames.Add(layer.source.clip.name);
                        }
                    }
                }

                // Process even if no clips found (prefab might not have clips loaded yet)
                string clipNameForType = clipNames.Count > 0 ? clipNames[0] : "";
                var clipNamesStr = clipNames.Count > 0 ? string.Join(", ", clipNames) : "(no clips at prefab scan time)";

                var soundType = DetermineSoundType(readerName, hierarchyPath, clipNameForType);

                var identifier = readerName;

                if (readerName.Equals("Clip", StringComparison.OrdinalIgnoreCase) ||
                    readerName.Equals("clip", StringComparison.OrdinalIgnoreCase))
                {
                    if (clipNames.Count > 0)
                    {
                        identifier = clipNames[0];
                        Main.DebugLog(() => $"SoundDiscovery: Using clip name '{clipNames[0]}' as identifier (GameObject name is generic: '{readerName}')");
                    }
                }

                if (soundType != SoundType.Unknown)
                {
                    AddMapping(carType, soundType, identifier, hierarchyPath);
                    discoveredCount++;
                    Main.DebugLog(() => $"SoundDiscovery: Mapped {soundType} -> {identifier} (LayeredAudioPortReader '{readerName}', clips: {clipNamesStr})");
                }
                else
                {
                    // Unknown sound type - treat as generic sound
                    AddMapping(carType, soundType, identifier, hierarchyPath);
                    discoveredCount++;
                    Main.DebugLog(() => $"SoundDiscovery: Mapped generic sound -> {identifier} (LayeredAudioPortReader '{readerName}', clips: {clipNamesStr})");
                }
            }

            // Check for AudioClipPortReader component
            var audioClipPortReader = gameObject.GetComponent<AudioClipPortReader>();
            if (audioClipPortReader != null)
            {
                var readerName = audioClipPortReader.name;
                var processedSoundTypes = new HashSet<SoundType>(); // Track which sound types we've already added
                var processedClipNames = new HashSet<string>(); // Track Unknown sounds by clip name to avoid duplicates

                Main.DebugLog(() => $"SoundDiscovery: AudioClipPortReader '{readerName}' has {audioClipPortReader.clips?.Length ?? 0} clips at prefab scan time");

                if (audioClipPortReader.clips != null)
                {
                    foreach (var clip in audioClipPortReader.clips)
                    {
                        if (clip != null)
                        {
                            var clipName = clip.name;

                            var soundType = DetermineSoundType(readerName, hierarchyPath, clipName);

                            if (soundType != SoundType.Unknown)
                            {
                                // Known sound type - only add once per type
                                if (!processedSoundTypes.Contains(soundType))
                                {
                                    var identifier = clipName;
                                    AddMapping(carType, soundType, identifier, hierarchyPath);
                                    processedSoundTypes.Add(soundType);
                                    discoveredCount++;
                                    Main.DebugLog(() => $"SoundDiscovery: Mapped {soundType} -> {identifier} (AudioClipPortReader '{readerName}', clip: {clipName})");
                                }
                            }
                            else
                            {
                                // Unknown sound type - treat as generic sound, use clip name to differentiate
                                if (!processedClipNames.Contains(clipName))
                                {
                                    var identifier = clipName;
                                    AddMapping(carType, soundType, identifier, hierarchyPath);
                                    processedClipNames.Add(clipName);
                                    discoveredCount++;
                                    Main.DebugLog(() => $"SoundDiscovery: Mapped generic sound -> {identifier} (AudioClipPortReader '{readerName}', clip: {clipName})");
                                }
                            }
                        }
                    }
                }
            }

            // Recursively scan all children
            for (int i = 0; i < transform.childCount; i++)
            {
                discoveredCount += ScanGameObjectForAudio(carType, transform.GetChild(i));
            }

            return discoveredCount;
        }

        private SoundType DetermineSoundType(string gameObjectName, string hierarchyPath, string clipName)
        {
            var lowerName = gameObjectName.ToLower();
            var lowerPath = hierarchyPath.ToLower();
            var lowerClipName = clipName.ToLower();
            var baseName = lowerName.Replace("_layered", "").Replace("-", "");

            // Horn sounds - check clip name for specific patterns
            if (baseName.Contains("horn") || lowerPath.Contains("horn") || lowerClipName.Contains("horn"))
            {
                if (lowerName.Contains("pulse") || lowerClipName.Contains("pulse"))
                    return SoundType.HornHit;
                return SoundType.HornLoop;
            }

            // Whistle (steam locomotives)
            if (baseName.Contains("whistle") || lowerPath.Contains("whistle"))
                return SoundType.Whistle;

            // Bell
            if (baseName.Contains("bell") || lowerPath.Contains("bell"))
                return SoundType.Bell;

            // Engine sounds
            if (baseName.Contains("engine") || lowerPath.Contains("engine"))
            {
                if (baseName == "engine" || baseName.Contains("idle") || lowerName == "engine_layered")
                    return SoundType.EngineLoop;

                if (baseName.Contains("piston") || baseName.Contains("exhaust"))
                    return SoundType.EnginePiston;

                if (baseName.Contains("ignition") || baseName.Contains("starter") || clipName.Contains("Starter"))
                    return SoundType.EngineStartup;

                if (baseName.Contains("throttle") || baseName.Contains("load") || baseName.Contains("gear"))
                    return SoundType.EngineLoadLoop;

                if (clipName.Contains("FuelCutoff"))
                    return SoundType.EngineShutdown;
            }

            // Traction motors
            if (baseName.Contains("electricmotor") || baseName.Contains("tractionmotor") || baseName.Contains("motor"))
                return SoundType.TractionMotors;

            if (baseName.Contains("tmoverspeed") || baseName.Contains("overspeed"))
                return SoundType.TMOverspeed;

            if (baseName.Contains("tmcontroller") || baseName.Contains("controller"))
                return SoundType.TMController;

            // Transmission/drivetrain
            if (baseName.Contains("fluidcoupler") || baseName.Contains("coupler"))
                return SoundType.FluidCoupler;

            if (baseName.Contains("transmission") || lowerPath.Contains("transmissionengaged"))
                return SoundType.TransmissionEngaged;

            if (baseName.Contains("hydrodynamic") || lowerPath.Contains("hydrodynamicbrake"))
                return SoundType.HydroDynamicBrake;

            if (baseName.Contains("activecooler") || baseName.Contains("cooler"))
                return SoundType.ActiveCooler;

            // Compressor
            if (baseName.Contains("compressor") || lowerPath.Contains("compressor"))
                return SoundType.AirCompressor;

            // Sand flow
            if (baseName.Contains("sandflow") || baseName.Contains("sand") || lowerPath.Contains("sandflow"))
                return SoundType.SandFlow;

            // Cab fan
            if (baseName.Contains("cabfan") || (baseName.Contains("fan") && lowerPath.Contains("cab")))
                return SoundType.CabFan;

            // Dynamic brake blower
            if (baseName.Contains("dbblower") || baseName.Contains("blower"))
                return SoundType.DBBlower;

            // Contactors
            if (clipName.Contains("ContactorOn"))
                return SoundType.ContactorOn;
            if (clipName.Contains("ContactorOff"))
                return SoundType.ContactorOff;

            // TM Blow
            if (clipName.Contains("TM_Blow") || baseName.Contains("tmblow"))
                return SoundType.TMBlow;

            // Gear changes
            if (clipName.Contains("GearChange") || baseName.Contains("gearchange"))
                return SoundType.GearChange;
            if (clipName.Contains("GearGrind") || baseName.Contains("geargrind"))
                return SoundType.GearGrind;

            // Jake brake
            if (clipName.Contains("CompressionBrake") || baseName.Contains("jakebrake") || baseName.Contains("jake"))
                return SoundType.JakeBrake;

            // Steam locomotive sounds
            if (lowerPath.Contains("steam"))
            {
                // Chuff sounds
                if (clipName.Contains("ChuffLoop2.67s") || clipName.Contains("ChuffLoop2_67s"))
                    return SoundType.SteamChuff2_67Hz;
                if (clipName.Contains("ChuffLoop3s"))
                    return SoundType.SteamChuff3Hz;
                if (clipName.Contains("ChuffLoop4s") && !clipName.Contains("Ash") && !clipName.Contains("Water"))
                    return SoundType.SteamChuff4Hz;
                if (clipName.Contains("ChuffLoop5.33s") || clipName.Contains("ChuffLoop5_33s"))
                    return SoundType.SteamChuff5_33Hz;
                if (clipName.Contains("ChuffLoop8s") && !clipName.Contains("Ash") && !clipName.Contains("Water"))
                    return SoundType.SteamChuff8Hz;
                if (clipName.Contains("ChuffLoop10.67s") || clipName.Contains("ChuffLoop10_67s"))
                    return SoundType.SteamChuff10_67Hz;
                if (clipName.Contains("ChuffLoop16s") && !clipName.Contains("Ash") && !clipName.Contains("Water"))
                    return SoundType.SteamChuff16Hz;

                // Water chuffs
                if (clipName.Contains("ChuffWaterLoop4s"))
                    return SoundType.SteamChuff4HzWater;
                if (clipName.Contains("ChuffWaterLoop8s"))
                    return SoundType.SteamChuff8HzWater;
                if (clipName.Contains("ChuffWaterLoop16s"))
                    return SoundType.SteamChuff16HzWater;

                // Ash chuffs
                if (clipName.Contains("ChuffAshLoop2s"))
                    return SoundType.SteamChuff2HzAsh;
                if (clipName.Contains("ChuffAshLoop4s"))
                    return SoundType.SteamChuff4HzAsh;
                if (clipName.Contains("ChuffAshLoop8s"))
                    return SoundType.SteamChuff8HzAsh;

                // Other steam sounds
                if (clipName.Contains("Dynamo"))
                    return SoundType.Dynamo;
                if (clipName.Contains("ValveGear"))
                    return SoundType.SteamValveGear;
                if (clipName.Contains("SteamRelease"))
                    return SoundType.SteamRelease;
                if (lowerPath.Contains("admission") || clipName.Contains("Admission"))
                    return SoundType.SteamChestAdmission;
                if (clipName.Contains("Injector"))
                    return SoundType.WaterInFlow;
                if (clipName.Contains("RunningGear_Grind"))
                    return SoundType.DamagedMechanism;
                if (clipName.Contains("Lubrication") || clipName.Contains("OilPour"))
                    return SoundType.Lubricator;
                if (clipName.Contains("WaterDump"))
                    return SoundType.CrownSheetBoiling;
                if (lowerName.Contains("primingcrank") || lowerPath.Contains("priming"))
                    return SoundType.PrimingCrank;
            }

            // Fire
            if (clipName.Contains("Fire") || lowerName.Contains("fire"))
                return SoundType.Fire;

            // Wind in firebox
            if (clipName.Contains("AirFlow") && lowerPath.Contains("firebox"))
                return SoundType.WindFirebox;

            return SoundType.Unknown;
        }

        private void AddMapping(TrainCarType carType, SoundType soundType, string clipName, string hierarchyPath)
        {
            if (soundType == SoundType.Unknown)
            {
                // Store Unknown sounds separately by clip name
                if (!_genericSoundMappings.ContainsKey(carType))
                {
                    _genericSoundMappings[carType] = new Dictionary<string, string>();
                }
                _genericSoundMappings[carType][clipName] = hierarchyPath;
                Main.DebugLog(() => $"SoundDiscovery: Added generic sound {carType}/{clipName} @ {hierarchyPath}");
            }
            else
            {
                // Store known sounds by SoundType
                if (!_soundMappings.ContainsKey(carType))
                {
                    _soundMappings[carType] = new Dictionary<SoundType, string>();
                }
                if (!_soundPaths.ContainsKey(carType))
                {
                    _soundPaths[carType] = new Dictionary<SoundType, string>();
                }

                _soundMappings[carType][soundType] = clipName;
                _soundPaths[carType][soundType] = hierarchyPath;

                Main.DebugLog(() => $"SoundDiscovery: Added {carType}/{soundType} -> {clipName} @ {hierarchyPath}");
            }
        }

        #endregion

        #region Private: Helper Methods

        private bool IsChuffSoundType(SoundType soundType)
        {
            return soundType == SoundType.SteamChuffLoop ||
                   soundType == SoundType.SteamChuff2_67Hz ||
                   soundType == SoundType.SteamChuff3Hz ||
                   soundType == SoundType.SteamChuff4Hz ||
                   soundType == SoundType.SteamChuff5_33Hz ||
                   soundType == SoundType.SteamChuff8Hz ||
                   soundType == SoundType.SteamChuff10_67Hz ||
                   soundType == SoundType.SteamChuff16Hz ||
                   soundType == SoundType.SteamChuff4HzWater ||
                   soundType == SoundType.SteamChuff8HzWater ||
                   soundType == SoundType.SteamChuff16HzWater ||
                   soundType == SoundType.SteamChuff2HzAsh ||
                   soundType == SoundType.SteamChuff4HzAsh ||
                   soundType == SoundType.SteamChuff8HzAsh;
        }

        private LayeredAudio? FindChuffLayeredAudioInChuffReader(TrainCar trainCar, SimAudioModule simAudio, SoundType soundType, string clipName)
        {
            // Find ChuffClipsSimReader in audioClipSimReadersController
            if (simAudio.audioClipSimReadersController?.entries == null)
                return null;

            foreach (var entry in simAudio.audioClipSimReadersController.entries)
            {
                if (entry is ChuffClipsSimReader chuffReader)
                {
                    // Search in regular chuff loops
                    if (chuffReader.chuffLoops != null)
                    {
                        foreach (var chuffLoop in chuffReader.chuffLoops)
                        {
                            if (chuffLoop?.chuffLoop != null)
                            {
                                // PRIORITY 1: Try matching by frequency pattern in GameObject name first
                                // This is more stable than clip name matching since it doesn't change when clips are replaced
                                if (TryExtractChuffFrequency(clipName, out var frequency))
                                {
                                    var gameObjectName = chuffLoop.chuffLoop.gameObject.name;
                                    if (gameObjectName.Contains(frequency) && !gameObjectName.Contains("Water") && !gameObjectName.Contains("Ash"))
                                    {
                                        Main.DebugLog(() => $"Found chuff LayeredAudio by frequency pattern: {clipName} -> {gameObjectName}");
                                        return chuffLoop.chuffLoop;
                                    }
                                }

                                // PRIORITY 2: Fallback to clip name matching (only works before clip replacement)
                                if (chuffLoop.chuffLoop.layers != null)
                                {
                                    foreach (var layer in chuffLoop.chuffLoop.layers)
                                    {
                                        if (layer?.source?.clip != null &&
                                            string.Equals(layer.source.clip.name, clipName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            Main.DebugLog(() => $"Found chuff LayeredAudio in ChuffClipsSimReader by clip name: {clipName}");
                                            return chuffLoop.chuffLoop;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Search in water chuff loops
                    if (chuffReader.waterChuffLoops != null)
                    {
                        foreach (var chuffLoop in chuffReader.waterChuffLoops)
                        {
                            if (chuffLoop?.chuffLoop != null)
                            {
                                // PRIORITY 1: Try frequency pattern match
                                if (TryExtractChuffFrequency(clipName, out var frequency))
                                {
                                    var gameObjectName = chuffLoop.chuffLoop.gameObject.name;
                                    if (gameObjectName.Contains(frequency) && gameObjectName.Contains("Water"))
                                    {
                                        Main.DebugLog(() => $"Found water chuff LayeredAudio by frequency pattern: {clipName} -> {gameObjectName}");
                                        return chuffLoop.chuffLoop;
                                    }
                                }

                                // PRIORITY 2: Fallback to clip name match
                                if (chuffLoop.chuffLoop.layers != null)
                                {
                                    foreach (var layer in chuffLoop.chuffLoop.layers)
                                    {
                                        if (layer?.source?.clip != null &&
                                            string.Equals(layer.source.clip.name, clipName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            Main.DebugLog(() => $"Found water chuff LayeredAudio by clip name: {clipName}");
                                            return chuffLoop.chuffLoop;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Search in ash chuff loops
                    if (chuffReader.ashChuffLoops != null)
                    {
                        foreach (var chuffLoop in chuffReader.ashChuffLoops)
                        {
                            if (chuffLoop?.chuffLoop != null)
                            {
                                // PRIORITY 1: Try frequency pattern match
                                if (TryExtractChuffFrequency(clipName, out var frequency))
                                {
                                    var gameObjectName = chuffLoop.chuffLoop.gameObject.name;
                                    if (gameObjectName.Contains(frequency) && gameObjectName.Contains("Ash"))
                                    {
                                        Main.DebugLog(() => $"Found ash chuff LayeredAudio by frequency pattern: {clipName} -> {gameObjectName}");
                                        return chuffLoop.chuffLoop;
                                    }
                                }

                                // PRIORITY 2: Fallback to clip name match
                                if (chuffLoop.chuffLoop.layers != null)
                                {
                                    foreach (var layer in chuffLoop.chuffLoop.layers)
                                    {
                                        if (layer?.source?.clip != null &&
                                            string.Equals(layer.source.clip.name, clipName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            Main.DebugLog(() => $"Found ash chuff LayeredAudio by clip name: {clipName}");
                                            return chuffLoop.chuffLoop;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Extracts the frequency pattern from a chuff clip name.
        /// E.g., "Steam_ChuffLoop2.67s_01" → "2.67"
        /// </summary>
        private bool TryExtractChuffFrequency(string clipName, out string frequency)
        {
            frequency = string.Empty;

            // Pattern: ChuffLoop{frequency}s or ChuffLoop{frequency}Hz
            var match = System.Text.RegularExpressions.Regex.Match(clipName, @"ChuffLoop(\d+\.?\d*)");
            if (match.Success && match.Groups.Count > 1)
            {
                frequency = match.Groups[1].Value;
                return true;
            }

            return false;
        }

        private string GetTransformPath(Transform transform)
        {
            if (transform.parent == null)
                return transform.name;
            return GetTransformPath(transform.parent) + "/" + transform.name;
        }

        #endregion
    }
}
