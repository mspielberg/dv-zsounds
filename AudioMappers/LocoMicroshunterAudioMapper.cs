using System.Collections.Generic;

namespace DvMod.ZSounds.AudioMappers
{
    /// Audio mapper for LocoMicroshunter train type
    public class LocoMicroshunterAudioMapper : BaseAudioMapper
    {
        public override Dictionary<SoundType, string> SoundMapping { get; } = new()
        {
            { SoundType.TractionMotors, "ElectricMotor_Layered" },
            { SoundType.AirCompressor, "Compressor_Layered" },
            { SoundType.HornLoop, "Horn_Layered" }
            // LocoMicroshunter is purely electric - no engine, dynamo, or HornHit clips
        };
    }
}
