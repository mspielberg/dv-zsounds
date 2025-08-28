# dv-zsounds Sound Configuration Tutorial

## Overview

The config file (named `config.json`) allows you to customize how a specific sound behaves for a train and sound type. You can control pitch, volume, and how these change over time or with engine speed using curves.

Place your `config.json` inside the appropriate sound folder, e.g.:
```
Sounds/<Train>/<SoundType>/config.json
```

## Config Fields Explained

| Field         | Type      | Description                                                                                 |
|---------------|-----------|---------------------------------------------------------------------------------------------|
| pitch         | float/null| Multiplies the base pitch. `1.0` = normal, `2.0` = double, `0.5` = half.                   |
| minPitch      | float/null| Minimum allowed pitch.                                                                      |
| maxPitch      | float/null| Maximum allowed pitch.                                                                      |
| minVolume     | float/null| Minimum allowed volume (0.0 = silent, 1.0 = full).                                         |
| maxVolume     | float/null| Maximum allowed volume.                                                                     |
| fadeStart     | float/null| When to start fading out (not always used).                                                 |
| fadeDuration  | float/null| How long the fade lasts (not always used).                                                  |
| pitchCurve    | array     | Animation curve for pitch. Each entry is a keyframe (see below).                            |
| volumeCurve   | array     | Animation curve for volume. Each entry is a keyframe.                                       |

### Animation Curve Keyframe

Each keyframe in `pitchCurve` or `volumeCurve` looks like:
```json
{
  "time": 0.0,         // Position along the curve (0.0 = start, 1.0 = end)
  "value": 1.0,        // Pitch or volume at this point
  "inTangent": 0.0,    // Slope coming into this keyframe (usually 0.0)
  "outTangent": 0.0    // Slope going out (usually 0.0)
}
```
- `time` must be between 0.0 and 1.0 (start to end of the parameter, e.g. engine speed).
- `value` is the pitch or volume multiplier at that point.

## How the System Works

- If you set `pitch`, it multiplies the base pitch for the sound.
- If you set a `pitchCurve`, it overrides `pitch` and lets you control pitch dynamically (e.g., with engine speed).
- The same applies for `volume` and `volumeCurve`.
- If you set a field to `null`, it is ignored and the default/game value is used.

## Examples

### 1. Simple: Lower the volume

```json
{
  "minVolume": 0.0,
  "maxVolume": 0.5
}
```
- Makes the sound play at half the normal volume.

---

### 2. Make the pitch increase with engine speed

```json
{
  "pitchCurve": [
    { "time": 0.0, "value": 0.8, "inTangent": 0.0, "outTangent": 0.0 },
    { "time": 1.0, "value": 1.5, "inTangent": 0.0, "outTangent": 0.0 }
  ]
}
```
- At idle (0.0), pitch is 0.8x (lower).
- At full speed (1.0), pitch is 1.5x (higher).

---

### 3. Keep pitch low until high speed, then ramp up

```json
{
  "pitchCurve": [
    { "time": 0.0, "value": 0.7, "inTangent": 0.0, "outTangent": 0.0 },
    { "time": 0.7, "value": 0.7, "inTangent": 0.0, "outTangent": 0.0 },
    { "time": 1.0, "value": 1.2, "inTangent": 0.0, "outTangent": 0.0 }
  ]
}
```
- Pitch stays at 0.7x until 70% speed, then rises to 1.2x at full speed.

---

### 4. Fade out the sound at high speed

```json
{
  "volumeCurve": [
    { "time": 0.0, "value": 1.0, "inTangent": 0.0, "outTangent": 0.0 },
    { "time": 0.8, "value": 1.0, "inTangent": 0.0, "outTangent": 0.0 },
    { "time": 1.0, "value": 0.0, "inTangent": 0.0, "outTangent": 0.0 }
  ]
}
```
- Volume is normal until 80% speed, then fades to silent at full speed.

---

### 5. Use only a fixed pitch and volume

```json
{
  "pitch": 1.2,
  "minVolume": 0.5,
  "maxVolume": 0.5
}
```
- Pitch is always 1.2x, volume is always 0.5x.

---

## Tips

- If you want to use curves, set `pitch`, `minPitch`, and `maxPitch` to `null` so only the curve is used.
- Use at least two keyframes for a curve: one at `time: 0.0` and one at `time: 1.0`.
- Tangents (`inTangent`, `outTangent`) can usually be `0.0` for a straight line.
- If you set a field to `null`, the mod will ignore it and use the default/game value.

## Troubleshooting

- If your sound is always high or low, check your curve’s `value` at `time: 0.0` and `time: 1.0`.
- If nothing changes, make sure your config file is in the correct folder and named `config.json`.
- All `time` values in curves must be between `0.0` and `1.0`.

---

## Advanced: Combining Curves and Limits

You can use both curves and min/max values for more control:
```json
{
  "minPitch": 0.8,
  "maxPitch": 1.5,
  "pitchCurve": [
    { "time": 0.0, "value": 0.8, "inTangent": 0.0, "outTangent": 0.0 },
    { "time": 1.0, "value": 1.5, "inTangent": 0.0, "outTangent": 0.0 }
  ]
}
```
- The pitch will always stay between 0.8 and 1.5, even if the curve would go outside that range.

---

## Summary

- Place your `config.json` in the correct sound folder.
- Use `pitchCurve` and `volumeCurve` for dynamic changes.
- Use `pitch`, `minPitch`, `maxPitch`, `minVolume`, `maxVolume` for fixed or limited values.
- All curve `time` values must be between 0.0 and 1.0.