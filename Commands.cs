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
                Terminal.Log(PlayerManager.Car.gameObject.DumpHierarchy());
            });

            Register("zsounds.dumpDieselAudio", _ =>
            {
                if (PlayerManager.Car == null || PlayerManager.Car.carType != TrainCarType.LocoDiesel)
                    return;
                var audio = PlayerManager.Car.transform.GetComponentInChildren<LocoAudioDiesel>();
                Terminal.Log($"engineOn.Length = {audio.engineOnClip.length}");
                Terminal.Log($"engineOff.Length = {audio.engineOffClip.length}");
            });

            Register("zsounds.dumpShunterAudio", _ =>
            {
                if (PlayerManager.Car == null || PlayerManager.Car.carType != TrainCarType.LocoShunter)
                    return;
                var audio = PlayerManager.Car.transform.GetComponentInChildren<LocoAudioShunter>();
                Terminal.Log($"engineOn.Length = {audio.engineOnClip.length}");
                Terminal.Log($"engineOff.Length = {audio.engineOffClip.length}");
            });

            Register("zsounds.dumpSteamAudio", _ =>
            {
                foreach (var component in Component.FindObjectsOfType<LocoAudioSteam>())
                    Terminal.Log(GetPath(component));
            });

            Register("zsounds.resetDieselAudio", _ =>
            {
                if (PlayerManager.Car?.carType != TrainCarType.LocoDiesel)
                    return;
                DieselAudio.ResetAudio(PlayerManager.Car.GetComponentInChildren<LocoAudioDiesel>());
            });

            Register("zsounds.dumpAudioSources", _ =>
            {
                if (PlayerManager.Car == null)
                    return;
                foreach (var source in PlayerManager.Car.GetComponentsInChildren<AudioSource>())
                {
                    Terminal.Log(source.GetPath());
                    Terminal.Log(source.DumpFields());
                }
            });

            Register("zsounds.dumpHornAudioSource", _ =>
            {
                Terminal.Log(PlayerManager.Car.GetComponent<Horn>().hit.GetPath());
            });

            Register("zsounds.showAudioState", _ =>
            {
                foreach (var source in PlayerManager.Car.GetComponentsInChildren<AudioSource>())
                {
                    Terminal.Log($"{GetPath(source)} {source.name}: pitch={source.pitch}, volume={source.volume}");
                }
            });

            Register("zsounds.dumpInteriorPath", args =>
            {
                if (PlayerManager.Car == null)
                    return;
                var path = string.Join(" ", args.Select(a => a.String));
                var transform = PlayerManager.Car.loadedInterior.transform.Find(path);
                Terminal.Log(transform == null ? "(null)" : GetPath(transform));
            });

            Register("zsounds.toggleInteriorLamp", args =>
            {
                if (PlayerManager.Car?.loadedInterior == null)
                    return;
                var name = string.Join(" ", args.Select(a => a.String));
                foreach (var lampControl in PlayerManager.Car.loadedInterior.GetComponentsInChildren<LampControl>())
                {
                    if (lampControl.name == name)
                    {
                        if (lampControl.lampState == LampControl.LampState.On)
                            lampControl.SetLampState(LampControl.LampState.Off);
                        else
                            lampControl.SetLampState(LampControl.LampState.On);
                    }
                }
            });

            Register("zsounds.dumpInteriorLamp", args =>
            {
                if (PlayerManager.Car?.loadedInterior == null)
                    return;
                var name = string.Join(" ", args.Select(a => a.String));
                foreach (var lampControl in PlayerManager.Car.loadedInterior.GetComponentsInChildren<LampControl>())
                {
                    if (lampControl.name == name)
                    {
                        Terminal.Log($"lampInd={lampControl.lampInd.GetPath()}");
                    }
                }
            });
        }

        private static string GetPath(Component c)
        {
            return string.Join("/", c.GetComponentsInParent<Transform>(true).Reverse().Select(c => c.name));
        }
    }
}
