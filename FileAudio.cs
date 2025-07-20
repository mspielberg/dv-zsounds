using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace DvMod.ZSounds
{
    public class ConfigException : Exception
    {
        public ConfigException(string message) : base(message) { }
    }

    public static class FileAudio
    {
        private static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        public static AudioClip Load(string path)
        {
            if (path.Length == 0)
                return Silent;
            if (cache.TryGetValue(path, out var clip))
                return clip;
            Main.DebugLog(() => $"Loading {path}");
            var extension = Path.GetExtension(path);
            if (!AudioTypes.ContainsKey(extension))
                throw new ConfigException($"Unsupported file extension for sound file: \"{path}\"");
            
            // Check if file exists before attempting to load
            if (!File.Exists(path))
                throw new ConfigException($"Sound file not found: \"{path}\"");
            
            var audioType = AudioTypes[Path.GetExtension(path)];
            var webRequest = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, audioType);
            var async = webRequest.SendWebRequest();
            while (!async.isDone)
            {
            }
            
            // Check for web request errors before accessing the audio clip
            if (webRequest.isNetworkError || webRequest.isHttpError)
            {
                var error = $"Failed to load audio file \"{path}\": {webRequest.error}";
                webRequest.Dispose();
                throw new ConfigException(error);
            }
            
            clip = DownloadHandlerAudioClip.GetContent(webRequest);
            webRequest.Dispose(); // Clean up the web request
            
            if (clip == null)
                throw new ConfigException($"Failed to extract audio clip from file: \"{path}\"");
            
            // Set the clip name to the filename for easier debugging
            clip.name = Path.GetFileNameWithoutExtension(path);
            
            cache[path] = clip;
            return clip;
        }

        private static readonly Dictionary<string, AudioType> AudioTypes = new Dictionary<string, AudioType>()
        {
            {".ogg", AudioType.OGGVORBIS},
            {".wav", AudioType.WAV},
        };

        public static AudioClip Silent = AudioClip.Create("silent", 1, 1, 44100, false);
    }
}