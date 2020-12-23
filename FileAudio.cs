using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace DvMod.ZSounds
{
    public static class FileAudio
    {
        public static AudioClip Load(string name)
        {
            var path = Path.Combine(Main.mod?.Path, name);
            var audioType = AudioTypes[Path.GetExtension(path)];
            var webRequest = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, audioType);
            var async = webRequest.SendWebRequest();
            while (!async.isDone)
            {
            }
            return DownloadHandlerAudioClip.GetContent(webRequest);
        }

        private static readonly Dictionary<string, AudioType> AudioTypes = new Dictionary<string, AudioType>()
        {
            {".aif", AudioType.AIFF},
            {".aiff", AudioType.AIFF},
            {".mp3", AudioType.MPEG},
            {".ogg", AudioType.OGGVORBIS},
            {".wav", AudioType.WAV},
        };
    }
}