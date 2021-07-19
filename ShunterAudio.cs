using System.Collections;
using DV.CabControls;
using DV.CabControls.Spec;
using DvMod.ZSounds.Config;
using HarmonyLib;
using UnityEngine;

namespace DvMod.ZSounds
{
    public static class ShunterAudio
    {
        public static void Apply(TrainCar car, SoundSet soundSet)
        {
            var audio = car.GetComponentInChildren<LocoAudioShunter>();
            SetBell(audio, soundSet);
            SetEngine(audio, soundSet);
            SetHorn(audio, soundSet);
        }

        private static void SetBell(LocoAudioShunter audio, SoundSet soundSet)
        {
            var audioSource = audio.transform.Find("Horn/ZSounds bell").GetComponent<AudioSource>();
            AudioUtils.Apply(soundSet[SoundType.Bell], "DE2 bell", audioSource);
        }

        private static void SetEngine(LocoAudioShunter audio, SoundSet soundSet)
        {
            soundSet.sounds.TryGetValue(SoundType.EngineStartup, out var startup);
            AudioUtils.Apply(startup, "DE2 engine startup", ref audio.engineOnClip);
            soundSet.sounds.TryGetValue(SoundType.EngineShutdown, out var shutdown);
            AudioUtils.Apply(shutdown, "DE2 engine shutdown", ref audio.engineOffClip);
            EngineFade.SetFadeSettings(audio, new EngineFade.Settings
            {
                fadeInStart = startup?.fadeStart ?? 0.15f * audio.engineOnClip.length,
                fadeOutStart = shutdown?.fadeStart ?? 0.10f * audio.engineOffClip.length,
                fadeInDuration = startup?.fadeDuration ?? 2f,
                fadeOutDuration = shutdown?.fadeDuration ?? 1f,
            });

            AudioUtils.Apply(soundSet[SoundType.EngineLoop], "DE2 engine loop", audio.engineAudio);
            AudioUtils.Apply(soundSet[SoundType.EngineLoadLoop], "DE2 engine load loop", audio.enginePistonAudio);
            AudioUtils.Apply(soundSet[SoundType.TractionMotors], "DE2 traction motor loop", audio.electricMotorAudio);
        }

        private static void SetHorn(LocoAudioShunter audio, SoundSet soundSet)
        {
            var hornHitSource = audio.hornAudio.transform.Find("train_horn_01_hit").GetComponent<AudioSource>();
            AudioUtils.Apply(soundSet[SoundType.HornHit], "DE2 horn hit", hornHitSource);
            AudioUtils.Apply(soundSet[SoundType.HornLoop], "DE2 horn loop", audio.hornAudio);
        }
    }

    public static class ShunterBell
    {
        [HarmonyPatch(typeof(ShunterDashboardControls), nameof(ShunterDashboardControls.OnEnable))]
        public static class InitPatch
        {
            public static void Postfix(ShunterDashboardControls __instance)
            {
                __instance.StartCoroutine(SetupBellLamp(__instance));
            }

            private static IEnumerator SetupBellLamp(ShunterDashboardControls __instance)
            {
                AudioSource bellAudioSource;
                ToggleSwitchBase bellSwitch;
                do
                {
                    yield return null;
                    bellAudioSource = TrainCar.Resolve(__instance.gameObject).transform.Find("AudioShunter(Clone)/Horn/ZSounds bell").GetComponent<AudioSource>();
                    bellSwitch = __instance.transform.Find("C dashboard buttons controller/C bell switch").GetComponent<ToggleSwitchBase>();
                }
                while (bellAudioSource == null || bellSwitch == null);

                bellSwitch.SetValue(bellAudioSource.loop ? 1 : 0);

                var bellLampControl = __instance.transform.Find("C dashboard buttons controller/I bell lamp").GetComponent<LampControl>();
                bellLampControl.lampInd = __instance.transform.Find("C dashboard buttons controller/I bell lamp/lamp emmision indicator").GetComponent<IndicatorEmission>();
                bellLampControl.SetLampState(bellAudioSource.loop ? LampControl.LampState.On : LampControl.LampState.Off);

                bellSwitch.ValueChanged += (ValueChangedEventArgs e) => {
                    bellLampControl.SetLampState(e.newValue >= 0.5f ? LampControl.LampState.On : LampControl.LampState.Off);
                    bellAudioSource.loop = e.newValue >= 0.5f;
                    if (bellAudioSource.loop && !bellAudioSource.isPlaying)
                        bellAudioSource.Play();
                };
            }
        }
        [HarmonyPatch(typeof(LocoAudioShunter), nameof(LocoAudioDiesel.SetupForCar))]
        public static class SetupForCarPatch
        {
            public static void Postfix(LocoAudioShunter __instance)
            {
                SetupBell(__instance);
            }

