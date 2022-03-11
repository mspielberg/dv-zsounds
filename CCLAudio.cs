using DVCustomCarLoader.LocoComponents;
using DVCustomCarLoader.LocoComponents.DieselElectric;
using DVCustomCarLoader.LocoComponents.Steam;
using DvMod.ZSounds.Config;
using UnityEngine;

namespace DvMod.ZSounds
{
    public class CCLAudio
    {
        public static void Apply(TrainCar car, SoundSet soundSet)
        {
            Main.DebugLog(() => $"Applying to {car.ID}:\n{soundSet}");
            var audio = car.GetComponentInChildren<CustomLocoAudio>();
            if (audio == null)
                return;
                Main.DebugLog(() => $"Found CustomLocoAudio {audio.GetPath()}");
            if (audio is CustomLocoAudioDiesel diesel)
                CCLDieselAudio.Apply(car, diesel, soundSet);
            if (audio is CustomLocoAudioSteam steam)
                CCLSteamAudio.Apply(car, steam, soundSet);
        }
    }

    public class CCLDieselAudio
    {
        public static void Apply(TrainCar car, CustomLocoAudioDiesel audio, SoundSet soundSet)
        {
            Main.DebugLog(() => $"Applying to {car.ID}:\n{soundSet}");
            SetEngine(car.carType, audio, soundSet);
            SetHorn(car.carType, audio, soundSet);
        }

        private static void SetEngine(TrainCarType carType, CustomLocoAudioDiesel audio, SoundSet soundSet)
        {
            soundSet.sounds.TryGetValue(SoundType.EngineStartup, out var startup);
            AudioUtils.Apply(carType, SoundType.EngineStartup, startup, ref audio.engineOnClip);
            soundSet.sounds.TryGetValue(SoundType.EngineShutdown, out var shutdown);
            AudioUtils.Apply(carType, SoundType.EngineShutdown, shutdown, ref audio.engineOffClip);
            EngineFade.SetFadeSettings(audio, new EngineFade.Settings
            {
                fadeInStart = startup?.fadeStart ?? 0.15f * audio.engineOnClip.length,
                fadeOutStart = shutdown?.fadeStart ?? 0.10f * audio.engineOffClip.length,
                fadeInDuration = startup?.fadeDuration ?? 2f,
                fadeOutDuration = shutdown?.fadeDuration ?? 1f,
            });

            AudioUtils.Apply(carType, SoundType.EngineLoop, soundSet[SoundType.EngineLoop], audio.engineAudio);
            AudioUtils.Apply(carType, SoundType.EngineLoadLoop, soundSet[SoundType.EngineLoadLoop], audio.enginePistonAudio);
            AudioUtils.Apply(carType, SoundType.TractionMotors, soundSet[SoundType.TractionMotors], audio.electricMotorAudio);
        }

        private static void SetHorn(TrainCarType carType, CustomLocoAudioDiesel audio, SoundSet soundSet)
        {
            var hornHitSource = audio.hornAudio.transform.Find("train_horn_01_hit").GetComponent<AudioSource>();
            AudioUtils.Apply(carType, SoundType.HornHit, soundSet[SoundType.HornHit], hornHitSource);
            AudioUtils.Apply(carType, SoundType.HornLoop, soundSet[SoundType.HornLoop], audio.hornAudio);
        }
    }

    public class CCLSteamAudio
    {
        public static void Apply(TrainCar car, CustomLocoAudioSteam audio, SoundSet soundSet)
        {
            AudioUtils.Apply(
                car.carType,
                SoundType.Whistle,
                soundSet[SoundType.Whistle],
                audio.whistleAudio);
        }
    }
}