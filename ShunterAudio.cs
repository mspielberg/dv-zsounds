using HarmonyLib;
using UnityEngine;

namespace DvMod.ZSounds
{
    public static class ShunterAudio
    {
        private static float originalMinPitch;
        private static float originalMaxPitch;
        private static AudioClip? originalEngineOnClip;
        private static AudioClip? originalEngineOffClip;

        public static void SetEngineClip(LayeredAudio engineAudio, string? name, float startPitch)
        {
            LayeredAudioUtils.SetClip(engineAudio, name, startPitch);
            if (name == null)
            {
                engineAudio.minPitch = originalMinPitch;
                engineAudio.maxPitch = originalMaxPitch;
            }
            else
            {
                engineAudio.minPitch = 275f / 575f;
                engineAudio.maxPitch = 825f / 575f;
            }
        }

        public static void ResetAudio(LocoAudioShunter __instance)
        {
            var engineAudio = __instance.engineAudio;
            if (originalMinPitch == default)
            {
                originalMinPitch = engineAudio.minPitch;
                originalMaxPitch = engineAudio.maxPitch;
                originalEngineOnClip = __instance.engineOnClip;
                originalEngineOffClip = __instance.engineOffClip;
            }

            if (Main.settings.shunterStartupSound != null)
                __instance.engineOnClip = FileAudio.Load(Main.settings.shunterStartupSound);
            else
                __instance.engineOnClip = originalEngineOnClip;

            SetEngineClip(engineAudio, Main.settings.shunterEngineSound, Main.settings.shunterEnginePitch);

            if (Main.settings.shunterShutdownSound != null)
                __instance.engineOffClip = FileAudio.Load(Main.settings.shunterShutdownSound);
            else
                __instance.engineOffClip = originalEngineOffClip;
        }

        public static void ResetAllAudio()
        {
            foreach (var audio in Component.FindObjectsOfType<LocoAudioShunter>())
                ResetAudio(audio);
        }

        [HarmonyPatch(typeof(LocoAudioShunter), nameof(LocoAudioShunter.SetupForCar))]
        public static class SetupForCarPatch
        {
            public static void Postfix(LocoAudioShunter __instance)
            {
                ResetAudio(__instance);
            }
        }
    }
}