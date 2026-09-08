using System.Collections.Generic;
using DV.ThingTypes;
using UnityEngine;

namespace DvMod.ZSounds.SoundHandler
{
    public static class TrainCarTracker
    {
        private static readonly Dictionary<TrainCarType, TrainCar> _carsByType = new();
        private static readonly Dictionary<string, TrainCar> _carsByPattern = new();

        // Vanilla locomotive hierarchy path patterns
        public static readonly Dictionary<string, TrainCarType> VanillaLocoPatterns = new()
        {
            { "LocoS282A", TrainCarType.LocoSteamHeavy },
            { "LocoS060", TrainCarType.LocoS060 },
            { "LocoDH4", TrainCarType.LocoDH4 },
            { "LocoDM3", TrainCarType.LocoDM3 },
            { "LocoDM1U", TrainCarType.LocoDM1U },
            { "LocoShunter", TrainCarType.LocoShunter },
            { "LocoMicroshunter", TrainCarType.LocoMicroshunter },
        };

        // Additional patterns from CCL manifests
        private static readonly HashSet<string> _manifestPatterns = new();

        public static void RegisterManifestPattern(string pattern)
        {
            _manifestPatterns.Add(pattern);
        }

        public static void Rebuild()
        {
            _carsByType.Clear();
            _carsByPattern.Clear();

            var allCars = Object.FindObjectsOfType<TrainCar>();
            if (allCars == null) return;

            foreach (var car in allCars)
            {
                if (car == null) continue;
                if (car.name.Contains("[interior]") || car.name.Contains("Audio")) continue;

                if (!_carsByType.ContainsKey(car.carType))
                    _carsByType[car.carType] = car;

                foreach (var pattern in VanillaLocoPatterns.Keys)
                {
                    if (car.name.Contains(pattern) && !_carsByPattern.ContainsKey(pattern))
                        _carsByPattern[pattern] = car;
                }

                // Also match manifest patterns
                foreach (var pattern in _manifestPatterns)
                {
                    if (car.name.Contains(pattern) && !_carsByPattern.ContainsKey(pattern))
                        _carsByPattern[pattern] = car;
                }
            }
        }

        public static TrainCar? FindByCarType(TrainCarType type)
        {
            if (_carsByType.TryGetValue(type, out var car) && car != null && car.gameObject != null)
                return car;
            return null;
        }

        public static TrainCar? FindByHierarchyPattern(string pattern)
        {
            if (_carsByPattern.TryGetValue(pattern, out var car) && car != null && car.gameObject != null)
                return car;
            return null;
        }

        public static TrainCar? FindFromHierarchyPath(string hierarchyPath)
        {
            foreach (var pattern in VanillaLocoPatterns.Keys)
            {
                if (hierarchyPath.Contains(pattern))
                    return FindByHierarchyPattern(pattern);
            }

            foreach (var pattern in _manifestPatterns)
            {
                if (hierarchyPath.Contains(pattern))
                    return FindByHierarchyPattern(pattern);
            }

            return null;
        }

        public static void Clear()
        {
            _carsByType.Clear();
            _carsByPattern.Clear();
        }
    }
}
