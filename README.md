# ZSounds (Zeibach's Sounds) for Derail Valley

# CURRENTLY UNDER DEVELOPMENT
## Some Soundtypes are a one-shot and can not be restored until game restart!
## All Sounds DO RESET ingame after a restart. THIS IS NOT A BUG

---

## Overview
ZSounds is a powerful and flexible sound mod for Derail Valley that allows you to replace and extend in-game train sounds. It uses a simple folder-based system for loading custom sound files.

- **Supports:** All major locomotive types (DE6, DE2, SH282, DH4, DM3, Microshunter, S060)
- **Features:**
  - Folder-based sound loading
  - CommsRadioAPI integration for in-game sound switching
  - Full reset and fallback to original game sounds (Currently bugged. Need help)

---

## Installation
1. **Requirements:**
   - Derail Valley
   - [UnityModManager](https://www.nexusmods.com/site/mods/21)
   - [CommsRadioAPI](https://www.nexusmods.com/derailvalley/mods/813)
2. **Install:**
   - Download the latest Release from the Release tab
   - Install the ZIP File with Unity Mod Manager
   - Add your sounds as described below

---

## Folder-Based Sound System

ZSounds now supports a simple folder-based sound system.

### Folder Structure
Organize your sounds in the following structure:

```
Sounds/
??? Generic/                    # Generic sounds (used for all trains)
?   ??? Collision/
?   ??? Coupling/
?   ??? Uncoupling/
?   ??? ...
??? LocoDiesel/                # Sounds for DE6 locomotive
?   ??? HornHit/
?   ??? HornLoop/
?   ??? Bell/
?   ??? EngineStartup/
?   ??? EngineLoop/
?   ??? EngineShutdown/
??? LocoShunter/               # Sounds for DE2 locomotive  
?   ??? HornHit/
?   ??? HornLoop/
?   ??? EngineStartup/
?   ??? EngineLoop/
??? LocoSteamHeavy/            # Sounds for SH282 locomotive
?   ??? Whistle/
??? ...
```

#### Train Types
Folder names must match the exact TrainCarType enum values:
- `LocoDiesel` - DE6 diesel locomotive
- `LocoShunter` - DE2 shunter locomotive  
- `LocoSteamHeavy` - SH282 steam locomotive
- `LocoDH4` - DH4 hydraulic locomotive
- `LocoDM3` - DM3 diesel multiple unit
- `LocoMicroshunter` - Microshunter
- `LocoS060` - S060 steam locomotive

#### Sound Types
Sound type folder names must match the SoundType enum values:
- `HornHit` - Horn hit sounds (Currently bugged. Can only be applied once)
- `HornLoop` - Horn loop sounds  
- `Whistle` - Steam whistle sounds
- `Bell` - Bell sounds (Currenty bugged. Won't reset until game restart)
- `EngineStartup` - Engine startup sounds
- `EngineLoop` - Engine idle/loop sounds
- `EngineLoadLoop` - Engine under load sounds
- `EngineShutdown` - Engine shutdown sounds
- `TractionMotors` - Electric motor sounds

##### Generic Sound Types
- `Collision`
- `Coupling`
- `Uncoupling`
- `RollingAudioDetailed`
- `RollingAudioSimple`
- `SquealAudioDetailed`
- `SquealAudioSimple`
- `Wind`
- `DerailHit`
- `Switch`
- `SwitchForced`
- `CargoLoadUnload`

#### Supported Audio Formats
- `.ogg` (recommended)
- `.wav`

---


#### Troubleshooting
- **Sounds not loading?** Check the console for error messages
- **Wrong train type?** Ensure folder names match exactly: `LocoDiesel`, `LocoShunter`, `LocoSteamHeavy`
- **Wrong sound type?** Ensure folder names match: `HornHit`, `HornLoop`, `Bell`, `EngineStartup`, etc.
- **Files not found?** Only `.ogg` and `.wav` files are supported
- **Sounds not resetting?** Restart the game. All sounds will be reset

---


## Development & Contribution
- **Source:** [GitHub](https://github.com/Fuggschen/dv-zsounds)
- **License:** MIT

---

## Credits
- Original mod by [Zeibach](https://github.com/mspielberg/dv-zsounds)
- Rewrite, folder system, and CommsRadioAPI integration by **Fuggschen**

