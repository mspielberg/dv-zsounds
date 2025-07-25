using DV.Simulation.Ports;
using DV.ThingTypes;
using HarmonyLib;
using UnityEngine;
using System.Linq;

namespace DvMod.ZSounds
{
    /// <summary>
    /// Patches AudioClipPortReader to apply custom pitch and volume from ZSounds configuration
    /// This enables pitch/volume control for sounds that use AudioClip arrays (like horn hits, engine startup/shutdown)
    /// </summary>
    [HarmonyPatch(typeof(AudioClipPortReader), nameof(AudioClipPortReader.OnValueUpdate))]
    public static class AudioClipPortReaderPatch
    {
        public static void Prefix(AudioClipPortReader __instance, ref float ___volume, ref float ___pitch)
        {
            try
            {
                // Get the TrainCar this AudioClipPortReader belongs to
                var trainCar = __instance.GetComponentInParent<TrainCar>();
                if (trainCar == null)
                {
                    Main.DebugLog(() => "AudioClipPortReaderPatch: No TrainCar found for AudioClipPortReader");
                    return;
                }
                
                // Get the sound set for this car
                var soundSet = Registry.Get(trainCar);
                if (soundSet == null)
                {
                    Main.DebugLog(() => $"AudioClipPortReaderPatch: No sound set found for car {trainCar.ID}");
                    return;
                }
                
                // Try to determine which sound type this AudioClipPortReader represents
                var soundType = DetermineSoundType(__instance, trainCar.carType);
                if (soundType == SoundType.Unknown)
                {
                    Main.DebugLog(() => $"AudioClipPortReaderPatch: Could not determine sound type for {__instance.name}");
                    return;
                }
                
                // Get the sound definition for this sound type
                var soundDefinition = soundSet[soundType];
                if (soundDefinition == null)
                {
                    Main.DebugLog(() => $"AudioClipPortReaderPatch: No sound definition found for {soundType}");
                    return;
                }
                
                // Apply custom pitch if specified
                if (soundDefinition.pitch.HasValue)
                {
                    ___pitch *= soundDefinition.pitch.Value;
                    Main.DebugLog(() => $"AudioClipPortReaderPatch: Applied custom pitch {soundDefinition.pitch.Value} to {soundType}");
                }
                
                // Apply custom volume if specified (use maxVolume as the main volume control)
                if (soundDefinition.maxVolume.HasValue)
                {
                    ___volume *= soundDefinition.maxVolume.Value;
                    Main.DebugLog(() => $"AudioClipPortReaderPatch: Applied custom volume {soundDefinition.maxVolume.Value} to {soundType}");
                }
                
                // Store final values for logging
                var finalPitch = ___pitch;
                var finalVolume = ___volume;
                Main.DebugLog(() => $"AudioClipPortReaderPatch: Modified {soundType} - pitch: {finalPitch}, volume: {finalVolume}");
            }
            catch (System.Exception ex)
            {
                Main.mod?.Logger.Error($"AudioClipPortReaderPatch error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Attempts to determine which SoundType this AudioClipPortReader represents
        /// based on the clips it contains and the car type
        /// </summary>
        private static SoundType DetermineSoundType(AudioClipPortReader portReader, TrainCarType carType)
        {
            // First try to match using AudioMapper
            if (AudioMapper.mappings.TryGetValue(carType, out var mapper))
            {
                // Check each possible audio clip sound type to see if this portReader matches
                foreach (var soundType in SoundTypes.audioClipsSoundTypes)
                {
                    var trainCar = portReader.GetComponentInParent<TrainCar>();
                    if (trainCar != null)
                    {
                        var trainAudio = AudioUtils.GetTrainAudio(trainCar);
                        if (trainAudio != null)
                        {
                            var mappedPortReader = mapper.GetAudioClipPortReader(soundType, trainAudio);
                            if (mappedPortReader == portReader)
                            {
                                return soundType;
                            }
                        }
                    }
                }
            }
            
            // Fallback: try to guess from the clips' names or GameObject name
            var objectName = portReader.name.ToLowerInvariant();
            
            if (portReader.clips != null && portReader.clips.Length > 0)
            {
                var clipName = portReader.clips[0].name.ToLowerInvariant();
                
                if (clipName.Contains("horn") && (clipName.Contains("hit") || clipName.Contains("pulse")))
                    return SoundType.HornHit;
                if (clipName.Contains("engine") && clipName.Contains("startup"))
                    return SoundType.EngineStartup;
                if (clipName.Contains("engine") && clipName.Contains("shutdown"))
                    return SoundType.EngineShutdown;
            }
            
            // Also check GameObject name
            if (objectName.Contains("horn") && objectName.Contains("hit"))
                return SoundType.HornHit;
            if (objectName.Contains("engine") && objectName.Contains("startup"))
                return SoundType.EngineStartup;
            if (objectName.Contains("engine") && objectName.Contains("shutdown"))
                return SoundType.EngineShutdown;
            
            Main.DebugLog(() => $"AudioClipPortReaderPatch: Could not determine sound type for {objectName} with clips: {string.Join(", ", portReader.clips?.Select(c => c.name) ?? new string[0])}");
            
            return SoundType.Unknown;
        }
    }
}
