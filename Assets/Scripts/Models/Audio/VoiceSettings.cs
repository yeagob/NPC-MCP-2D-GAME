using UnityEngine;
using ChatSystem.Enums;

namespace ChatSystem.Models.Audio
{
    [System.Serializable]
    public class VoiceSettings
    {
        [Header("Audio Configuration")]
        public AudioFormat inputFormat = AudioFormat.PCM16;
        public AudioFormat outputFormat = AudioFormat.PCM16;
        public int sampleRate = 24000;
        public int channels = 1;
        
        [Header("Voice Activity Detection")]
        public bool enableVAD = true;
        public float vadThreshold = 0.5f;
        public float silenceTimeoutMs = 1000f;
        
        [Header("Real-Time Settings")]
        public bool enableRealTimeTools = true;
        public int maxConcurrentToolCalls = 3;
        public float audioChunkSizeMs = 100f;
        
        [Header("Quality Settings")]
        [Range(0.1f, 2.0f)]
        public float playbackSpeed = 1.0f;
        [Range(0.0f, 1.0f)]
        public float inputGain = 1.0f;
        [Range(0.0f, 1.0f)]
        public float outputVolume = 1.0f;
    }
}