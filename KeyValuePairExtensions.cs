using System.Collections.Generic;

namespace DvMod.ZSounds
{
    public static class DictionaryExtensions
    {
        public static void Deconstruct<K, V>(this KeyValuePair<K, V> kvp, out K key, out V value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }
    }
}