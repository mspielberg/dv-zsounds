# Zeibach's Sounds

Sound replacement mod for Derail Valley. Replace train sounds using folders and audio files.

## Features

- Replace sounds for all locomotives
- Drop audio files into folders (`.ogg` or `.wav`)
- Switch sounds in-game with CommsRadio or UI
- UI for editing the sound config files
- Support for editing vanilla sound values like pitch
- Saving of the last applied sounds of a locomotive between sessions
- Experimental support for CCL Locos. Not guaranteed to work

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
