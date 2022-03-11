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
                SoundType.Whistle,
                soundSet[SoundType.Whistle],
                audio.whistleAudio);
        }
    }
}