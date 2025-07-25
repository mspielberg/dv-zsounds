using DV.ModularAudioCar;
using DV.Simulation.Controllers;
using DV.Simulation.Ports;
using DV.ThingTypes;
using System.Collections.Generic;

namespace DvMod.ZSounds.AudioMappers
{
    /// Interface for audio mappers that handle sound mapping for specific train types
    public interface IAudioMapper
    {
        /// Gets the sound mapping dictionary for this train type
        Dictionary<SoundType, string> SoundMapping { get; }

        /// Gets LayeredAudio for the specified sound type
        LayeredAudio? GetLayeredAudio(SoundType soundType, TrainAudio trainAudio);

        /// Gets AudioClipPortReader for the specified sound type
        AudioClipPortReader? GetAudioClipPortReader(SoundType soundType, TrainAudio trainAudio);
    }
}
