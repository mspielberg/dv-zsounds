using System;
using HarmonyLib;

namespace DvMod.ZSounds
{
    public static class SpawnPatches
    {
        public static void ApplyAudio(TrainAudio trainAudio)
        {
            var car = trainAudio.car;
            
            // Apply custom sounds
            var soundSet = Registry.Get(car);
            Main.DebugLog(() => $"Applying sounds for {car.ID}");
            AudioUtils.Apply(trainAudio, soundSet);
        }

        public static void ApplyAudio(TrainCar car)
        {
            // Apply custom sounds
            var soundSet = Registry.Get(car);
            Main.DebugLog(() => $"Applying sounds for {car.ID}");
            AudioUtils.Apply(car, soundSet);
        }

        [HarmonyPatch(typeof(TrainAudio), nameof(TrainAudio.SetupForCar))]
        public static class SetupForCarPatch
        {
            public static void Postfix(TrainAudio __instance)
            {
                try 
                {
                    ApplyAudio(__instance);
                }
                catch (Exception ex)
                {
                    Main.mod?.Logger.Error($"Failed to apply audio to car {__instance.car?.ID}: {ex.Message}");
                }
            }
        }
    }
}