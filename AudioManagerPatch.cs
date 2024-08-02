using DV.ThingTypes;
using DvMod.ZSounds.Config;
using HarmonyLib;

namespace DvMod.ZSounds
{
    [HarmonyPatch(typeof(AudioManager), nameof(AudioManager.Start))]
    public static class AudioManagerPatch
    {
        public static void Postfix()
        {
            var soundSet = Config.Config.Active!.GenericSoundSet();
            var audioManager = AudioManager.Instance;
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.Collision, soundSet, ref audioManager.collisionClips);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.JunctionJoint, soundSet, ref audioManager.junctionJointClips);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.RollingAudioDetailed, soundSet, audioManager.rollingAudioDetailed);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.RollingAudioSimple, soundSet, audioManager.rollingAudioSimple);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.SquealAudioDetailed, soundSet, audioManager.squealAudioDetailed);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.SquealAudioSimple, soundSet, audioManager.squealAudioSimple);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.Coupling, soundSet, ref audioManager.couplingClips);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.Uncoupling, soundSet, ref audioManager.uncouplingClips);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.Wind, soundSet, audioManager.windAudio);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.DerailHit, soundSet, ref audioManager.derailHitClip);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.Switch, soundSet, ref audioManager.switchClips);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.SwitchForced, soundSet, ref audioManager.switchForcedClips);
            AudioUtils.Apply(TrainCarType.NotSet, SoundType.CargoLoadUnload, soundSet, ref audioManager.cargoLoadUnload);
        }
    }
}