using CommandTerminal;
using DV.PointSet;
using DV.Signs;
using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;

namespace DvMod.ZSounds
{
    public static class Commands
    {
        [HarmonyPatch(typeof(Terminal), nameof(Terminal.Start))]
        public static class RegisterCommandsPatch
        {
            public static void Postfix()
            {
                Register();
            }
        }

        private static void Register(string name, Action<CommandArg[]> proc)
        {
            if (Terminal.Shell == null)
                return;
            if (Terminal.Shell.Commands.Remove(name.ToUpper()))
                Main.DebugLog(() => $"replacing existing command {name}");
            else
                Terminal.Autocomplete.Register(name);
            Terminal.Shell.AddCommand(name, proc);
        }

        public static void Register()
        {
            Register("zsounds.dumpCar", _ =>
            {
                foreach (var component in PlayerManager.Car.GetComponents<Component>())
                    Terminal.Log(component.GetType().Name);
            });

            Register("zsounds.dumpDieselAudio", _ =>
            {
                foreach (var component in Component.FindObjectsOfType<LocoAudioDiesel>())
                    Terminal.Log(GetPath(component));
            });

            Register("zsounds.resetDieselAudio", _ =>
            {
                if (PlayerManager.Car?.carType != TrainCarType.LocoDiesel)
                    return;
                DieselAudio.ResetAudio(PlayerManager.Car.GetComponentInChildren<LocoAudioDiesel>());
            });

            Register("zsounds.showAudioState", _ =>
            {
                foreach (var source in PlayerManager.Car.GetComponentsInChildren<AudioSource>())
                {
                    Terminal.Log($"{GetPath(source)} {source.name}: pitch={source.pitch}, volume={source.volume}");
                }
            });
        }

        private static string GetPath(Component c)
        {
            return string.Join("/", c.GetComponentsInParent<Transform>(true).Reverse().Select(c => c.name));
        }
    }
}
