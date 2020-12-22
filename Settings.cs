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

        [Draw("Enable logging")] public bool enableLogging;
        public readonly string? version = Main.mod?.Info.Version;

        public string? dieselEngineSound = null;
        public float dieselEnginePitch = 1;

        public void Draw()
        {
            bool changed = false;

            changed |= UnityModManager.UI.DrawFloatField(ref dieselEnginePitch, "DE6 engine");
            var soundFileOptions = SoundLibrary.SoundFiles.Prepend("(Default)");
            var selected = dieselEngineSound == null ? 0 : Math.Max(soundFileOptions.ToList().IndexOf(dieselEngineSound), 0);
            changed |= UnityModManager.UI.PopupToggleGroup(ref selected, soundFileOptions.ToArray());

            if (changed)
                DieselAudio.ResetAllAudio();
        }

        public override void Save(UnityModManager.ModEntry entry) => Save<Settings>(this, entry);

        public void OnChange()
        {
        }
    }

    public static class SoundLibrary
    {
        private static readonly HashSet<string> SupportedExtensions = new HashSet<string>() { "aif", "ogg", "mp3", "wav" };
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