using UnityEngine;

namespace ChatSystem.Models.Audio
{
    [System.Serializable]
    public struct AudioData
    {
        public float[] samples;
        public int channels;
        public int sampleRate;
        public float duration;
        public byte[] rawData;
    }
}