using System.Collections.Generic;

namespace DvMod.ZSounds.AudioMappers
{
    // Audio mapper for LocoS060 train type
    public class LocoS060AudioMapper : BaseAudioMapper
    {
        public override Dictionary<SoundType, string> SoundMapping { get; } = new()
        {
            { SoundType.Bell, "Bell_Layered" },
            { SoundType.SteamValveGear, "Mechanism_Layered" },
            { SoundType.Dynamo, "Dynamo_Layered" },
            { SoundType.AirCompressor, "AirPump_Layered" },
            { SoundType.Whistle, "Whistle_Layered" },
            // Steam chuff frequencies - all speed variants
            { SoundType.SteamChuff2_67Hz, "2.67ChuffsPerSecond" },
            { SoundType.SteamChuff3Hz, "3ChuffsPerSecond" },
            { SoundType.SteamChuff4Hz, "4ChuffsPerSecond" },
            { SoundType.SteamChuff5_33Hz, "5.33ChuffsPerSecond" },
            { SoundType.SteamChuff8Hz, "8ChuffsPerSecond" },
            { SoundType.SteamChuff10_67Hz, "10.67ChuffsPerSecond" },
            { SoundType.SteamChuff16Hz, "16ChuffsPerSecond" },
            // Water injection chuffs
            { SoundType.SteamChuff4HzWater, "4WaterChuffsPerSecond" },
            { SoundType.SteamChuff8HzWater, "8WaterChuffsPerSecond" },
            { SoundType.SteamChuff16HzWater, "16WaterChuffsPerSecond" },
            // Ash chuffs
            { SoundType.SteamChuff2HzAsh, "2AshChuffsPerSecond" },
            { SoundType.SteamChuff4HzAsh, "4AshChuffsPerSecond" },
            { SoundType.SteamChuff8HzAsh, "8AshChuffsPerSecond" },
        };
    }
}
