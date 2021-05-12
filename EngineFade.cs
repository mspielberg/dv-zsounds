using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace DvMod.ZSounds
{
    public static class EngineFade
    {
        public class Settings
        {
            public float fadeInStart;
            public float fadeOutStart;
            public float fadeInDuration = 2f;
            public float fadeOutDuration = 1f;
        }

        private static readonly Dictionary<TrainCar, Settings> settings = new Dictionary<TrainCar, Settings>();

        private static Settings GetDefaultSettings(TrainCar car)
        {
            if (car.carType == TrainCarType.LocoShunter)
            {
                var audio = car.GetComponent<LocoAudioShunter>();
                return new Settings
                {
                    fadeInStart = audio.engineOnClip.length * 0.15f,
                    fadeOutStart = audio.engineOffClip.length * 0.10f,
                };
            }
            else
            {
                var audio = car.GetComponent<LocoAudioDiesel>();
                return new Settings
                {
                    fadeInStart = audio.engineOnClip.length * 0.15f,
                    fadeOutStart = audio.engineOffClip.length * 0.10f,
                };
            }
        }

        private static Settings GetSettings(LocoTrainAudio audio)
        {
            var car = TrainCar.Resolve(audio.gameObject);
            return settings.ContainsKey(car) ? settings[car] : GetDefaultSettings(car);
        }

        public static float GetFadeInStart(LocoTrainAudio audio) => GetSettings(audio).fadeInStart;
        public static float GetFadeOutStart(LocoTrainAudio audio) => GetSettings(audio).fadeOutStart;
        public static float GetFadeInDuration(LocoTrainAudio audio) => GetSettings(audio).fadeInDuration;
        public static float GetFadeOutDuration(LocoTrainAudio audio) => GetSettings(audio).fadeOutDuration;

        public static void SetFadeSettings(TrainCar car, Settings fadeSettings) => settings[car] = fadeSettings;

        [HarmonyPatch]
        public static class EngineAudioHandlePatch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var iter = instructions.GetEnumerator();
                IEnumerable<CodeInstruction> ReplaceNext(float value, string methodName)
                {
                    while (iter.MoveNext())
                    {
                        if (iter.Current.Is(OpCodes.Ldc_R4, value))
                        {
                            yield return new CodeInstruction(OpCodes.Ldarg_0) { labels = iter.Current.labels };
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EngineFade), methodName));
                            yield break;
                        }
                        yield return iter.Current;
                    }
                }
                foreach (var inst in ReplaceNext(0.1f, nameof(EngineFade.GetFadeInStart))) yield return inst;
                foreach (var inst in ReplaceNext(0.15f, nameof(EngineFade.GetFadeOutStart))) yield return inst;
                foreach (var inst in ReplaceNext(1f, nameof(EngineFade.GetFadeInDuration))) yield return inst;
                foreach (var inst in ReplaceNext(2f, nameof(EngineFade.GetFadeOutDuration))) yield return inst;
                while (iter.MoveNext())
                    yield return iter.Current;
            }

            public static IEnumerable<MethodBase> TargetMethods()
            {
                return typeof(LocoAudioDiesel).Assembly.GetTypes()
                    .Where(t => t.FullName.Contains("+<EngineAudioHandle>"))
                    .Select(t => t.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic));
            }
        }
    }
}