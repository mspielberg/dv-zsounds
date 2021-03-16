using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace DvMod.ZSounds
{
    public static class ShunterAudio
    {
        private static float originalMinPitch;
        private static float originalMaxPitch;
        private static AnimationCurve? originalVolumeCurve;

        public static void SetEngineClip(LayeredAudio engineAudio)
        {
            AudioUtils.SetClip(
                "DE2 engine loop",
                engineAudio,
                Main.settings.shunterEngineSound,
                enabled: true,
                Main.settings.shunterEnginePitch);

            if (Main.settings.shunterEngineSound == null)
            {
                engineAudio.minPitch = originalMinPitch;
                engineAudio.maxPitch = originalMaxPitch;
                engineAudio.layers[0].volumeCurve = originalVolumeCurve;
            }
            else
            {
                engineAudio.minPitch = 1f;
                engineAudio.maxPitch = 2100/1250f;
                engineAudio.layers[0].volumeCurve = AnimationCurve.EaseInOut(0, 0.1f, 1, 0.4f);
            }
        }

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
            var engineAudio = __instance.engineAudio;
            if (originalMinPitch == default)
            {
                originalMinPitch = engineAudio.minPitch;
                originalMaxPitch = engineAudio.maxPitch;
                originalVolumeCurve = engineAudio.layers[0].volumeCurve;
            }

            AudioUtils.SetClip(
                "DE2 startup",
                ref __instance.engineOnClip,
                Main.settings.shunterStartupSound,
                Main.settings.shunterStartupEnabled);
            SetEngineClip(engineAudio);
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

        [HarmonyPatch(typeof(LocoAudioShunter), nameof(LocoAudioShunter.EngineAudioHandle))]
        public static class EngineAudioHandlePatch
        {
            public static bool Prefix(bool engineTurnedOn, LocoAudioShunter __instance, ref IEnumerator __result)
            {
                __result = EngineAudioHandle(__instance, engineTurnedOn);
                return false;
            }

            private static IEnumerator EngineAudioHandle(LocoAudioShunter __instance, bool engineTurnedOn)
            {
                __instance.IsEngineVolumeFadeActive = true;
                if (engineTurnedOn)
                {
                    __instance.engineOnClip.Play(__instance.playEngineAt.position, 1f, 1f, 0f, 1f, 500f, default(AudioSourceCurves), null, __instance.playEngineAt);
                }
                else
                {
                    __instance.engineOffClip.Play(__instance.playEngineAt.position, 1f, 1f, 0f, 1f, 500f, default(AudioSourceCurves), null, __instance.playEngineAt);
                }
                yield return WaitFor.Seconds(engineTurnedOn ? Main.settings.shunterFadeInStart : Main.settings.shunterFadeOutStart);
                float startTime = Time.realtimeSinceStartup;
                float duration = (engineTurnedOn ? Main.settings.shunterFadeInDuration : Main.settings.shunterFadeOutDuration);
                float startEngVol = __instance.engineAudio.masterVolume;
                float startEngPistonVol = __instance.enginePistonAudio.masterVolume;
                float startElectricMotorVol = __instance.electricMotorAudio.masterVolume;
                int endVolume = (engineTurnedOn ? 1 : 0);
                float endEnginePistonVolume = (engineTurnedOn ? __instance.neutralEnginePistonVolume : 0f);
                while (true)
                {
                    float num = (Time.realtimeSinceStartup - startTime) / duration;
                    __instance.engineAudio.masterVolume = Mathf.Lerp(startEngVol, endVolume, num);
                    __instance.enginePistonAudio.masterVolume = Mathf.Lerp(startEngPistonVol, endEnginePistonVolume, num);
                    __instance.electricMotorAudio.masterVolume = Mathf.Lerp(startElectricMotorVol, endVolume, num);
                    if (num >= 1f)
                    {
                        break;
                    }
                    yield return null;
                }
                __instance.IsEngineVolumeFadeActive = false;
                __instance.engineAudioCoro = null;
            }
        }
    }
}