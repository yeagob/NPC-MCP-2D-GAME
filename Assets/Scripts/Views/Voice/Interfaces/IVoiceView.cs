namespace ChatSystem.Views.Voice.Interfaces
{
    public interface IVoiceView
    {
        void OnSessionStarted(string conversationId, string agentId);
        void OnSessionEnded();
        void OnAgentChanged(string agentId);
        void OnRecordingStarted();
        void OnRecordingStopped();
        
        void ShowTranscription(string transcription);
        void ShowMessage(string message);
        void ShowToolExecution(string toolInfo);
        void ShowAudioStatus(string status);
        void ShowError(string error);
        
        void SetAvailableAgents(string[] agentIds, string[] agentNames);
        void UpdateConnectionStatus(string status);
        void UpdateAudioLevel(float level);
    }
}