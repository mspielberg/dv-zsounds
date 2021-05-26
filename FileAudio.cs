using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace DvMod.ZSounds
{
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
            var audioType = AudioTypes[Path.GetExtension(path)];
            var webRequest = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, audioType);
            var async = webRequest.SendWebRequest();
            while (!async.isDone)
            {
            }
            clip = DownloadHandlerAudioClip.GetContent(webRequest);
            cache[path] = clip;
            return clip;
        }

        private static readonly Dictionary<string, AudioType> AudioTypes = new Dictionary<string, AudioType>()
        {
            {".aif", AudioType.AIFF},
            {".aiff", AudioType.AIFF},
            {".mp3", AudioType.MPEG},
            {".ogg", AudioType.OGGVORBIS},
            {".wav", AudioType.WAV},
        };

        public static AudioClip Silent = AudioClip.Create("silent", 1, 1, 44100, false);
    }
}