using HarmonyLib;
using UnityModManagerNet;

namespace DvMod.ZSounds
{
    public static class SpawnPatches
    {
        public static void ApplyAudio(TrainCar car)
        {
            var soundSet = Registry.Get(car);
            Main.DebugLog(() => $"Applying sounds for {car.ID}");
            switch (car.carType)
            {
                case TrainCarType.LocoDiesel:
                    DieselAudio.Apply(car, soundSet);
                    break;
                case TrainCarType.LocoShunter:
                    ShunterAudio.Apply(car, soundSet);
                    break;
                case TrainCarType.LocoSteamHeavy:
                    SteamAudio.Apply(car, soundSet);
                    break;
                default:
                    if (UnityModManager.FindMod("DVCustomCarLoader").Loaded)
                        CCLAudio.Apply(car, soundSet);
                    break;
            }
        }

        [HarmonyPatch(typeof(TrainCar), nameof(TrainCar.InitAudio))]
        public static class InitAudioPatch
        {
            public static void Postfix(TrainCar __instance)
            {
                if (CarTypes.IsLocomotive(__instance.carType))
                    ApplyAudio(__instance);
            }
        }
    }
}