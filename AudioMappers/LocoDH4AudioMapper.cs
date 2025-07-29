using System.Collections.Generic;

namespace DvMod.ZSounds.AudioMappers
{
    // Audio mapper for LocoDH4 train type
    public class LocoDH4AudioMapper : BaseAudioMapper
    {
        public override Dictionary<SoundType, string> SoundMapping { get; } = new()
        {
            { SoundType.Bell, "Bell_Layered" },
            { SoundType.EngineLoop, "Engine_Layered" },
            { SoundType.EngineLoadLoop, "EnginePiston_Layered" },
            { SoundType.EngineStartup, "EngineIgnition_Layered" },
            { SoundType.EngineShutdown, "ICE_FuelCutoff_01" },
            // LocoDH4 is diesel-hydraulic, not diesel-electric, so no electric traction motors or dynamo
            { SoundType.AirCompressor, "Compressor_Layered" },
            { SoundType.HornLoop, "Horn_Layered" },
            { SoundType.HornHit, "Horn_LocoDE2_01_Pulse" },
        };
    }
}
