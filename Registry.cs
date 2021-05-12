using DvMod.ZSounds.Config;
using HarmonyLib;
using Newtonsoft.Json;
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
                SaveGameManager.data.SetJObject(typeof(Registry).FullName, obj);
            }
        }

        [HarmonyPatch(typeof(SaveGameManager), nameof(SaveGameManager.Load))]
        public static class LoadPatch
        {
            public static void Postfix()
            {
                var saved = SaveGameManager.data.GetJObject(typeof(Registry).FullName);
                if (saved == null)
                    return;

                foreach (var (guid, soundsObj) in saved)
                {
                    if (soundsObj is JObject obj)
                    {
                        var soundSet = new SoundSet();
                        foreach (var (typeStr, soundName) in obj)
                        {
                            if (Config.Config.Active!.sounds.TryGetValue(soundName.Value<string>(), out var soundDefinition))
                                soundSet.sounds[Config.Util.ParseEnum<SoundType>(typeStr)] = soundDefinition;
                        }
                        if (soundSet.sounds.Count > 0)
                            soundSets[guid] = soundSet;
                    }
                }
            }
        }
    }
}