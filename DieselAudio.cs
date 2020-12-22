using DV.Logic.Job;
using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace DvMod.ZSounds
{
    public static class DieselAudio
    {
        private static AudioClip? originalClip = null;
        private static float originalMinPitch = 0;
        private static float originalMaxPitch = 0;

        private static void ResetToDefault(LayeredAudio engineAudio)
        {
            if (originalClip != null)
                engineAudio.layers[0].source.clip = originalClip;
            for (int i = 1; i < engineAudio.layers.Length; i++)
                engineAudio.layers[i].source.mute = false;
            engineAudio.minPitch = originalMinPitch;
            engineAudio.maxPitch = originalMaxPitch;
        }

        public static void SetEngineClip(LayeredAudio engineAudio, string name)
        {
            engineAudio.layers[0].source.clip = FileAudio.Load(name);
            for (int i = 1; i < engineAudio.layers.Length; i++)
                engineAudio.layers[i].source.mute = true;
            engineAudio.minPitch = 275f / 575f;
            engineAudio.maxPitch = 825f / 575f;
        }

        public static void SetStartPitch(LayeredAudio audio, float startPitch)
        {
            foreach (var layer in audio.layers)
                layer.startPitch = startPitch;
        }

        public static void ResetAudio(LocoAudioDiesel __instance)
        {
            var engineAudio = __instance.engineAudio;
            if (originalClip == null)
            {
                originalClip = engineAudio.layers[0].source.clip;
                originalMinPitch = engineAudio.minPitch;
                originalMaxPitch = engineAudio.maxPitch;
            }

            if (Main.settings.dieselEngineSound == null)
                ResetToDefault(engineAudio);
            else
                SetEngineClip(engineAudio, Main.settings.dieselEngineSound);
            SetStartPitch(engineAudio, Main.settings.dieselEnginePitch);
        }

        public static void ResetAllAudio()
        {
            foreach (var car in SingletonBehaviour<IdGenerator>.Instance.logicCarToTrainCar.Values.Where(car => car.carType == TrainCarType.LocoDiesel))
                ResetAudio(car.GetComponent<LocoAudioDiesel>());
        }
    }

    [HarmonyPatch(typeof(LocoAudioDiesel), nameof(LocoAudioDiesel.SetupForCar))]
    public static class SetupForCarPatch
    {
        public static void Postfix(LocoAudioDiesel __instance)
        {
            DieselAudio.ResetAudio(__instance);
        }
    }
}