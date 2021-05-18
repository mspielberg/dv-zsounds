using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace DvMod.ZSounds
{
    public static class BogieAudio
    {
        [HarmonyPatch(typeof(TrainAudio), nameof(TrainAudio.AudioLODCheckup))]
        public static class AudioLODCheckupPatch
        {
            public static bool Prefix(TrainAudio __instance, ref IEnumerator __result)
            {
                __result = Coro(__instance);
                return false;
            }

            private static IEnumerator Coro(TrainAudio __instance)
            {
                while (true)
                {
                    var car = __instance.Car;
                    if (Mathf.Abs(car.GetForwardSpeed()) < 0.1f)
                        __instance.SetBogiesAudioLOD(AudioLOD.NONE);
                    else if (Vector3.Distance(car.transform.position, PlayerManager.PlayerTransform.position) < 50f)
                        __instance.SetBogiesAudioLOD(AudioLOD.DETAILED);
                    else
                        __instance.SetBogiesAudioLOD(AudioLOD.SIMPLE);
                    yield return WaitFor.SecondsRealtime(1f);
                }
            }
        }
    }
}