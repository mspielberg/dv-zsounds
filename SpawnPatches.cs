using HarmonyLib;

namespace DvMod.ZSounds
{
    public static class SpawnPatches
    {
        public static void ApplyAudio(TrainAudio trainAudio)
        {
            var car = trainAudio.car;
            var soundSet = Registry.Get(car);
            Main.DebugLog(() => $"Applying sounds for {car.ID}");
            AudioUtils.Apply(trainAudio, soundSet);
        }

        [HarmonyPatch(typeof(TrainAudio), nameof(TrainAudio.SetupForCar))]
        public static class SetupForCarPatch
        {
            public static void Postfix(TrainAudio __instance)
            {
                __instance.car.LogicCarInitialized += () => ApplyAudio(__instance);
            }
        }
    }
}