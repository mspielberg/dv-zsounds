using DvMod.ZSounds.Config;

namespace DvMod.ZSounds
{
    public static class SteamAudio
    {
        public static void Apply(TrainCar car, SoundSet soundSet)
        {
            var audio = car.GetComponentInChildren<LocoAudioSteam>();
            AudioUtils.Apply(TrainCarType.LocoSteamHeavy, SoundType.SteamCylinderChuffs, soundSet, ref audio.cylClipsSlow);
            AudioUtils.Apply(TrainCarType.LocoSteamHeavy, SoundType.SteamStackChuffs, soundSet, ref audio.chimneyClipsSlow);
            AudioUtils.Apply(TrainCarType.LocoSteamHeavy, SoundType.SteamValveGear, soundSet, audio.valveGearLayered);
            AudioUtils.Apply(TrainCarType.LocoSteamHeavy, SoundType.SteamChuffLoop, soundSet, audio.steamChuffsLayered);
            AudioUtils.Apply(TrainCarType.LocoSteamHeavy, SoundType.Whistle, soundSet, audio.whistleAudio);
        }
    }
}