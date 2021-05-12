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

        public void OnChange() {}
    }
}