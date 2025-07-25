# Steam Chuff Sounds

Steam locomotive chuff sounds use a complex multi-frequency system with different variants based on locomotive speed, water injection, and ash content. This mod now provides **complete control** over all chuff frequencies and types.

## Complete Chuff Frequency System

### Speed-Based Chuffs (Normal Operation)
- **SteamChuff2_67Hz** → `2.67ChuffsPerSecond` - Very slow speed (idle)
- **SteamChuff3Hz** → `3ChuffsPerSecond` - Slow speed  
- **SteamChuff4Hz** → `4ChuffsPerSecond` - Low speed
- **SteamChuff5_33Hz** → `5.33ChuffsPerSecond` - Medium-low speed
- **SteamChuff8Hz** → `8ChuffsPerSecond` - Medium speed
- **SteamChuff10_67Hz** → `10.67ChuffsPerSecond` - Medium-high speed
- **SteamChuff16Hz** → `16ChuffsPerSecond` - High speed

### Water Injection Chuffs
- **SteamChuff4HzWater** → `4WaterChuffsPerSecond` - Water injection at low speed
- **SteamChuff8HzWater** → `8WaterChuffsPerSecond` - Water injection at medium speed
- **SteamChuff16HzWater** → `16WaterChuffsPerSecond` - Water injection at high speed

### Ash Chuffs (Dirty Firebox)
- **SteamChuff2HzAsh** → `2AshChuffsPerSecond` - Ash particles at slow speed
- **SteamChuff4HzAsh** → `4AshChuffsPerSecond` - Ash particles at low speed  
- **SteamChuff8HzAsh** → `8AshChuffsPerSecond` - Ash particles at medium speed

## How to Use

### Complete Customization
Place custom sound files in **any or all** of the chuff frequency folders:

```
Sounds/
├── LocoS060/
│   ├── SteamChuff2_67Hz/        ← Very slow chuffs
│   │   ├── config.json
│   │   └── idle-chuff.ogg
│   ├── SteamChuff8Hz/           ← Medium speed chuffs  
│   │   ├── config.json
│   │   └── medium-chuff.ogg
│   ├── SteamChuff16Hz/          ← High speed chuffs
│   │   ├── config.json
│   │   └── fast-chuff.ogg
│   ├── SteamChuff4HzWater/      ← Water injection sounds
│   │   ├── config.json
│   │   └── water-chuff.ogg
│   └── SteamChuff8HzAsh/        ← Ash particle sounds
│       ├── config.json
│       └── ash-chuff.ogg
└── LocoSteamHeavy/
    └── [same structure]
```

### Selective Replacement
- **Replace only specific frequencies** - Only place sounds in folders you want to customize
- **Leave others unchanged** - Game will use original sounds for folders without custom files
- **Mix and match** - Replace fast chuffs but keep original slow chuffs, etc.

## Advanced Configuration

Each chuff frequency can have its own `config.json` settings:

```json
{
  "pitch": 0.9,
  "volume": 1.2,
  "comment": "Lower pitch for heavier chuff sound"
}
```

