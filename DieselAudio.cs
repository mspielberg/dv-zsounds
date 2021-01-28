using DV.CabControls;
using HarmonyLib;
using System.Collections;
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
                engineAudio.minPitch = 1f;
                engineAudio.maxPitch = 825f / 275f;
            }
        }

        private static void SetBell(LocoAudioDiesel audio)
        {
            var audioSource = audio.transform.Find("Horn/ZSounds bell").GetComponent<AudioSource>();
            if (Main.settings.dieselBellSound == null)
            {
                audioSource.Stop();
                audioSource.volume = 0;
            }
            else
            {
                audioSource.clip = FileAudio.Load(Main.settings.dieselBellSound);
                audioSource.pitch = Main.settings.dieselBellPitch;
                audioSource.volume = 1;
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

            SetBell(__instance);

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
                SetupBell(__instance);
                ResetAudio(__instance);
            }

            private static void SetupBell(LocoAudioDiesel __instance)
            {
                var originalHornLoop = __instance.transform.Find("Horn/LocoDiesel_Horn_Layered/train_horn_01_loop").GetComponent<AudioSource>();
                var bellSource = GameObject.Instantiate(originalHornLoop, __instance.transform.Find("Horn"));
                bellSource.name = "ZSounds bell";
                bellSource.loop = false;
            }
        }

        [HarmonyPatch(typeof(LocoAudioDiesel), nameof(LocoAudioDiesel.EngineAudioHandle))]
        public static class EngineAudioHandlePatch
        {
            public static bool Prefix(bool engineTurnedOn, LocoAudioDiesel __instance, ref IEnumerator __result)
            {
                __result = EngineAudioHandle(__instance, engineTurnedOn);
                return false;
            }

            private static IEnumerator EngineAudioHandle(LocoAudioDiesel __instance, bool engineTurnedOn)
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
                yield return WaitFor.Seconds(engineTurnedOn ? Main.settings.dieselFadeInStart : Main.settings.dieselFadeOutStart);
                float startTime = Time.realtimeSinceStartup;
                float duration = (engineTurnedOn ? Main.settings.dieselFadeInDuration : Main.settings.dieselFadeOutDuration);
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

    public static class DieselBell
    {
        [HarmonyPatch(typeof(DieselDashboardControls), nameof(DieselDashboardControls.Init))]
        public static class InitPatch
        {
            public static void Postfix(DieselDashboardControls __instance)
            {
                __instance.StartCoroutine(SetupBellLamp(__instance));
            }

            private static IEnumerator SetupBellLamp(DieselDashboardControls __instance)
            {
                yield return null;

                var bellAudioSource = TrainCar.Resolve(__instance.gameObject).transform.Find("AudioDiesel(Clone)/Horn/ZSounds bell").GetComponent<AudioSource>();

                var bellButton = __instance.transform.Find("offset/C bell button").GetComponent<ButtonBase>();
                bellButton.SetValue(bellAudioSource.loop ? 1 : 0);

                var bellLampControl = __instance.transform.Find("offset/I Indicator lamps/I bell_lamp").GetComponent<LampControl>();
                bellLampControl.lampInd = __instance.transform.Find("offset/I Indicator lamps/I bell_lamp/lamp emmision indicator").GetComponent<IndicatorEmission>();
                bellLampControl.SetLampState(bellAudioSource.loop ? LampControl.LampState.On : LampControl.LampState.Off);

                bellButton.ValueChanged += (ValueChangedEventArgs e) => {
                    bellLampControl.SetLampState(e.newValue >= 0.5f ? LampControl.LampState.On : LampControl.LampState.Off);
                    bellAudioSource.loop = e.newValue >= 0.5f;
                    if (bellAudioSource.loop && !bellAudioSource.isPlaying)
                        bellAudioSource.Play();
                };
            }
        }
    }
}