# Zeibach's Sounds

Sound replacement mod for Derail Valley. Replace train sounds using folders and audio files.

## Features

- Replace sounds for all locomotives
- Drop audio files into folders (`.ogg` or `.wav`)
- Switch sounds in-game with CommsRadio or UI
- UI for editing the sound config files
- Support for editing vanilla sound values like pitch
- Saving of the last applied sounds of a locomotive between sessions
- Declarative sound rules via `sound_rules.json` (extensible without code changes)
- CCL locomotive support via `sound_manifest.json`

## Installation

1. Install [UnityModManager](https://www.nexusmods.com/site/mods/21)
2. Install [DerailValleyModToolbar](https://www.nexusmods.com/derailvalley/mods/1367)
3. Download [Zeibach's Sounds](https://www.nexusmods.com/derailvalley/mods/249?tab=files) and install with Unity Mod Manager

## Using the Mod

### Adding Sounds

1. Navigate to `Mods/ZSounds/Sounds/`
2. Place audio files in the sound type folder (e.g., `HornLoop`)
3. In game, open the UI and click "Update Soundlist"

### In-Game Controls

1. Press Alt and click on the ZS button
2. From there select the Loco you want to edit
3. Select the sound type you want to edit
4. From the dropdown select the sound that should replace the vanilla sound

### Optional Configuration

- Press the Config Button next to a sound in the UI to edit the config for it.
- Alternatively the configs are also stored as .json files in the `Sounds/Config` folder.

### Custom Sound Rules

The mod uses a `sound_rules.json` file to identify which audio components belong to which sound type. You can add your own rules to customize sound detection without modifying the mod.

**Built-in rules:** `Mods/ZSounds/sound_rules.json` (do not edit — overwritten on update)

**Custom rules:** Place your rules in one of these locations:
- `Mods/ZSounds/Sounds/custom_rules.json` — single file
- `Mods/ZSounds/Sounds/rules/*.json` — one or more files

User rules take priority over built-in rules. Rule format:
```json
{
  "version": 1,
  "rules": [
    {
      "pattern": "MyCustomHorn",
      "matchField": "name",
      "soundType": "HornLoop",
      "priority": 100,
      "description": "My custom horn sound"
    }
  ]
}
```

- `pattern`: Regex pattern to match against
- `matchField`: What to match — `"name"` (GameObject name), `"path"` (hierarchy path), `"clip"` (audio clip name), or `"any"` (all three)
- `soundType`: Must match a `SoundType` enum value (e.g., `HornLoop`, `EngineLoop`, `Bell`, `Whistle`)
- `priority`: Higher values are checked first (default: 100)

### CCL Locomotive Support

Custom Car Loader locomotives are supported via sound manifests. CCL authors can place a `sound_manifest.json` in their mod folder (`Mods/<ModName>/sound_manifest.json`) to declare their audio hierarchy:

```json
{
  "version": "1",
  "liveryId": "MyCustomLoco_Livery",
  "audioPrefabPattern": "MyCustomLoco",
  "sounds": [
    {
      "soundType": "EngineLoop",
      "gameObjectName": "engine_layered",
      "componentType": "LayeredAudio"
    },
    {
      "soundType": "HornLoop",
      "gameObjectName": "horn_layered",
      "componentType": "LayeredAudio"
    }
  ]
}
```

See `Examples/sound_manifest.example.json` for a complete reference.

## Additional Information

### Whiste Quilling

As many request this, here a config that works most of the time. Either paste it directly into the config file or copy the values to the UI:
```json
{
  "pitch": 1.0,
  "minPitch": 0.8,
  "maxPitch": 1.0,
  "minVolume": 0.0,
  "maxVolume": 1.0,
  "pitchCurve": [
    { "time": 0.0, "value": 0.8 },
    { "time": 1.0, "value": 1.5 }
  ],
  "volumeCurve": [
    { "time": 0.0, "value": 1.0 },
    { "time": 1.0, "value": 1.0 }
  ]
}
```
### Known Bugs (Restarting the game will probably fix those)
- Chuff sound reset is not working properly. They stay at a constant pitch after resetting.
- Some sounds may not apply or may not reset.
- The CommsRadio menu is considered deprecated in desktop mode and only left in for VR players. If you encounter bugs there, switch to the new UI.

## Credits

- Original mod by [Zeibach](https://github.com/mspielberg)
- Rewrite by [Fuggschen](https://github.com/Fuggschen)

## License

MIT
