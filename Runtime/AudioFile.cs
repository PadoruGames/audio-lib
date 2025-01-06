using Padoru.Core.Utils;
using UnityEngine;
using UnityEngine.Audio;

namespace Padoru.Audio
{
    [System.Serializable]
    public class AudioFile
    {
        public AudioClip Clip;
        public AudioMixerGroup Mixer;
        [Range(0, 1)]
        public float Volume;
        public bool PlayOnAwake;
        public bool Loop;
        public bool Disabled;
        public bool ShiftPitch;
        public MinMax MinMaxPitch;
    }
}
