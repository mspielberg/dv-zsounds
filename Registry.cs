using DvMod.ZSounds.Config;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace DvMod.ZSounds
{
    public static class Registry
    {
        public static readonly Dictionary<string, SoundSet> soundSets = new Dictionary<string, SoundSet>();
        public static SoundSet Get(TrainCar car)
        {
            if (!soundSets.TryGetValue(car.logicCar.carGuid, out var soundSet))
            {
                soundSets[car.logicCar.carGuid] = soundSet = Config.Config.Active!.Apply(car);
            }
            return soundSet;
        }

        public static void LoadFromSaveManager()
        {
            var saved = SaveGameManager.Instance.data?.GetJObject(typeof(Registry).FullName);
            if (saved == null)
                return;

            foreach (var (guid, soundsObj) in saved)
            {
                if (soundsObj != null)
                {
                    var soundSet = LoadSoundSet(guid, (JObject) soundsObj);
                    if (soundSet != null)
                        soundSets[guid] = soundSet;
                }
            }
        }

        private static SoundSet? LoadSoundSet(string carGuid, JObject soundsObj)
        {
            var soundSet = new SoundSet();
            foreach (var (key, value) in soundsObj)
            {
                var soundType = Util.ParseEnum<SoundType>(key);

                if (value == null || value.Type != JTokenType.String)
                {
                    Main.DebugLog(() => $"Unable to load saved sound state for car GUID {carGuid}");
                    return null;
                }
                var soundName = value.StrictValue<string>();

                if (Config.Config.Active!.sounds.TryGetValue(soundName, out var soundDefinition))
                    soundSet.sounds[soundType] = soundDefinition;
            }
            return soundSet;
        }

        [HarmonyPatch(typeof(SaveGameManager), nameof(SaveGameManager.Save))]
        public static class SavePatch
        {
            public static void Prefix()
            {
                var obj = new JObject();
                foreach (var (guid, soundSet) in soundSets)
                {
                    if (soundSet.sounds.Count > 0)
                        obj[guid] = soundSet.ToJson();
                }
                SaveGameManager.Instance.data.SetJObject(typeof(Registry).FullName, obj);
            }
        }

        [HarmonyPatch(typeof(SaveGameManager), nameof(SaveGameManager.FindStartGameData))]
        public static class LoadPatch
        {
            public static void Postfix()
            {
                LoadFromSaveManager();
            }
        }
    }
}