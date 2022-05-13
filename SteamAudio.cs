using DvMod.ZSounds.Config;

namespace DvMod.ZSounds
{
    public static class SteamAudio
    {
        public static void Apply(TrainCar car, SoundSet soundSet)
        {
            var audio = car.GetComponentInChildren<LocoAudioSteam>();
            AudioUtils.Apply(
                TrainCarType.LocoSteamHeavy,
                SoundType.SteamCylinderChuffs,
                soundSet[SoundType.SteamCylinderChuffs],
                ref audio.cylClipsSlow);
            AudioUtils.Apply(
                TrainCarType.LocoSteamHeavy,
                SoundType.SteamStackChuffs,
                soundSet[SoundType.SteamStackChuffs],
                ref audio.chimneyClipsSlow);
            AudioUtils.Apply(
                TrainCarType.LocoSteamHeavy,
                SoundType.SteamValveGear,
                soundSet[SoundType.SteamValveGear],
                audio.valveGearLayered);
            AudioUtils.Apply(
                TrainCarType.LocoSteamHeavy,
                SoundType.SteamChuffLoop,
                soundSet[SoundType.SteamChuffLoop],
                audio.steamChuffsLayered);
            AudioUtils.Apply(
                TrainCarType.LocoSteamHeavy,
                SoundType.Whistle,
                soundSet[SoundType.Whistle],
                audio.whistleAudio);
        }
    }
}