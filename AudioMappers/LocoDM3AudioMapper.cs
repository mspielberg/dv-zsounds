using System.Collections.Generic;

namespace DvMod.ZSounds.AudioMappers
{
    /// Audio mapper for LocoDM3 train type
    public class LocoDM3AudioMapper : BaseAudioMapper
    {
        public override Dictionary<SoundType, string> SoundMapping { get; } = new()
        {
            { SoundType.EngineLoop, "Engine_Layered" },
            { SoundType.EngineLoadLoop, "EnginePiston_Layered" },
            // LocoDM3 is diesel-mechanical, not diesel-electric, so no electric traction motors or dynamo
            { SoundType.AirCompressor, "Compressor_Layered" },
            { SoundType.HornLoop, "Horn_Layered" },
            { SoundType.EngineStartup, "EngineIgnition_Layered" },
            // HornHit clips - need to check actual clip names in the game for DM3
            // { SoundType.HornHit, "Horn_LocoDM3_01_Pulse" },
        };
    }
}
