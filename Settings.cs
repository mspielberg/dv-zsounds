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

        public string? shunterStartupSound = null;
        public string? shunterEngineSound = null;
        public float shunterEnginePitch = 1;
        public string? shunterShutdownSound = null;

        public string? dieselStartupSound = null;
        public string? dieselEngineSound = "EMD_567C.ogg";
        public float dieselEnginePitch = 1;
        public string? dieselShutdownSound = null;

        public string? steamWhistleSound = "Manns_Creek_3_Chime.ogg";
        public float steamWhistlePitch = 1;

        private bool DrawSoundSelector(string label, ref string? sample)
        {
            bool changed = false;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label);
            var soundFileOptions = SoundLibrary.SoundFiles.Prepend("(Default)");
            var selected = sample == null ? 0 : Math.Max(soundFileOptions.ToList().IndexOf(sample), 0);
            changed |= UnityModManager.UI.PopupToggleGroup(ref selected, soundFileOptions.ToArray(), label);
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

            changed |= DrawSoundSelector("DE2 startup", ref shunterStartupSound);
            changed |= DrawSoundSelector("DE2 engine", ref shunterEnginePitch, ref shunterEngineSound);
            changed |= DrawSoundSelector("DE2 shutdown", ref shunterShutdownSound);

            changed |= DrawSoundSelector("DE6 startup", ref dieselStartupSound);
            changed |= DrawSoundSelector("DE6 engine", ref dieselEnginePitch, ref dieselEngineSound);
            changed |= DrawSoundSelector("DE6 shutdown", ref dieselShutdownSound);

            changed |= DrawSoundSelector("SH282 whistle", ref steamWhistlePitch, ref steamWhistleSound);

            if (changed)
            {
                DieselAudio.ResetAllAudio();
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