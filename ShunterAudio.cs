using HarmonyLib;
using UnityEngine;

namespace DvMod.ZSounds
{
    public static class ShunterAudio
    {
        private static AudioClip? originalEngineOnClip;
        private static AudioClip? originalEngineOffClip;
        private static AudioClip? originalHornHitClip;

        public static void SetEngineClip(LayeredAudio engineAudio, string? name, float startPitch)
        {
            LayeredAudioUtils.SetClip(engineAudio, name, startPitch);
        }

        private static AudioSource GetHornHitSource(LayeredAudio hornAudio)
        {
            return hornAudio.transform.Find("train_horn_01_hit").GetComponent<AudioSource>();
        }

        private static void SetHornHit(LayeredAudio hornAudio, string? name)
        {
            if (name == null)
                GetHornHitSource(hornAudio).clip = originalHornHitClip;
            else
                GetHornHitSource(hornAudio).clip = FileAudio.Load(name);
        }

        public static void ResetAudio(LocoAudioShunter __instance)
        {
            var engineAudio = __instance.engineAudio;
            if (originalEngineOnClip == default)
            {
                originalEngineOnClip = __instance.engineOnClip;
                originalEngineOffClip = __instance.engineOffClip;
                originalHornHitClip = GetHornHitSource(__instance.hornAudio).clip;
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

            SetHornHit(__instance.hornAudio, Main.settings.dieselHornHitSound);
            LayeredAudioUtils.SetClip(__instance.hornAudio, Main.settings.shunterHornLoopSound, Main.settings.shunterHornPitch);
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