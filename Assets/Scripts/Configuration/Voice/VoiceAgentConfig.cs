using UnityEngine;
using ChatSystem.Configuration.ScriptableObjects;
using ChatSystem.Models.Audio;

namespace ChatSystem.Configuration.Voice
{
    [CreateAssetMenu(fileName = "VoiceAgentConfig", menuName = "LLM/Voice Agent Configuration")]
    public class VoiceAgentConfig : AgentConfig
    {
        [Header("Voice-Specific Settings")]
        [SerializeField] private VoiceSettings voiceSettings = new VoiceSettings();
        [SerializeField] private bool enableStreamingAudio = true;
        [SerializeField] private bool enableVoiceActivityDetection = true;
        [SerializeField] private float connectionTimeoutSeconds = 10f;
        [SerializeField] private int maxReconnectAttempts = 3;
        
        [Header("WebSocket Configuration")]
        [SerializeField] private string realtimeEndpoint = "wss://api.openai.com/v1/realtime";
        [SerializeField] private string model = "gpt-4o-realtime-preview-2024-10-01";
        [SerializeField] private string voice = "alloy";
        [SerializeField] private bool enableTurnDetection = true;
        
        public VoiceSettings VoiceSettings => voiceSettings;
        public bool EnableStreamingAudio => enableStreamingAudio;
        public bool EnableVoiceActivityDetection => enableVoiceActivityDetection;
        public float ConnectionTimeoutSeconds => connectionTimeoutSeconds;
        public int MaxReconnectAttempts => maxReconnectAttempts;
        public string RealtimeEndpoint => realtimeEndpoint;
        public string Model => model;
        public string Voice => voice;
        public bool EnableTurnDetection => enableTurnDetection;
    }
}