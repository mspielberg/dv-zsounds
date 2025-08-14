using System;

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

        // Intentionally no automatic patching of TrainAudio.SetupForCar.
    }
}