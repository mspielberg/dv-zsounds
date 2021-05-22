# Configuring ZSounds

ZSounds uses configuation files named `zsounds-config.json`. The file located in the `Mods\ZSounds` directory is loaded first, then any other `zsounds-config.json` files found anywhere in the `Mods` directory in orthographic order by path. More on using multiple configuation files later.

The config file has the following sections. The version number is required. The other sections are all optional:

    {
        "version": 1,
        "rules": {
            ...
        },
        "sounds": {
            ...
        },
        "hooks": [
            ...
        ]
    }

## Defining sounds

The `sounds` section lists all the possible sounds that ZSounds can use for your trains.

    "sounds": {
        "EBell": { "type": "Bell", "filename": "Sounds\\Graham_White_E_Bell.ogg" },
        ...
    }

Sounds are listed by a short name, which must be unique across all configuration files. Each sound must have a `type`, which must come from the following list. The `filename` is interpreted relative to where the configuration file is located. Not all sound types are supported by all locomotives. Some sound types can take options as well. All `pitch` options use `1.0` to indicate the sound should be played unmodified; `2.0` plays it back at double speed (one octave higher), etc.

### Sound types

* Bell (DE6 only)
    - pitch
* HornHit (DE2, DE6)
    - pitch
* HornLoop (DE2, DE6)
    - minPitch: pitch when horn lever is moved the minimum amount.
    - maxPitch: pitch when horn lever is moved fully.
* EngineLoop (DE2, DE6)
    - minPitch: pitch at idle throttle.
    - maxPitch: pitch at maximum throttle.
* EngineShutdown (DE2, DE6)
    - fadeStart: delay in seconds until the EngineLoop begins to fade out.
    - fadeDuration: number of seconds until the EngineLoop reaches 0% volume after the fade out begins.
* EngineStartup (DE2, DE6)
    - fadeStart: delay in seconds until the EngineLoop begins to fade in.
    - fadeDuration: number of seconds until the EngineLoop reaches 100% volume after the fade in begins.
* Whistle (SH282)
    - minPitch: pitch when whistle rope is pulled the minimum amount.
    - maxPitch: pitch when whistle rope is pulled fully.

## Manually assigning sounds

You can force ZSounds to use particular sounds for particular locomotives with the console.

1. Board the locomotive whose sounds you want to change.
2. Open the console. On US keyboards you can do this with the tilde (`~`) key. Other keyboard layouts may use different keys, e.g. the `ö` key.
3. Enter the `zsounds.applysound` command followed by the short name of the sound to use on the locomotive. For example, board an SH-282 locomotive and then type `zsounds.applysound Hancock_3Chime` to change its whistle to that sound.
4. Repeat step 3 to set additional sounds on the same locomotive.
5. Close the console with the same key as used to open it.

To use a sound effect from vanilla Derail Valley, use the `zsounds.applydefaultsound` command, followed by the SoundType to modify. For example, to force an SH-282 to use the vanilla whistle, type the command `zsounds.applydefaultsound Whistle` in the console.

## Defining rules

The `rules` section defines how ZSounds automatically assigns sounds to different locomotives. Whenever a locomotive is spawned, the special rule named `root` is executed, causing other rules to be executed. Along the way, sounds are assigned to the locomotive. ZSounds will remember what sounds were assigned and use them as long as the locomotive exists.

    "rules": {
        "root": { "type": "AllOf", ... },
        "loco_de2": { "type": "AllOf", ...},
        ...
    }

As with sounds, rules here have a short name that must be unique across all configuration files. Every rule has a `type`. Some rules have nested subrules, which do not have names.

### Rule types

* AllOf: Executes every subrule in order.
    - rules (optional): The subrules to execute.
    - sounds (optional): Each name listed here is translated to a Sound rule, which are executed in order after the subrules in `rules`.
* OneOf: Execute one of its subrules, at random.
    - rules: The subrules to choose from.
    - weights (optional): A weight for each rule, affecting how likely ZSounds is to execute that rule. The first weight applies to the first rule in `rules`, and so on. If this field is present it must have the same number of weights as there are rules in `rules`. If this field is missing, all rules are assumed to have equal weight of `1.0`.
* If: Executes its subrule only when a condition is met.
    - property: One of `CarType` or `SkinName`.
    - value: If property is `CarType`, then one of: `LocoShunter`, `LocoSteamHeavy`, or `LocoDiesel`. If property is `SkinName`, then the name of a skin.
    - rule: The subrule to execute if the locomotive's type / applied skin matches `value`.
* Sound: Assigns a sound to the locomotive.
    - name: The short name of a sound.
* Ref: executes a named rule.
    - name: The short name of the rule to execute.

As a convenience, anywhere a subrule appears (in AllOf, OneOf, or If), the name of a rule can appear instead, and that named rule will be executed.

### Example of rule execution

Suppose that a new DE2 locomotive spawns. Here is how ZSounds decides what sounds that locomotive should have.

1. The `root` rule is always executed first.

        "root": {
            "type": "AllOf",
            "rules": [
                ...
            ]
        }

1. It is an `AllOf` rule, so each subrule is executed in order.
1. The first subrule is an `If` rule. The current locomotive is not a LocoDiesel (DE6), so the subrule of the `If` is ignored.

                { "type": "If", "property": "CarType", "value": "LocoDiesel", "rule": { ... } },

