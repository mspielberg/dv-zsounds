using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace DvMod.ZSounds
{
    /*
    public static class HornQuilling
    {
        private static readonly Dictionary<Horn, float> lastReset = new Dictionary<Horn, float>();

        [HarmonyPatch(typeof(Horn), nameof(Horn.InitializeAudio))]
        public static class HornInitializeAudioPatch
        {
            public static void Postfix(Horn __instance)
            {
                foreach (var layer in __instance.loopLayered.layers)
                    layer.inertialPitch = false;
            }
        }

        [HarmonyPatch(typeof(Horn), nameof(Horn.Update))]
        public static class HornUpdatePatch
        {
            public static bool Prefix(Horn __instance)
            {
                if (__instance.input < 0.1f)
                    lastReset[__instance] = Time.time;
                else if (!__instance.hitPlayed && __instance.input >= 0.9f && Time.time - lastReset[__instance] < 0.5f && __instance.hit != null)
                {
                    __instance.hit.Play();
                    __instance.hitPlayed = true;
                }
                __instance.loopLayered.Set(__instance.input);
                return false;
            }
        }
    }
    */
}