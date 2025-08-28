using System.Collections.Generic;

namespace DvMod.ZSounds.AudioMappers
{
    // Audio mapper for LocoDiesel train type
    public class LocoDieselAudioMapper : BaseAudioMapper
    {
        public override Dictionary<SoundType, string> SoundMapping { get; } = new()
        {
            { SoundType.Bell, "Bell_Layered" },
            { SoundType.EngineLoop, "Engine_Idle" },
            { SoundType.EngineLoadLoop, "Engine_Throttling" },
            { SoundType.EngineStartup, "EngineIgnition_Layered" },
            { SoundType.EngineShutdown, "ICE_FuelCutoff_01" },
            { SoundType.TractionMotors, "ElectricMotor_Layered" },
            { SoundType.AirCompressor, "Compressor_Layered" },
            { SoundType.HornLoop, "LocoDiesel_Horn_Layered" },
            { SoundType.HornHit, "Horn_LocoDE6_01_Pulse" },
        };
    }
}