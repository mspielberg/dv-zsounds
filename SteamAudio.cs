using DvMod.ZSounds.Config;

namespace DvMod.ZSounds
{
    public static class SteamAudio
    {
        public static void Apply(TrainCar car, SoundSet soundSet)
        {
            var audio = car.GetComponentInChildren<LocoAudioSteam>();
            Main.DebugLog(() => $"[{car.ID}] Loading {SoundType.Whistle} {soundSet[SoundType.Whistle]}");
            AudioUtils.Apply(
                soundSet[SoundType.Whistle],
                "SH282 whistle",
                audio.whistleAudio);
        }

        public static void ResetAllAudio()
        {}
    }
}