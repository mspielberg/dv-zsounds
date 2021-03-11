using System.Linq;
using UnityEngine;

namespace DvMod.ZSounds
{
    public static class UnityObjectExtensions
    {
        public static string GetPath(this Component c)
        {
            return string.Join("/", c.GetComponentsInParent<Transform>(true).Reverse().Select(c => c.name));
        }

        public static string DumpHierarchy(this GameObject gameObject)
        {
            return string.Join("\n",
                gameObject.GetComponentsInChildren<Component>().Select(c => $"{GetPath(c)} {c.GetType()} {c.transform.localPosition}"));
        }
    }
}