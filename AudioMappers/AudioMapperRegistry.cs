using System.Collections.Generic;
using System.Collections.ObjectModel;

using DV.ThingTypes;

using DvMod.ZSounds.AudioMappers;

namespace DvMod.ZSounds
{
    // Main audio mapper that delegates to specific train type mappers
    public class AudioMapperRegistry
    {
        public static readonly IDictionary<TrainCarType, IAudioMapper> Mappers =
            new ReadOnlyDictionary<TrainCarType, IAudioMapper>(new Dictionary<TrainCarType, IAudioMapper>()
            {
                { TrainCarType.LocoShunter, new LocoShunterAudioMapper() },
                { TrainCarType.LocoDiesel, new LocoDieselAudioMapper() },
                { TrainCarType.LocoDH4, new LocoDH4AudioMapper() },
                { TrainCarType.LocoDM1U, new LocoDM1UAudioMapper() },
                { TrainCarType.LocoDM3, new LocoDM3AudioMapper() },
                { TrainCarType.LocoS060, new LocoS060AudioMapper() },
                { TrainCarType.LocoSteamHeavy, new LocoSteamHeavyAudioMapper() },
                { TrainCarType.LocoMicroshunter, new LocoMicroshunterAudioMapper() },
            });
    }
}