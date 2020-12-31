using DV.CabControls;
using DV.CabControls.Spec;
using HarmonyLib;
using UnityEngine;

namespace DvMod.ZSounds
{
    public static class AdjustCabControls
    {
        [HarmonyPatch(typeof(ControlsInstantiator), nameof(ControlsInstantiator.Spawn))]
        public static class SpawnPatch
        {
            public static void Prefix(ControlSpec spec)
            {
                if (spec.name == "C horn" && spec is Lever leverSpec)
                {
                    if (leverSpec.jointDamper == 3) // DE6
                        leverSpec.jointDamper = 4.5f;
                }
            }
        }
    }
}