using System;
using HarmonyLib;

namespace DvMod.ZSounds
{
    public static class SpawnPatches
    {
        // Manually applies audio to a train car using the registry system
        public static void ApplyAudio(TrainAudio trainAudio)
        {
            var car = trainAudio.car;
            ApplyAudio(car);
        }

        // Manually applies audio to a train car using the registry system
        public static void ApplyAudio(TrainCar car)
        {
            Main.DebugLog(() => $"Manually applying sounds for {car.ID}");
            
            // Use registry system
            var soundSet = Registry.Get(car);
            AudioUtils.Apply(car, soundSet);
            Main.DebugLog(() => $"Applied sounds for {car.ID}");
        }

        [HarmonyPatch(typeof(TrainAudio), nameof(TrainAudio.SetupForCar))]
        public static class SetupForCarPatch
        {
            public static void Postfix(TrainAudio __instance)
            {
                // No automatic sound changes - sounds applied manually via CommsRadio
                Main.DebugLog(() => $"TrainAudio setup completed for car {__instance.car?.ID} - no automatic sound changes applied");
            }
        }
    }
}