            private static void SetupBell(LocoAudioShunter __instance)
            {
                var originalHornLoop =
                    __instance.transform.Find("Horn/Horn_Layered/train_horn_01_loop").GetComponent<AudioSource>();
                var bellSource = GameObject.Instantiate(originalHornLoop, __instance.transform.Find("Horn"));
                bellSource.name = "ZSounds bell";
                bellSource.loop = false;
                bellSource.volume = 1f;
                bellSource.spatialBlend = 1f;
            }
        }

        [HarmonyPatch(typeof(TrainCar), nameof(TrainCar.LoadInterior))]
        public static class GetCarPrefabPatch
        {
            public static void Postfix(TrainCar __instance)
            {
                if (__instance.carType != TrainCarType.LocoShunter)
                    return;
                __instance.StartCoroutine(CreateBellControlCoro(__instance));
            }

            private static IEnumerator CreateBellControlCoro(TrainCar __instance)
            {
                var buttonsController = __instance.loadedInterior.transform.Find("C dashboard buttons controller");

                // clone interaction area
                var bellIA = buttonsController.Find("IA bell switch")?.gameObject;
                if (bellIA == null)
                {
                    var fanIA = buttonsController.Find("IA fan switch");
                    bellIA = GameObject.Instantiate(fanIA.gameObject, fanIA.parent);
                    bellIA.name = "IA bell switch";
                    var localPosition = bellIA.transform.localPosition;
                    bellIA.transform.localPosition = new Vector3(0.41f, localPosition.y, localPosition.z);
                }

                // clone lamp
                var bellLamp = buttonsController.Find("I bell lamp")?.gameObject;
                if (bellLamp == null)
                {
                    var fanLamp = buttonsController.Find("I fan_lamp");
                    bellLamp = GameObject.Instantiate(fanLamp.gameObject, fanLamp.parent);
                    bellLamp.name = "I bell lamp";
                    var localPosition = bellLamp.transform.localPosition;
                    bellLamp.transform.localPosition = new Vector3(0.41f, localPosition.y, localPosition.z);
                }

                // clone switch
                var bellSwitch = buttonsController.Find("C bell switch")?.gameObject;
                if (bellSwitch == null)
                {
                    var fanSwitch = buttonsController.Find("C fan switch").gameObject;
                    // ensure the Spec doesn't try to create duplicate components when cloned
                    fanSwitch.SetActive(false);
                    bellSwitch = GameObject.Instantiate(fanSwitch.gameObject, fanSwitch.transform.parent);
                    fanSwitch.SetActive(true);

                    bellSwitch.name = "C bell switch";
                    var localPosition = bellSwitch.transform.localPosition;
                    bellSwitch.transform.localPosition = new Vector3(0.41f, localPosition.y, localPosition.z);

                    var toggleSwitchSpec = bellSwitch.GetComponent<ToggleSwitch>();
                    toggleSwitchSpec.nonVrStaticInteractionArea = bellIA.GetComponent<StaticInteractionArea>();

                    foreach (var comp in bellSwitch.GetComponents<Component>())
                    {
                        switch (comp)
                        {
                        case Transform _:
                        case ControlSpec _:
                            break;
                        default:
                            // these will be recreated when the GameObject is set active next frame
                            Component.Destroy(comp);
                            break;
                        }
                    }

                    yield return null;

                    bellSwitch.SetActive(true);
                }
            }
        }
    }
}