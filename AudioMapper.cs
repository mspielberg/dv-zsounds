using DV.ModularAudioCar;
using DV.Simulation.Controllers;
using DV.Simulation.Ports;
using DV.ThingTypes;
using DvMod.ZSounds.Config;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace DvMod.ZSounds
{
    public class AudioMapper
    {
        private Dictionary<SoundType, string> mapping;

        public AudioMapper(Dictionary<SoundType, string> mapping)
        {
            this.mapping = mapping;
        }

        public LayeredAudio? GetLayeredAudio(SoundType soundType, TrainAudio trainAudio)
        {
            if (!mapping.TryGetValue(soundType, out var path))
                return null;

            var modularAudio = trainAudio as CarModularAudio;
            if (modularAudio == null)
                return null;

            var simAudio = modularAudio.audioModules.OfType<SimAudioModule>().FirstOrDefault();
            if (simAudio == null)
                return null;

            var portReaders = simAudio.layeredAudioSimReadersController.entries.OfType<LayeredAudioPortReader>();
            var match = portReaders.FirstOrDefault(entry => entry.name == path)?.layeredAudio;
            if (match == null)
                Main.DebugLog(() => $"Could not find LayeredAudio: carType={trainAudio.car.carType}, soundType={soundType}, path={path}");
            return match;
        }

        public AudioClipPortReader? GetAudioClipPortReader(SoundType soundType, TrainAudio trainAudio)
        {
            if (!mapping.TryGetValue(soundType, out var path))
                return null;

            var modularAudio = trainAudio as CarModularAudio;
            if (modularAudio == null)
                return null;

            var simAudio = modularAudio.audioModules.OfType<SimAudioModule>().FirstOrDefault();
            if (simAudio == null)
                return null;

            var portReaders = simAudio.layeredAudioSimReadersController.entries.OfType<AudioClipPortReader>();
            var match = portReaders.FirstOrDefault(portReader => portReader.clips.Any(clip => clip.name == path));
            if (match == null)
                Main.DebugLog(() => $"Could not find AudioClipPortReader: carType={trainAudio.car.carType}, soundType={soundType}, path={path}");
            return match;
        }

        public static readonly IDictionary<TrainCarType, AudioMapper> mappings =
            new ReadOnlyDictionary<TrainCarType, AudioMapper>(new Dictionary<TrainCarType, AudioMapper>()
            {
                {
                    TrainCarType.LocoShunter, new AudioMapper(new Dictionary<SoundType, string>()
                    {
                        { SoundType.EngineLoop, "Engine_Layered" },
                        { SoundType.EngineLoadLoop, "EnginePiston_Layered" },
                        { SoundType.TractionMotors, "ElectricMotor_Layered" },

                        { SoundType.HornHit, "Horn_LocoDE2_01_Pulse" },
                        { SoundType.HornLoop, "Horn_Layered" },
                    })
                },
                {
                    TrainCarType.LocoDiesel, new AudioMapper(new Dictionary<SoundType, string>()
                    {
                        { SoundType.EngineLoop, "Engine_Idle" },
                        { SoundType.EngineLoadLoop, "Engine_Throttling" },
                        { SoundType.TractionMotors, "ElectricMotor_Layered" },

                        { SoundType.HornHit, "Horn_LocoDE6_01_Pulse "},
                        { SoundType.HornLoop, "LocoDiesel_Horn_Layered" },
                    })
                },
            });
    }
}