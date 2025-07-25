using DV.ModularAudioCar;
using DV.Simulation.Controllers;
using DV.Simulation.Ports;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DvMod.ZSounds.AudioMappers
{
    /// Base implementation for audio mappers
    public abstract class BaseAudioMapper : IAudioMapper
    {
        public abstract Dictionary<SoundType, string> SoundMapping { get; }

        public LayeredAudio? GetLayeredAudio(SoundType soundType, TrainAudio trainAudio)
        {
            if (!SoundMapping.TryGetValue(soundType, out var path))
                return null;

            var modularAudio = trainAudio as CarModularAudio;
            if (modularAudio == null)
                return null;

            var simAudio = modularAudio.audioModules.OfType<SimAudioModule>().FirstOrDefault();
            if (simAudio == null)
                return null;

            // First try direct LayeredAudioPortReader entries
            var portReaders = simAudio.layeredAudioSimReadersController.entries.OfType<LayeredAudioPortReader>();
            var match = portReaders.FirstOrDefault(entry => entry.name == path)?.layeredAudio;
            
            if (match != null)
                return match;

            // For steam chuff sounds, search more deeply in the hierarchy
            if (IsChuffSoundType(soundType))
            {
                match = FindChuffLayeredAudio(simAudio.transform, path);
            }
            
            if (match == null)
                Main.DebugLog(() => $"Could not find LayeredAudio: carType={trainAudio.car.carType}, soundType={soundType}, path={path}");
            return match;
        }

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

        private LayeredAudio? FindChuffLayeredAudio(UnityEngine.Transform parent, string targetName)
        {
            // Recursively search for LayeredAudio components with the target name
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                
                // Check if this child has a LayeredAudio component with the target name
                var layeredAudio = child.GetComponent<LayeredAudio>();
                if (layeredAudio != null && child.name == targetName)
                {
                    Main.DebugLog(() => $"Found nested LayeredAudio: {targetName} at path: {GetTransformPath(child)}");
                    return layeredAudio;
                }
                
                // Recursively search children
                var found = FindChuffLayeredAudio(child, targetName);
                if (found != null)
                    return found;
            }
            
            return null;
        }

        private string GetTransformPath(UnityEngine.Transform transform)
        {
            if (transform.parent == null)
                return transform.name;
            return GetTransformPath(transform.parent) + "/" + transform.name;
        }

        public AudioClipPortReader? GetAudioClipPortReader(SoundType soundType, TrainAudio trainAudio)
        {
            if (!SoundMapping.TryGetValue(soundType, out var path))
                return null;

            var modularAudio = trainAudio as CarModularAudio;
            if (modularAudio == null)
                return null;

            var simAudio = modularAudio.audioModules.OfType<SimAudioModule>().FirstOrDefault();
            if (simAudio == null)
                return null;

            // Check if the SimAudioModule is fully initialized
            if (simAudio.audioClipSimReadersController?.entries == null)
            {
                Main.DebugLog(() => $"SimAudioModule not fully initialized for {trainAudio.car.carType}, skipping HornHit validation");
                return null;
            }

            var portReaders = simAudio.audioClipSimReadersController.entries.OfType<AudioClipPortReader>();
            
            var match = portReaders.FirstOrDefault(portReader => portReader.clips.Any(clip => clip.name == path));
            if (match == null)
                Main.DebugLog(() => $"Could not find AudioClipPortReader: carType={trainAudio.car.carType}, soundType={soundType}, path={path}");
            return match;
        }
    }
}
