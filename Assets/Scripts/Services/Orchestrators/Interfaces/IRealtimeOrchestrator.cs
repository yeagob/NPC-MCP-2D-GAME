using System;
using System.Threading.Tasks;
using ChatSystem.Models.Audio;

namespace ChatSystem.Services.Orchestrators.Interfaces
{
    public interface IRealtimeOrchestrator
    {
        event Action<string> OnTranscriptionReceived;
        event Action<string> OnResponseGenerated;
        event Action<string> OnToolExecuted;
        event Action<string> OnAudioReceived;
        event Action<string> OnErrorOccurred;
        
        Task StartSessionAsync(string conversationId, string agentId);
        Task ProcessVoiceInputAsync(AudioData audioData);
        Task SendTextMessageAsync(string message);
        Task EndSessionAsync();
        
        Task SwitchAgentAsync(string newAgentId);
        
        bool IsSessionActive { get; }
        string CurrentSessionId { get; }
        string CurrentAgentId { get; }
    }
}