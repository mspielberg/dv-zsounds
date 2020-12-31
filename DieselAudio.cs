using HarmonyLib;
using UnityEngine;

namespace DvMod.ZSounds
{
    public static class DieselAudio
    {
        private static float originalMinPitch;
        private static float originalMaxPitch;

        public static void SetEngineClip(LayeredAudio engineAudio)
        {
            AudioUtils.SetClip(
                "DE6 engine loop",
                engineAudio,
                Main.settings.dieselEngineSound,
                enabled: true,
                Main.settings.dieselEnginePitch);

            if (Main.settings.dieselEngineSound == null)
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

        private static void SetHornHit(LayeredAudio hornAudio)
        {
            var source = hornAudio.transform.Find("train_horn_01_hit").GetComponent<AudioSource>();
            var clip = source.clip;
            AudioUtils.SetClip(
                "DE6 horn hit",
                ref clip,
                Main.settings.dieselHornHitSound,
                Main.settings.dieselHornHitEnabled);
            source.clip = clip;
        }

        public static void ResetAudio(LocoAudioDiesel __instance)
        {
            var engineAudio = __instance.engineAudio;
            if (originalMinPitch == default)
            {
                originalMinPitch = engineAudio.minPitch;
                originalMaxPitch = engineAudio.maxPitch;
            }

            AudioUtils.SetClip(
                "DE6 startup",
                ref __instance.engineOnClip,
                Main.settings.dieselStartupSound,
                Main.settings.dieselStartupEnabled);

            SetEngineClip(engineAudio);

            AudioUtils.SetClip(
                "DE6 shutdown",
                ref __instance.engineOffClip,
                Main.settings.dieselShutdownSound,
                Main.settings.dieselShutdownEnabled);

            SetHornHit(__instance.hornAudio);
            AudioUtils.SetClip(
                "DE6 horn loop",
                __instance.hornAudio,
                Main.settings.dieselHornLoopSound,
                Main.settings.dieselHornLoopEnabled,
                Main.settings.dieselHornPitch);
        }

        public static void ResetAllAudio()
        {
            foreach (var audio in Component.FindObjectsOfType<LocoAudioDiesel>())
                ResetAudio(audio);
        }

        [HarmonyPatch(typeof(LocoAudioDiesel), nameof(LocoAudioDiesel.SetupForCar))]
        public static class SetupForCarPatch
        {
            public static void Postfix(LocoAudioDiesel __instance)
            {
                ResetAudio(__instance);
            }
        }
    }
}