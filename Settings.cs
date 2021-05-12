using System.IO;
using UnityEngine;
using UnityModManagerNet;

namespace DvMod.ZSounds
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        public string? version;

        [Draw("Enable logging")]
        public bool enableLogging;

        public Settings()
        {
        }

        public Settings(string? version)
        {
            this.version = version;
        }

        public void Draw()
        {
            this.Draw(Main.mod);
            if (GUILayout.Button("Open configuration file"))
                System.Diagnostics.Process.Start(Path.Combine(Main.mod!.Path, "zsounds-config.json"));
        }

        public void OnChange() {}

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}