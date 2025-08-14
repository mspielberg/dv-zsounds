using System.Collections.Generic;

namespace DvMod.ZSounds.AudioMappers
{
    // Audio mapper for LocoDM3 train type
    public class LocoDM3AudioMapper : BaseAudioMapper
    {
        public override Dictionary<SoundType, string> SoundMapping { get; } = new()
        {
            { SoundType.EngineLoop, "Engine_Layered" },
            { SoundType.EngineLoadLoop, "EnginePiston_Layered" },
            { SoundType.EngineStartup, "EngineIgnition_Layered" },
            { SoundType.EngineShutdown, "ICE_FuelCutoff_01" },
            // LocoDM3 is diesel-mechanical, not diesel-electric, so no electric traction motors or dynamo
            { SoundType.AirCompressor, "Compressor_Layered" },
            { SoundType.HornLoop, "Horn_Layered" },
            // No HornHit audio clips available for LocoDM3
        };
    }
}