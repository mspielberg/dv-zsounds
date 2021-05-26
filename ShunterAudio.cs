using DvMod.ZSounds.Config;
using UnityEngine;

namespace DvMod.ZSounds
{
    public static class ShunterAudio
    {
        public static void Apply(TrainCar car, SoundSet soundSet)
        {
            var audio = car.GetComponentInChildren<LocoAudioShunter>();
            SetEngine(audio, soundSet);
            SetHorn(audio, soundSet);
        }

        public static void SetEngine(LocoAudioShunter audio, SoundSet soundSet)
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
}