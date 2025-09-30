using System;
using System.Threading.Tasks;
using ChatSystem.Models.Audio;
using ChatSystem.Services.Audio.Interfaces;
using ChatSystem.Services.Context.Interfaces;
using ChatSystem.Services.Orchestrators.Interfaces;
using ChatSystem.Views.Voice.Interfaces;

namespace ChatSystem.Controllers.Voice.Interfaces
{
    public interface IVoiceController
    {
        event Action<string> OnTranscriptionReceived;
        event Action<string> OnResponseReceived;
        event Action<string> OnErrorOccurred;
        
        Task StartVoiceSessionAsync(string conversationId, string agentId);
        Task ProcessVoiceInputAsync(AudioData audioData);
        Task StopVoiceSessionAsync();
        Task SetActiveAgentAsync(string agentId);
        Task StartRecordingAsync();
        Task StopRecordingAsync();
        Task SendTextMessageAsync(string message);

        //TODO: Separar en un interface diferente
        public void SetRealtimeOrchestrator(IRealtimeOrchestrator orchestrator);
        public void SetAudioService(IAudioService audioService);
        public void SetContextManager(IContextManager contextManager);

        public void SetVoiceView(IVoiceView voiceView);

        bool IsSessionActive { get; }
        string CurrentConversationId { get; }
        string CurrentAgentId { get; }
    }
}