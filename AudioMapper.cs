using DV.ModularAudioCar;
using DV.Simulation.Controllers;
using DV.Simulation.Ports;
using DV.ThingTypes;
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

            // Check if the SimAudioModule is fully initialized
            if (simAudio.audioClipSimReadersController?.entries == null)
            {
                Main.DebugLog(() => $"SimAudioModule not fully initialized for {trainAudio.car.carType}, skipping HornHit validation");
                return null;
            }

            var portReaders = simAudio.audioClipSimReadersController.entries.OfType<AudioClipPortReader>();
            
            var match = portReaders.FirstOrDefault(portReader => portReader.clips.Any(clip => clip.name == path));
            if (match == null)
                Main.DebugLog(() => $"Could not find AudioClipPortReader: carType={trainAudio.car.carType}, soundType={soundType}, path={path}");
            return match;
        }

        public static readonly IDictionary<TrainCarType, AudioMapper> mappings =
            new ReadOnlyDictionary<TrainCarType, AudioMapper>(new Dictionary<TrainCarType, AudioMapper>()
            {
                {
                    TrainCarType.LocoShunter, new(new()
                    {
                        { SoundType.EngineLoop, "Engine_Layered" },
                        { SoundType.EngineLoadLoop, "EnginePiston_Layered" },
                        { SoundType.TractionMotors, "ElectricMotor_Layered" },
                        { SoundType.Dynamo, "Dynamo_Layered" },
                        { SoundType.AirCompressor, "AirCompressor_Layered" },

                        { SoundType.HornLoop, "Horn_Layered" },
                    })
                },
                {
                    TrainCarType.LocoDiesel, new(new()
                    {
                        { SoundType.Bell, "Bell_Layered" },

                        { SoundType.EngineLoop, "Engine_Idle" },
                        { SoundType.EngineLoadLoop, "Engine_Throttling" },
                        { SoundType.TractionMotors, "ElectricMotor_Layered" },
                        { SoundType.Dynamo, "Dynamo_Layered" },
                        { SoundType.AirCompressor, "AirCompressor_Layered" },

                        { SoundType.HornLoop, "LocoDiesel_Horn_Layered" },
                    })
                },
                {
                    TrainCarType.LocoDH4, new(new()
                    {
                        { SoundType.Bell, "Bell_Layered" },

                        { SoundType.EngineLoop, "Engine_Layered" },
                        { SoundType.EngineLoadLoop, "EnginePiston_Layered" },
                        { SoundType.TractionMotors, "ElectricMotor_Layered" },
                        { SoundType.Dynamo, "Dynamo_Layered" },
                        { SoundType.AirCompressor, "AirCompressor_Layered" },

                        { SoundType.HornLoop, "Horn_Layered" },
                    })
                },
                {
                    TrainCarType.LocoDM3, new(new()
                    {
                        { SoundType.EngineLoop, "Engine_Layered" },
                        { SoundType.EngineLoadLoop, "EnginePiston_Layered" },
                        { SoundType.TractionMotors, "ElectricMotor_Layered" },
                        { SoundType.Dynamo, "Dynamo_Layered" },
                        { SoundType.AirCompressor, "AirCompressor_Layered" },

                        { SoundType.HornLoop, "Horn_Layered" },
                    })
                },
                {
                    TrainCarType.LocoMicroshunter, new AudioMapper(new()
                    {
                        { SoundType.TractionMotors, "ElectricMotor_Layered" },
                        { SoundType.Dynamo, "Dynamo_Layered" },
                        { SoundType.AirCompressor, "AirCompressor_Layered" },

                        { SoundType.HornLoop, "Horn_Layered" },
                    })
                },
                {
                    TrainCarType.LocoS060, new(new()
                    {
                        { SoundType.Bell, "Bell_Layered" },
                        { SoundType.SteamCylinderChuffs, "ChuffClips_Layered" },
                        { SoundType.SteamValveGear, "ValveGear_Layered" },
                        { SoundType.Dynamo, "Dynamo_Layered" },
                        { SoundType.AirCompressor, "AirCompressor_Layered" },

                        { SoundType.Whistle, "Whistle_Layered" },
                    })
                },
                {
                    TrainCarType.LocoSteamHeavy, new(new()
                    {
                        { SoundType.Bell, "Bell_Layered" },
                        { SoundType.SteamCylinderChuffs, "ChuffClips_Layered" },
                        { SoundType.SteamValveGear, "ValveGear_Layered" },
                        { SoundType.Dynamo, "Dynamo_Layered" },
                        { SoundType.AirCompressor, "AirCompressor_Layered" },

                        { SoundType.Whistle, "Whistle_Layered" },
                    })
                },
            });
    }
}