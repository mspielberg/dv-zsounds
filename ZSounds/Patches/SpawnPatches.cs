namespace DvMod.ZSounds.Patches
{
    public static class SpawnPatches
    {
        // Manually applies audio to a train car using the registry system
        public static void ApplyAudio(TrainCar car)
        {
            Main.DebugLog(() => $"Manually applying sounds for {car.ID}");

            // Use new service architecture
            if (Main.registryService != null && Main.applicatorService != null)
            {
                var soundSet = Main.registryService.GetSoundSet(car);
                Main.applicatorService.ApplySoundSet(car, soundSet);
                Main.DebugLog(() => $"Applied sounds for {car.ID} using new services");
            }
        }
    }
}
