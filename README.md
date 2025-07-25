# ZSounds (Zeibach's Sounds) for Derail Valley

> **Flexible, folder-based train sound replacement for Derail Valley**

---

## What is ZSounds?
ZSounds is a mod for Derail Valley that lets you easily replace and customize train sounds using simple folders and config files. You can swap out horns, bells, engines, and more for each locomotive type—no coding required!

- **Supports:** All major locomotives (DE6, DE2, SH282, DH4, DM3, Microshunter, S060)
- **Features:**
  - Drag-and-drop folder-based sound replacement
  - In-game sound switching with CommsRadio
  - Per-sound customization (pitch, volume, curves)
  - Always resets to original sounds on game restart

---

## Quick Start
1. **Install Requirements:**
   - [UnityModManager](https://www.nexusmods.com/site/mods/21)
   - [CommsRadioAPI](https://www.nexusmods.com/derailvalley/mods/813)
2. **Install ZSounds:**
   - Download the latest release ZIP
   - Install with Unity Mod Manager
3. **Add Your Sounds:**
   - Open the `Sounds/` folder in the mod directory
   - Find the folder for your train (e.g. `LocoDiesel` for DE6)
   - Place your `.ogg` or `.wav` files in the correct subfolder (e.g. `HornLoop`)
   - (Optional) Add a `config.json` to customize pitch, volume, etc.
4. **Launch the Game:**
   - Use CommsRadio in-game to switch sounds

---

### Train Types
- `LocoDiesel` - DE6
- `LocoShunter` - DE2
- `LocoSteamHeavy` - SH282
- `LocoDH4` - DH4
- `LocoDM3` - DM3
- `LocoMicroshunter` - BE2
- `LocoS060` - S060

### Sound Types
- `HornHit` (Not on all trains)
- `HornLoop`
- `Bell` (Only Steam, DH4, DE6)
- `EngineLoop`
- `EngineLoadLoop`
- `EngineStartup`
- `EngineShutdown`
- `TractionMotors`
- `AirCompressor`

**Steam Only:**
- `Whistle`
- `Dynamo`
- `SteamCylinderChuffs`
- `SteamStackChuffs`
- `SteamValveGear`
- `SteamChuffLoop`

### Supported Audio Formats
- `.ogg` (recommended)
- `.wav`

---

## Sound Configuration (`config.json`)
Each sound folder can have a `config.json` to control pitch, volume, and curves.
The ingame default values are used if the file is missing

**Example:**
```json
{
  "pitch": 1.0,
  "minPitch": 0.8,
  "maxPitch": 1.2,
  "minVolume": 0.0,
  "maxVolume": 1.0,
  "pitchCurve": [
    { "time": 0.0, "value": 0.8 },
    { "time": 0.5, "value": 1.0 },
    { "time": 1.0, "value": 1.2 }
  ],
  "volumeCurve": [
    { "time": 0.0, "value": 0.0 },
    { "time": 0.2, "value": 1.0 },
    { "time": 0.8, "value": 1.0 },
    { "time": 1.0, "value": 0.0 }
  ]
}
```

**Config Fields:**
- `pitch`: Base pitch multiplier (1.0 = normal)
- `minPitch`/`maxPitch`: Pitch range
- `minVolume`/`maxVolume`: Volume range
- `pitchCurve`/`volumeCurve`: Animation curves (see below)

**Curve Keyframe Format:**
```json
{ "time": 0.0, "value": 1.0 }
```
- `time`: 0.0 (start) to 1.0 (end)
- `value`: Pitch or volume at that point

---

## Usage Workflow
- **Replace a sound:** Drop your file in the right folder and click in Settings on "Update Soundlist"
- **Switch sounds in-game:** Use CommsRadio to pick which sound to use for each type.
- **Reset to default:** Restart the game. All sounds revert to original unless you re-select them.

---

## Example Configurations

**1. Lower the bell volume:**
```json
{ "maxVolume": 0.5 }
```

**2. Make the dynamo pitch rise with speed:**
```json
{
  "pitchCurve": [
    { "time": 0.0, "value": 0.7 },
    { "time": 1.0, "value": 1.3 }
  ]
}
```

**3. Fade out the air compressor at high speed:**
```json
{
  "volumeCurve": [
    { "time": 0.0, "value": 1.0 },
    { "time": 0.8, "value": 1.0 },
    { "time": 1.0, "value": 0.0 }
  ]
}
```

---

## Troubleshooting
- **Sound not playing?**
  - Check file format (`.ogg` or `.wav`)
  - Make sure folder names match exactly
  - Check the log for errors
- **CommsRadio not switching sounds?**
  - Make sure CommsRadioAPI is installed and up to date
  - Only sound types with files in their folders will appear in the menu
- **Sounds not resetting?**
  - Restart the game to restore all original sounds
- **One-shot sounds (e.g. HornHit, Bell) not working?**
  - These can only be changed once per game session due to limitations

---

## Advanced Tips
- Use multiple keyframes in curves for more complex pitch/volume changes
- You can use `null` for any config field to leave it at the default

---

## Credits
- Original mod by [Zeibach](https://github.com/mspielberg/dv-zsounds)
- Rewrite by **Fuggschen**

---

## License
MIT

