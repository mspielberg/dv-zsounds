Config =
    version: semver string
    soundSets:
        LocoDiesel: LocoSoundSet[]
        loco_621: LocoSoundSet[]
        loco_steam_H: LocoSoundSet[]
    rules: SkinRule[]

LocoSoundSet =
    name: string
    sounds:
        engineLoop: SoundDef[]
        horn: SoundDef[]
        whistle: SoundDef[]
        ...

SoundDef =
    type: "file" | "default"
    filename
    pitch
    minPitch
    maxPitch
    fadeStart
    fadeDuration

SkinRule =
    type: "default" | "skin"
    skin: string
    soundSets: []
        name: string
        weight: float

===============================

Rule engine

Rule =
    name: string?
    type: RuleType

RuleType =
    if
        property
        value
        rule
|   one-of
        rules
        weights
|   all-of
        rules
|   ref
        name
|   sound