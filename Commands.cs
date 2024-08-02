using CommandTerminal;
using DV.ThingTypes;
using HarmonyLib;
using System;
using System.Linq;

namespace DvMod.ZSounds
{
    public static class Commands
    {
        [HarmonyPatch(typeof(CommandShell), nameof(CommandShell.RegisterCommands))]
        public static class RegisterCommandsPatch
        {
            public static void Postfix()
            {
                Register();
            }
        }

        private static void Register(string name, Action<CommandArg[]> proc)
        {
            name = Main.mod!.Info.Id + "." + name;
            if (Terminal.Shell == null)
                return;
            if (Terminal.Shell.Commands.Remove(name.ToUpper()))
                Main.DebugLog(() => $"replacing existing command {name}");
            CommandInfo commandInfo = Terminal.Shell.AddCommand(name, proc);
            if (Terminal.Autocomplete.known_words.ContainsKey(commandInfo.name.ToLower()))
                Terminal.Autocomplete.known_words.Remove(commandInfo.name.ToLower());
            Terminal.Autocomplete.Register(commandInfo);
        }

        public static void Register()
        {
            Register("reloadconfig", _ =>
            {
                Config.Config.LoadAll();
                Terminal.Log($"Reloaded configuration:\n{Config.Config.Active}");
            });

            Register("applysound", args =>
            {
                if (args.Length < 1)
                {
                    Terminal.Log("usage: zsounds.applysound <soundname>");
                    return;
                }
                var car = PlayerManager.Car;
                var soundSet = Registry.Get(car);
                if (car == null || !CarTypes.IsLocomotive(car.carLivery))
                {
                    Terminal.Log("Car must be locomotive");
                    return;
                }
                var sounds = Config.Config.Active!.sounds;
                if (!sounds.TryGetValue(args[0].String, out var soundDefinition))
                {
                    Terminal.Log($"Unknown sound name: {args[0].String}\nKnown sound names: {string.Join(", ", sounds.Keys.OrderBy(x => x))}");
                    return;
                }
                soundDefinition.Apply(soundSet);
                SpawnPatches.ApplyAudio(car);
                Terminal.Log(Registry.Get(car).ToString());
            });

            Register("applydefaultsound", args =>
            {
                if (args.Length < 1)
                {
                    Terminal.Log("usage: zsounds.applydefaultsound <soundtype>");
                    return;
                }
                var car = PlayerManager.Car;
                var soundSet = Registry.Get(car);
                if (car == null || !CarTypes.IsLocomotive(car.carLivery))
                {
                    Terminal.Log("Car must be locomotive");
                    return;
                }
                if (!Enum.TryParse<Config.SoundType>(args[0].String, ignoreCase: true, out var soundType))
                {
                    Terminal.Log($"unknown sound type: {args[0].String}");
                    return;
                }
                soundSet.sounds.Remove(soundType);
                SpawnPatches.ApplyAudio(car);
                Terminal.Log(Registry.Get(car).ToString());
            });

            Register("getcarsounds", _ =>
            {
                var car = PlayerManager.Car;
                if (car == null)
                    return;
                var soundSet = Registry.Get(car);
                Terminal.Log(soundSet.ToString());
            });

            Register("resetcarsounds", _ =>
            {
                var car = PlayerManager.Car;
                if (car == null)
                    return;
                Registry.soundSets.Remove(car.CarGUID);
                SpawnPatches.ApplyAudio(car);
                Terminal.Log(Registry.Get(car).ToString());
            });
        }
    }
}
