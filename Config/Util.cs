using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace DvMod.ZSounds.Config
{
    public static class Util
    {
        public static T ParseEnum<T>(JToken token)
                where T : struct, Enum
        {
            if (token.Type != JTokenType.String)
                throw new ConfigException($"Unexpected {token.Type} at path {token.Path}. Expected JSON string.");

            if (Enum.TryParse((string) token!, out T result))
                return result;

            var acceptable = string.Join(", ", Enum.GetValues(typeof(T)));
            throw new ConfigException(
                $"Unexpected value {token} at path {token.Path}. Expected one of: {acceptable}");
        }

        public static JObject EnsureJObject(this JToken token)
        {
            if (token is JObject jObject)
                return jObject;
            throw new ConfigException(
                $"Unexpected {token.Type} at path {token.Path}. Expected JSON Object.");
        }

        private static T Extract<T>(JToken token, JTokenType expectedTokenType)
        {
            if (token.Type == expectedTokenType)
                return token.ToObject<T>()!;
            throw new ConfigException(
                $"Unexpected {token.Type} {token} at path {token.Path}. Expected integer.");
        }

        public static T ExtractChild<T>(this JObject jObject, string key)
        {
            if (jObject.TryGetValue(key, out var child))
                return Extract<T>(child, ExpectedTokenType<T>());
            throw new ConfigException($"Expected child named '{key}' at {jObject.Path}");
        }

        public static T ExtractChildOrEmpty<T>(this JObject jObject, string key)
            where T : JContainer, new()
        {
            if (jObject.TryGetValue(key, out var child))
                return Extract<T>(child, ExpectedTokenType<T>());
            return new T();
        }

        private static Dictionary<Type, JTokenType> typeMap = new()
        {
            { typeof(JArray), JTokenType.Array },
            { typeof(JObject), JTokenType.Object },

            { typeof(int), JTokenType.Integer },
            { typeof(string), JTokenType.String },
        };

        private static JTokenType ExpectedTokenType<T>()
        {
            if (typeof(T).IsEnum)
                return JTokenType.String;
            return typeMap[typeof(T)];
        }

        private static Dictionary<Type, HashSet<JTokenType>> allowedTokenTypes = new()
        {
            { typeof(float), new() { JTokenType.Float, JTokenType.Integer } },
            { typeof(int), new() { JTokenType.Integer } },
            { typeof(string), new() { JTokenType.String } },
        };

        private static HashSet<JTokenType> AllowedTokenTypes<T>() => allowedTokenTypes[typeof(T)];

        public static T StrictValue<T>(this JToken token)
        {
            if (AllowedTokenTypes<T>().Contains(token.Type))
                return token.ToObject<T>()!;
            throw new ConfigException(
                $"Unexpected {token.Type} at path {token.Path}. Expected JSON {ExpectedTokenType<T>()}.");
        }
    }
}
