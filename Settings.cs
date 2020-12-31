using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace DvMod.ZSounds
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        public bool enableLogging;
        public readonly string? version = Main.mod?.Info.Version;

        public bool shunterStartupEnabled = true;
        public string? shunterStartupSound = null;
        public string? shunterEngineSound = null;
        public float shunterEnginePitch = 1;
        public bool shunterShutdownEnabled = true;
        public string? shunterShutdownSound = null;
        public bool shunterHornHitEnabled = true;
        public string? shunterHornHitSound = null;
        public string? shunterHornLoopSound = null;
        public float shunterHornPitch = 1;

        public bool dieselStartupEnabled = true;
        public string? dieselStartupSound = "EMD_SD70ACe_Startup.ogg";
        public string? dieselEngineSound = "EMD_567C.ogg";
        public float dieselEnginePitch = 1;
        public bool dieselShutdownEnabled = true;
        public string? dieselShutdownSound = null;
        public bool dieselHornHitEnabled = true;
        public string? dieselHornHitSound = "Leslie_A200_Hit.ogg";
        public string? dieselHornLoopSound = "Leslie_A200_Loop.ogg";
        public float dieselHornPitch = 1;

        public string? steamWhistleSound = "Manns_Creek_3_Chime.ogg";
        public float steamWhistlePitch = 1;

        private bool DrawSoundSelector(string label, ref string? sample, ref bool enabled)
        {
            bool changed = false;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label);
            var soundFileOptions = SoundLibrary.SoundFiles.Prepend("(Default)");
            var selected = sample == null ? 0 : Math.Max(soundFileOptions.ToList().IndexOf(sample), 0);
            changed |= UnityModManager.UI.PopupToggleGroup(ref selected, soundFileOptions.ToArray(), label);
            GUILayout.Label("Enabled");
            var newEnabled = GUILayout.Toggle(enabled, "");
            changed |= newEnabled != enabled;
            enabled = newEnabled;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (changed)
                sample = selected == 0 ? null : SoundLibrary.SoundFiles[selected - 1];
            return changed;
        }

        private bool DrawSoundSelector(string label, ref float pitch, ref string? sample)
        {
            bool changed = false;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label);
            var soundFileOptions = SoundLibrary.SoundFiles.Prepend("(Default)");
            var selected = sample == null ? 0 : Math.Max(soundFileOptions.ToList().IndexOf(sample), 0);
            changed |= UnityModManager.UI.PopupToggleGroup(ref selected, soundFileOptions.ToArray(), label);
            changed |= UnityModManager.UI.DrawFloatField(ref pitch, "Pitch");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (changed)
                sample = selected == 0 ? null : SoundLibrary.SoundFiles[selected - 1];
            return changed;
        }

        public void Draw()
        {
            bool changed = false;

            GUILayout.BeginVertical("box");
            changed |= DrawSoundSelector("DE2 startup", ref shunterStartupSound, ref shunterStartupEnabled);
            changed |= DrawSoundSelector("DE2 engine", ref shunterEnginePitch, ref shunterEngineSound);
            changed |= DrawSoundSelector("DE2 shutdown", ref shunterShutdownSound, ref shunterShutdownEnabled);
            changed |= DrawSoundSelector("DE2 horn hit", ref shunterHornHitSound, ref shunterHornHitEnabled);
            changed |= DrawSoundSelector("DE2 horn loop", ref shunterHornPitch, ref shunterHornLoopSound);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            changed |= DrawSoundSelector("DE6 startup", ref dieselStartupSound, ref dieselStartupEnabled);
            changed |= DrawSoundSelector("DE6 engine", ref dieselEnginePitch, ref dieselEngineSound);
            changed |= DrawSoundSelector("DE6 shutdown", ref dieselShutdownSound, ref dieselShutdownEnabled);
            changed |= DrawSoundSelector("DE6 horn hit", ref dieselHornHitSound, ref dieselHornHitEnabled);
            changed |= DrawSoundSelector("DE6 horn loop", ref dieselHornPitch, ref dieselHornLoopSound);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            changed |= DrawSoundSelector("SH282 whistle", ref steamWhistlePitch, ref steamWhistleSound);
            GUILayout.EndVertical();

            if (changed)
            {
                DieselAudio.ResetAllAudio();
                ShunterAudio.ResetAllAudio();
                SteamAudio.ResetAllAudio();
            }
            enableLogging = GUILayout.Toggle(enableLogging, "Enable logging");
        }

        public override void Save(UnityModManager.ModEntry entry) => Save<Settings>(this, entry);

        public void OnChange()
        {
        }
    }

    public static class SoundLibrary
    {
        private static readonly HashSet<string> SupportedExtensions = new HashSet<string>() { ".aif", ".mp3", ".ogg", ".wav" };
        private static float lastCheck = 0;
        private static string[] soundFiles = new string[0];
        public static string[] SoundFiles {
            get
            {
                if (lastCheck + 10 < Time.time)
                {
                    soundFiles = Directory.EnumerateFiles(Main.mod?.Path)
                        .Where(path => SupportedExtensions.Contains(Path.GetExtension(path).ToLower()))
                        .Select(Path.GetFileName)
                        .OrderBy(x => x)
                        .ToArray();
                    lastCheck = Time.time;
                }
                return soundFiles;
            }
        }
    }
}