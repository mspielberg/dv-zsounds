using System.Collections.Generic;

namespace DvMod.ZSounds.AudioMappers
{
    // Audio mapper for LocoShunter train type
    public class LocoShunterAudioMapper : BaseAudioMapper
    {
        public override Dictionary<SoundType, string> SoundMapping { get; } = new()
        {
            { SoundType.EngineLoop, "Engine_Layered" },
            { SoundType.EngineLoadLoop, "EnginePiston_Layered" },
            { SoundType.TractionMotors, "ElectricMotor_Layered" },
            { SoundType.AirCompressor, "Compressor_Layered" },
            { SoundType.HornLoop, "Horn_Layered" },
            { SoundType.EngineStartup, "EngineIgnition_Layered" },
            { SoundType.HornHit, "Horn_LocoDE2_01_Pulse" },
        };
    }
}
