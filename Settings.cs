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

        // DE2
        public bool shunterStartupEnabled = true;
        public string? shunterStartupSound = "OM402LA_Startup";
        public float shunterFadeInStart = 0.18f;
        public float shunterFadeInDuration = 2f;

        public string? shunterEngineSound = "OM402LA_Loop";
        public float shunterEnginePitch = 1;

        public bool shunterShutdownEnabled = true;
        public string? shunterShutdownSound = null;
        public float shunterFadeOutStart = 0.27f;
        public float shunterFadeOutDuration = 1f;

        public bool shunterHornHitEnabled = true;
        public string? shunterHornHitSound = "Leslie_A200_Hit.ogg";

        public bool shunterHornLoopEnabled = true;
        public string? shunterHornLoopSound = "Leslie_A200_Loop.ogg";
        public float shunterHornPitch = 1;

        // DE6
        public bool dieselStartupEnabled = true;
        public string? dieselStartupSound = "645E3_Startup.ogg";
        public float dieselFadeInStart = 10f;
        public float dieselFadeInDuration = 2f;

        public string? dieselEngineSound = "645E3_idle.ogg";
        public float dieselEnginePitch = 1;

        public bool dieselShutdownEnabled = true;
        public string? dieselShutdownSound = null;
        public float dieselFadeOutStart = 0.27f;
        public float dieselFadeOutDuration = 1f;

        public bool dieselHornHitEnabled = true;
        public string? dieselHornHitSound = "RS3L_start";

        public bool dieselHornLoopEnabled = true;
        public string? dieselHornLoopSound = "RS3L_loop";
        public float dieselHornPitch = 1;

        // SH282
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

        private bool DrawSoundSelector(string label, ref string? sample, ref float pitch)
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

        private bool DrawSoundSelector(string label, ref string? sample, ref bool enabled, ref float pitch)
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

            changed |= UnityModManager.UI.DrawFloatField(ref pitch, "Pitch");

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (changed)
                sample = selected == 0 ? null : SoundLibrary.SoundFiles[selected - 1];
            return changed;
        }

        private bool DrawEngineTransitionSelector(string label, ref string? sample, ref bool enabled, ref float fadeStart, ref float fadeDuration)
        {
            bool changed = false;
            GUILayout.BeginHorizontal();

            GUILayout.Label(label);
            var soundFileOptions = SoundLibrary.SoundFiles.Prepend("(Default)");
            var selected = sample == null ? 0 : Math.Max(soundFileOptions.ToList().IndexOf(sample), 0);
            changed |= UnityModManager.UI.PopupToggleGroup(ref selected, soundFileOptions.ToArray(), label);
            if (changed)
                sample = selected == 0 ? null : SoundLibrary.SoundFiles[selected - 1];

            GUILayout.Label("Enabled");
            var newEnabled = GUILayout.Toggle(enabled, "");
            changed |= newEnabled != enabled;
            enabled = newEnabled;

            UnityModManager.UI.DrawFloatField(ref fadeStart, "Fade start");
            UnityModManager.UI.DrawFloatField(ref fadeDuration, "Fade duration");

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            return changed;
        }

        public void Draw()
        {
            bool changed = false;

            GUILayout.BeginVertical("box");
            changed |= DrawEngineTransitionSelector("DE2 startup", ref shunterStartupSound, ref shunterStartupEnabled, ref shunterFadeInStart, ref shunterFadeInDuration);
            changed |= DrawSoundSelector("DE2 engine", ref shunterEngineSound, ref shunterEnginePitch);
            changed |= DrawEngineTransitionSelector("DE2 shutdown", ref shunterShutdownSound, ref shunterShutdownEnabled, ref shunterFadeOutStart, ref shunterFadeOutDuration);
            changed |= DrawSoundSelector("DE2 horn hit", ref shunterHornHitSound, ref shunterHornHitEnabled);
            changed |= DrawSoundSelector("DE2 horn loop", ref shunterHornLoopSound, ref shunterHornLoopEnabled, ref shunterHornPitch);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            changed |= DrawEngineTransitionSelector("DE6 startup", ref dieselStartupSound, ref dieselStartupEnabled, ref dieselFadeInStart, ref dieselFadeInDuration);
            changed |= DrawSoundSelector("DE6 engine", ref dieselEngineSound, ref dieselEnginePitch);
            changed |= DrawEngineTransitionSelector("DE6 shutdown", ref dieselShutdownSound, ref dieselShutdownEnabled, ref dieselFadeOutStart, ref dieselFadeOutDuration);
            changed |= DrawSoundSelector("DE6 horn hit", ref dieselHornHitSound, ref dieselHornHitEnabled);
            changed |= DrawSoundSelector("DE6 horn loop", ref dieselHornLoopSound, ref dieselHornLoopEnabled, ref dieselHornPitch);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            changed |= DrawSoundSelector("SH282 whistle", ref steamWhistleSound, ref steamWhistlePitch);
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

        private static IEnumerable<string> EnumerateModDirectories()
        {
            return UnityModManager.modEntries
                .Where(mod => mod.Requirements.ContainsKey(Main.mod!.Info.Id))
                .Append(Main.mod!)
                .Select(mod => mod.Path);
        }

        private static IEnumerable<string> EnumerateFiles(string modDirectoryPath)
        {
            return Directory.EnumerateFiles(modDirectoryPath)
                .Where(path => SupportedExtensions.Contains(Path.GetExtension(path).ToLower()))
                .Select(Path.GetFileName);
        }

        private static IEnumerable<string> EnumerateAllFiles()
        {
            return EnumerateModDirectories().SelectMany(EnumerateFiles);
        }

        public static string[] SoundFiles {
            get
            {
                try
                {
                    if (lastCheck + 10 < Time.time)
                    {
                        soundFiles = EnumerateAllFiles().OrderBy(x => x).ToArray();
                        lastCheck = Time.time;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    soundFiles = new string[0];
                }
                return soundFiles;
            }
        }
    }
}