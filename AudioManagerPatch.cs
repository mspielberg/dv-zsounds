using DvMod.ZSounds.Config;
using HarmonyLib;

namespace DvMod.ZSounds
{
    [HarmonyPatch(typeof(AudioManager), nameof(AudioManager.Awake))]
    public static class AudioManagerPatch
    {
        public static void Postfix()
        {
            var soundSet = Config.Config.Active!.GenericSoundSet();
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.Collision, soundSet, ref AudioManager.e.collisionClips);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.JunctionJoint, soundSet, ref AudioManager.e.junctionJointClips);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.RollingAudioDetailed, soundSet, AudioManager.e.rollingAudioDetailed);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.RollingAudioSimple, soundSet, AudioManager.e.rollingAudioSimple);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.SquealAudioDetailed, soundSet, AudioManager.e.squealAudioDetailed);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.SquealAudioSimple, soundSet, AudioManager.e.squealAudioSimple);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.Coupling, soundSet, ref AudioManager.e.couplingClips);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.Uncoupling, soundSet, ref AudioManager.e.uncouplingClips);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.Wind, soundSet, AudioManager.e.windAudio);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.DerailHit, soundSet, ref AudioManager.e.derailHitClip);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.Switch, soundSet, ref AudioManager.e.switchClips);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.SwitchForced, soundSet, ref AudioManager.e.switchForcedClips);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.CargoLoadUnload, soundSet, ref AudioManager.e.cargoLoadUnload);
        }
    }
}