using System;
using HarmonyLib;

namespace DvMod.ZSounds
{
    public static class SpawnPatches
    {
        /// <summary>
        /// Manually applies audio to a train car. Only used when explicitly called via CommsRadio.
        /// </summary>
        public static void ApplyAudio(TrainAudio trainAudio)
        {
            var car = trainAudio.car;
            
            // Get the current sound set (which may have been manually configured)
            var soundSet = Registry.Get(car);
            Main.DebugLog(() => $"Manually applying sounds for {car.ID}");
            AudioUtils.Apply(trainAudio, soundSet);
        }

        /// <summary>
        /// Manually applies audio to a train car. Only used when explicitly called via CommsRadio.
        /// </summary>
        public static void ApplyAudio(TrainCar car)
        {
            // Get the current sound set (which may have been manually configured)
            var soundSet = Registry.Get(car);
            Main.DebugLog(() => $"Manually applying sounds for {car.ID}");
            AudioUtils.Apply(car, soundSet);
        }

        [HarmonyPatch(typeof(TrainAudio), nameof(TrainAudio.SetupForCar))]
        public static class SetupForCarPatch
        {
            public static void Postfix(TrainAudio __instance)
            {
                // No automatic sound application - sounds are only applied when manually selected via CommsRadio
                Main.DebugLog(() => $"TrainAudio setup completed for car {__instance.car?.ID} - no automatic sound changes applied");
            }
        }
    }
}