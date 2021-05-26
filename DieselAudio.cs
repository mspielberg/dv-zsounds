using DV.CabControls;
using DvMod.ZSounds.Config;
using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace DvMod.ZSounds
{
    public static class DieselAudio
    {
        public static void Apply(TrainCar car, SoundSet soundSet)
        {
            var audio = car.GetComponentInChildren<LocoAudioDiesel>();
            SetBell(audio, soundSet);
            SetEngine(audio, soundSet);
            SetHorn(audio, soundSet);
        }

        public static void SetEngine(LocoAudioDiesel audio, SoundSet soundSet)
        {
            soundSet.sounds.TryGetValue(SoundType.EngineStartup, out var startup);
            AudioUtils.Apply(startup, "DE6 engine startup", ref audio.engineOnClip);
            soundSet.sounds.TryGetValue(SoundType.EngineShutdown, out var shutdown);
            AudioUtils.Apply(shutdown, "DE6 engine shutdown", ref audio.engineOffClip);
            EngineFade.SetFadeSettings(audio, new EngineFade.Settings
            {
                fadeInStart = startup?.fadeStart ?? 0.15f * audio.engineOnClip.length,
                fadeOutStart = shutdown?.fadeStart ?? 0.10f * audio.engineOffClip.length,
                fadeInDuration = startup?.fadeDuration ?? 2f,
                fadeOutDuration = shutdown?.fadeDuration ?? 1f,
            });

            AudioUtils.Apply(soundSet[SoundType.EngineLoop], "DE6 engine loop", audio.engineAudio);
            AudioUtils.Apply(soundSet[SoundType.EngineLoadLoop], "DE6 engine load loop", audio.enginePistonAudio);
            AudioUtils.Apply(soundSet[SoundType.TractionMotors], "DE6 traction motor loop", audio.electricMotorAudio);
        }

        private static void SetBell(LocoAudioDiesel audio, SoundSet soundSet)
        {
            var audioSource = audio.transform.Find("Horn/ZSounds bell").GetComponent<AudioSource>();
            AudioUtils.Apply(soundSet[SoundType.Bell], "DE6 bell", audioSource);
        }

        private static void SetHorn(LocoAudioDiesel audio, SoundSet soundSet)
        {
            var hornHitSource = audio.hornAudio.transform.Find("train_horn_01_hit").GetComponent<AudioSource>();
            AudioUtils.Apply(soundSet[SoundType.HornHit], "DE6 horn hit", hornHitSource);
            AudioUtils.Apply(soundSet[SoundType.HornLoop], "DE6 horn loop", audio.hornAudio);
        }

        [HarmonyPatch(typeof(LocoAudioDiesel), nameof(LocoAudioDiesel.SetupForCar))]
        public static class SetupForCarPatch
        {
            public static void Postfix(LocoAudioDiesel __instance)
            {
                SetupBell(__instance);
            }

            private static void SetupBell(LocoAudioDiesel __instance)
            {
                var originalHornLoop =
                    __instance.transform.Find("Horn/LocoDiesel_Horn_Layered/train_horn_01_loop").GetComponent<AudioSource>();
                var bellSource = GameObject.Instantiate(originalHornLoop, __instance.transform.Find("Horn"));
                bellSource.name = "ZSounds bell";
                bellSource.loop = false;
                bellSource.volume = 1f;
                bellSource.spatialBlend = 1f;
            }
        }
    }

    public static class DieselBell
    {
        [HarmonyPatch(typeof(DieselDashboardControls), nameof(DieselDashboardControls.OnEnable))]
        public static class InitPatch
        {
            public static void Postfix(DieselDashboardControls __instance)
            {
                __instance.StartCoroutine(SetupBellLamp(__instance));
            }

            private static IEnumerator SetupBellLamp(DieselDashboardControls __instance)
            {
                AudioSource bellAudioSource;
                do
                {
                    yield return null;
                    bellAudioSource = TrainCar.Resolve(__instance.gameObject).transform.Find("AudioDiesel(Clone)/Horn/ZSounds bell").GetComponent<AudioSource>();
                }
                while (bellAudioSource == null);

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