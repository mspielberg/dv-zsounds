using Newtonsoft.Json.Linq;

namespace DvMod.ZSounds.Config
{
    public static class Util
    {
        public static T ParseEnum<T>(JToken token)
        {
            return (T)System.Enum.Parse(typeof(T), token.Value<string>(), ignoreCase: true);
        }
    }
}