1. The second subrule of `root` is also an `If` rule. The current locomotive is a LocoShunter (DE2), the subrule is executed. The subrule of the `If` is a `Ref` rule, telling ZSounds it should execute the `loco_de2` rule immediately.

                { "type": "If", "property": "CarType", "value": "LocoShunter",
                    "rule": { "type": "Ref", "name": "loco_de2" } },

1. The `loco_de2` rule is an `AllOf` rule, so each subrule is executed in order. The first subrule is a reference to the `random_diesel_horn` rule.

        "loco_de2": {
            "type": "AllOf",
            "rules": [ "random_diesel_horn", ... ],

1. The `random_diesel_horn` rule is a `OneOf` rule, so ZSounds randomly selects one of the subrules to execute. There are 2 subrules, and each has a 50% chance to execute. Only one will be executed, and the other ignored.

        "random_diesel_horn": {
            "type": "OneOf",
            "rules": [
                { "type": "AllOf", "sounds": [ "A200_hit", "A200_loop" ] },
                { "type": "AllOf", "sounds": [ "RS3L_hit", "RS3L_loop" ] },
            ]
        },

1. Assume that the first subrule is executed, which is another `AllOf` rule. This rule executed its two subrules, which are `Sound` subrules. Our locomotive is now assigned the `A200_hit` and `A200_loop` sounds. Looking at the definitions for those sounds, they have types `HornHit` and `HornLoop`, respectively.

1. Execution of `random_diesel_horn` is complete, but not all subrules of the `loco_de2` rule have been executed yet, so we continue.

        "loco_de2": {
            "type": "AllOf",
            "rules": [ ..., "random_de2_engine" ],
        },

1. This time we execute the `random_de2_engine` rule. This is another `OneOf` rule, but it specifies a list of `weights`. The first subrule has a weight of 1, and the second a weight of 3, so the second rule is 3 times as likely to execute. In other words, there is a 25% chance that our locomotive will have no subrules applied, keeping the default engine sounds, and a 75% chance that it will have the `OM402LA_startup` and `OM402LA_loop` sounds applied instead. Assume that the 2nd rule is selected.

        "random_de2_engine": {
            "type": "OneOf",
            "rules": [
                { "type": "AllOf" }, // default engine sounds
                { "type": "AllOf", "sounds": [ "OM402LA_startup", "OM402LA_loop", ]},
            ],
            "weights": [ 1, 3 ]
        },

1. Execution of the `random_de2_engine` rule is finished, as so we are now done with the `loco_de2` rule. The `root` rule still has one last subrule to execute. This is an `If` subrule, but the current locomotive is not a LocoSteamHeavy, so it does nothing.

        "root": {
            "type": "AllOf",
            "rules": [
                ...
                { "type": "If", "property": "CarType", "value": "LocoSteamHeavy",
                    "rule": { "type": "Ref", "name": "loco_sh282" } },
            ]
        },


1. The `root` rule is finished, so our DE2 has been assigned 4 sounds, 2 for the horn and  2 more for the engine.

## Adding hooks (for mod authors)

The `hooks` section allows new rules to be added to the evaluation chain, which always starts at the rule named `root`. This allows mod authors to ship additional sets of sounds, or use special sounds for certain skinned locomotives, without the end user needing to modify configuration files.

Here is an example `zsounds-config.json` file (included with ZSounds) that shows how new possibilities can be added to the pool of randomly selected sounds:

    {
        "version": 1,
        "hooks": [
            {
                "type": "AddRule",
                "path": "random_sh282_whistle",
                "rule": { "type": "Sound", "name": "Polar_Express_Whistle" }
            }
        ],
        "sounds": {
            "Polar_Express_Whistle": { "type": "Whistle", "filename": "Polar_Express.ogg" }
        }
    }

This configuration file defines a new sound named `Polar_Express_Whistle`, but no rules in the standard ZSounds configuration file reference this sound, so it will never be used on its own. However, the hook defined in this file adds a new entry to the rule named `random_sh282_whistle` in the default configuration file. This rule is executed whenever a LocoSteamHeavy is spawned.

Hooks are executed after all named rules and sounds have been loaded from all configuration files.

As another example, here is a `zsounds-config.json` that a skin author could include in a DE6 skin. It uses a hook to add a rule that runs after all other rules in the default `loco_de6` rule. Since it runs last, it can override the randomly selected sounds from the default rules. This file also demonstrates that a configuration file included in a skin can define its own named rules as well as sounds and hooks.

    {
        "version": 1,
        "hooks": [
            {
                "type": "AddRule",
                "path": "loco_de6",
                "rule": {
                    "type": "If",
                    "property": "SkinName",
                    "value": "MyCustomSkin",
                    "rule": "loco_de6_MyCustomSkin"
                }
            }
        ],
        "rules": {
            "loco_de6_MyCustomSkin": {
                "type": "AllOf",
                "sounds": [ "MyCustomSkin-horn-hit", "MyCustomSkin-horn-loop" ]
            }
        },
        "sounds": {
            "MyCustomSkin-horn-hit": { "type": "HornHit", "filename": "MyHornHit.ogg" },
            "MyCustomSkin-horn-loop": { "type": "HornLoop", "filename": "MyHornLoop.ogg" }
        }
    }