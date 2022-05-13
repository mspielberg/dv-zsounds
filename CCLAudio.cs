using DVCustomCarLoader.LocoComponents;
using DVCustomCarLoader.LocoComponents.DieselElectric;
using DVCustomCarLoader.LocoComponents.Steam;
using DvMod.ZSounds.Config;
using UnityEngine;

namespace DvMod.ZSounds
{
    public static class CCLAudio
    {
        public static void Apply(TrainCar car, SoundSet soundSet)
        {
            Main.DebugLog(() => $"Applying to {car.ID}:\n{soundSet}");
            switch (car.GetComponentInChildren<CustomLocoAudio>())
            {
                case CustomLocoAudioDiesel diesel:
                    CCLDieselAudio.Apply(car, diesel, soundSet);
                    break;
                case CustomLocoAudioSteam steam:
                    CCLSteamAudio.Apply(car, steam, soundSet);
                    break;
            }
        }
    }

    public static class CCLDieselAudio
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

    public static class CCLSteamAudio
    {
        public static void Apply(TrainCar car, CustomLocoAudioSteam audio, SoundSet soundSet)
        {
            AudioUtils.Apply(
                car.carType,
                SoundType.SteamCylinderChuffs,
                soundSet[SoundType.SteamCylinderChuffs],
                ref audio.cylClipsSlow);
            AudioUtils.Apply(
                car.carType,
                SoundType.SteamStackChuffs,
                soundSet[SoundType.SteamStackChuffs],
                ref audio.chimneyClipsSlow);
            AudioUtils.Apply(
                car.carType,
                SoundType.SteamValveGear,
                soundSet[SoundType.SteamValveGear],
                audio.valveGearLayered);
            AudioUtils.Apply(
                car.carType,
                SoundType.SteamChuffLoop,
                soundSet[SoundType.SteamChuffLoop],
                audio.steamChuffsLayered);
            AudioUtils.Apply(
                car.carType,
                SoundType.Whistle,
                soundSet[SoundType.Whistle],
                audio.whistleAudio);
        }
    }
}