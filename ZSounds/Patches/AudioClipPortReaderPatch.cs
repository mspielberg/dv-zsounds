using System.Linq;
using DV.Simulation.Ports;
using DV.ThingTypes;
using HarmonyLib;

namespace DvMod.ZSounds.Patches
{
    // Patches AudioClipPortReader to apply custom pitch and volume from ZSounds configuration
    // This enables pitch/volume control for sounds that use AudioClip arrays (like horn hits, engine startup/shutdown)
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
                    // This is expected for non-train audio components (environment, UI, etc.)
                    // Silently return without logging
                    return;
                }

                // Get the sound set for this car
                var soundSet = Main.registryService?.GetSoundSet(trainCar);

                if (soundSet == null)
                {
                    Main.DebugLog(() => $"AudioClipPortReaderPatch: No sound set found for car {trainCar.ID}");
                    return;
                }

                // Try to determine which sound type this AudioClipPortReader represents
                var soundType = DetermineSoundType(__instance, trainCar.carType);

                SoundDefinition? soundDefinition = null;

                if (soundType == SoundType.Unknown)
                {
                    // For Unknown type, try to match by clip name for generic sounds
                    soundDefinition = TryGetGenericSoundDefinition(__instance, trainCar, soundSet);

                    if (soundDefinition == null)
                    {
                        Main.DebugLog(() => $"AudioClipPortReaderPatch: Could not determine sound type or match generic sound for {__instance.name}");
                        return;
                    }
                }
                else
                {
                    // Get the sound definition for known sound types
                    soundDefinition = soundSet[soundType];
                    if (soundDefinition == null)
                    {
                        Main.DebugLog(() => $"AudioClipPortReaderPatch: No sound definition found for {soundType}");
                        return;
                    }
                }

                if (soundDefinition.pitch.HasValue) ___pitch = soundDefinition.pitch.Value;
                if (soundDefinition.maxVolume.HasValue) ___volume = soundDefinition.maxVolume.Value;

            }
            catch (System.Exception ex)
            {
                Main.mod?.Logger.Error($"AudioClipPortReaderPatch error: {ex.Message}");
            }
        }

        // Try to match a generic sound by clip name
        private static SoundDefinition? TryGetGenericSoundDefinition(AudioClipPortReader portReader, TrainCar trainCar, SoundSet soundSet)
        {
            if (portReader.clips == null || portReader.clips.Length == 0)
                return null;

            // Get the first clip name
            var clipName = portReader.clips[0].name;

            // Check if there's a generic sound mapping for this clip name
            var genericSounds = Main.discoveryService?.GetGenericSoundNames(trainCar.carType);
            if (genericSounds != null && genericSounds.Contains(clipName))
            {
                // Try to find a custom sound definition for this generic sound
                var soundDef = soundSet.GetGenericSound(clipName);
                if (soundDef != null)
                {
                    Main.DebugLog(() => $"AudioClipPortReaderPatch: Found custom generic sound definition for '{clipName}'");
                    return soundDef;
                }

                Main.DebugLog(() => $"AudioClipPortReaderPatch: Generic sound '{clipName}' found but no custom definition applied, using defaults");
            }

            return null;
        }

        // Attempts to determine which SoundType this AudioClipPortReader represents
        // based on the clips it contains and the car type
        private static SoundType DetermineSoundType(AudioClipPortReader portReader, TrainCarType carType)
        {
            // First try to match using new discoveryService
            if (Main.discoveryService != null)
            {
                var trainCar = portReader.GetComponentInParent<TrainCar>();
                if (trainCar != null)
                {
                    // Check each possible audio clip sound type to see if this portReader matches
                    foreach (var soundType in SoundTypes.audioClipsSoundTypes)
                    {
                        var mappedPortReader = Main.discoveryService.GetAudioClipPortReader(trainCar, soundType);
                        if (mappedPortReader == portReader)
                        {
                            return soundType;
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
