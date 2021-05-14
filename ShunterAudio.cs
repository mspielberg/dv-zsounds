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

            soundSet.sounds.TryGetValue(SoundType.EngineLoop, out var loop);
            AudioUtils.Apply(loop, "DE2 engine loop", audio.engineAudio);
            audio.engineAudio.minPitch = 1f;
            audio.engineAudio.maxPitch = 2100f / 1250f;
            audio.engineAudio.layers[0].volumeCurve = AnimationCurve.EaseInOut(0, 0.1f, 1, 0.4f);
        }

        private static void SetHorn(LocoAudioShunter audio, SoundSet soundSet)
        {
            soundSet.sounds.TryGetValue(SoundType.HornHit, out var hit);
            var hornHitSource = audio.hornAudio.transform.Find("train_horn_01_hit").GetComponent<AudioSource>();
            AudioUtils.Apply(hit, "DE2 horn hit", hornHitSource);
            soundSet.sounds.TryGetValue(SoundType.HornLoop, out var loop);
            AudioUtils.Apply(loop, "DE2 horn loop", audio.hornAudio);
        }
    }
}