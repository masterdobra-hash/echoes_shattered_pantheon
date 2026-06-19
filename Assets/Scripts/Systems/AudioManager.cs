using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Systems
{
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _i;
        private AudioSource _sfx;
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();

        public static void EnsureExists()
        {
            if (_i != null) return;
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            _i = go.AddComponent<AudioManager>();
            _i._sfx = go.AddComponent<AudioSource>();
        }

        public static void Play(string clipName)
        {
            EnsureExists();
            if (!_i._clips.TryGetValue(clipName, out var c))
            {
                c = Resources.Load<AudioClip>("Audio/" + clipName);
                _i._clips[clipName] = c;
            }
            if (c != null) _i._sfx.PlayOneShot(c);
        }
    }
}
