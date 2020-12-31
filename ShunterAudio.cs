using HarmonyLib;
using UnityEngine;

namespace DvMod.ZSounds
{
    public static class ShunterAudio
    {
        private static AudioSource GetHornHitSource(LayeredAudio hornAudio)
        {
            return hornAudio.transform.Find("train_horn_01_hit").GetComponent<AudioSource>();
        }

        private static void SetHornHit(LayeredAudio hornAudio)
        {
            var source = GetHornHitSource(hornAudio);
            var clip = source.clip;
            AudioUtils.SetClip(
                "DE2 horn hit",
                ref clip,
                Main.settings.shunterHornHitSound,
                Main.settings.shunterHornHitEnabled);
            source.clip = clip;
        }

        public static void ResetAudio(LocoAudioShunter __instance)
        {
            AudioUtils.SetClip(
                "DE2 startup",
                ref __instance.engineOnClip,
                Main.settings.shunterStartupSound,
                Main.settings.shunterStartupEnabled);
            AudioUtils.SetClip(
                "DE2 engine loop",
                __instance.engineAudio,
                Main.settings.shunterEngineSound,
                enabled: true,
                startPitch: Main.settings.shunterEnginePitch);
            AudioUtils.SetClip(
                "DE2 shutdown",
                ref __instance.engineOffClip,
                Main.settings.shunterShutdownSound,
                Main.settings.shunterShutdownEnabled);
            SetHornHit(__instance.hornAudio);
            AudioUtils.SetClip(
                "DE2 horn loop",
                __instance.hornAudio,
                Main.settings.shunterHornLoopSound,
                Main.settings.shunterHornLoopEnabled,
                Main.settings.shunterHornPitch);
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