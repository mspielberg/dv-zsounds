using DV.ModularAudioCar;
using DV.Simulation.Ports;
using DV.ThingTypes;
using DvMod.ZSounds.AudioMappers;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DvMod.ZSounds
{
    /// Main audio mapper that provides access to train-specific audio mappers
    public static class AudioMapper
    {
        /// Gets the audio mapper for the specified train type
        public static IAudioMapper? GetMapper(TrainCarType trainCarType)
        {
            return AudioMapperRegistry.Mappers.TryGetValue(trainCarType, out var mapper) ? mapper : null;
        }

        /// Gets LayeredAudio for the specified sound type and train
        public static LayeredAudio? GetLayeredAudio(SoundType soundType, TrainAudio trainAudio)
        {
            var mapper = GetMapper(trainAudio.car.carType);
            return mapper?.GetLayeredAudio(soundType, trainAudio);
        }

        /// Gets AudioClipPortReader for the specified sound type and train
        public static AudioClipPortReader? GetAudioClipPortReader(SoundType soundType, TrainAudio trainAudio)
        {
            var mapper = GetMapper(trainAudio.car.carType);
            return mapper?.GetAudioClipPortReader(soundType, trainAudio);
        }

        /// Dictionary of all available audio mappers by train type
        public static readonly IDictionary<TrainCarType, IAudioMapper> Mappers = AudioMapperRegistry.Mappers;
    }
}