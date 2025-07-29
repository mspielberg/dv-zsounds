using System.Collections.Generic;

namespace DvMod.ZSounds.AudioMappers
{
    // Audio mapper for LocoDM1U train type
    public class LocoDM1UAudioMapper : BaseAudioMapper
    {
        public override Dictionary<SoundType, string> SoundMapping { get; } = new()
        {
            { SoundType.EngineLoop, "Engine_Layered" },
            { SoundType.EngineLoadLoop, "EnginePiston_Layered" },
            // LocoDM1U is diesel-mechanical, not diesel-electric, so no electric traction motors or dynamo
            { SoundType.AirCompressor, "Compressor_Layered" },
            { SoundType.HornLoop, "Horn_Layered" },
            { SoundType.EngineStartup, "EngineIgnition_Layered" },
            // No HornHit audio clips available for LocoDM1U
        };
    }
}
