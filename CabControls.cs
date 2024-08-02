using DV.CabControls;
using DV.CabControls.Spec;
using DV.ThingTypes;
using HarmonyLib;
using System.Collections;
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
                if (spec.name == "C horn" && spec is Lever leverSpec && TrainCar.Resolve(spec.gameObject).carType == TrainCarType.LocoDiesel)
                    leverSpec.jointLimitMin = 0;
            }
        }

        // [HarmonyPatch(typeof(DieselDashboardControls), nameof(DieselDashboardControls.Init))]
        // public static class DieselDashboardControlsInitPatch
        // {
        //     public static void Postfix(DieselDashboardControls __instance)
        //     {
        //         __instance.StartCoroutine(ResetHornCallback(__instance));
        //     }

        //     private static IEnumerator ResetHornCallback(DieselDashboardControls __instance)
        //     {
        //         while ((__instance.hornControl?.ValueChanged?.GetInvocationList()?.Length ?? 0) == 0)
        //             yield return null;

        //         var hornControl = __instance.hornControl!;
        //         hornControl.ValueChanged = null;
        //         hornControl.ValueChanged += e => __instance.horn.SetInput(e.newValue);
        //     }
        // }
    }
}