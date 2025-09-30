using System;
using System.Threading.Tasks;
using ChatSystem.Controllers.Voice.Interfaces;
using ChatSystem.Services.Orchestrators.Interfaces;
using ChatSystem.Services.Audio.Interfaces;
using ChatSystem.Services.Context.Interfaces;
using ChatSystem.Services.Logging;
using ChatSystem.Models.Audio;
using ChatSystem.Views.Voice.Interfaces;

namespace ChatSystem.Controllers.Voice
{
    public class VoiceController : IVoiceController
    {
        private IRealtimeOrchestrator realtimeOrchestrator;
        private IAudioService audioService;
        private IContextManager contextManager;
        private IVoiceView voiceView;
        
        private string currentConversationId;
        private string currentAgentId;
        private bool isSessionActive;

        public event Action<string> OnTranscriptionReceived;
        public event Action<string> OnResponseReceived;
        public event Action<string> OnErrorOccurred;

        public bool IsSessionActive => isSessionActive;
        public string CurrentConversationId => currentConversationId;
        public string CurrentAgentId => currentAgentId;

        public VoiceController()
        {
            LoggingService.LogInfo("VoiceController initialized");
        }

        public void SetRealtimeOrchestrator(IRealtimeOrchestrator orchestrator)
        {
            realtimeOrchestrator = orchestrator;
            SetupOrchestratorEvents();
            LoggingService.LogInfo("RealtimeOrchestrator assigned to VoiceController");
        }

        public void SetAudioService(IAudioService audioService)
        {
            this.audioService = audioService;
            SetupAudioServiceEvents();
            LoggingService.LogInfo("AudioService assigned to VoiceController");
        }

        public void SetContextManager(IContextManager contextManager)
        {
            this.contextManager = contextManager;
            LoggingService.LogInfo("ContextManager assigned to VoiceController");
        }

        public void SetVoiceView(IVoiceView voiceView)
        {
            this.voiceView = voiceView;
            LoggingService.LogInfo("VoiceView assigned to VoiceController");
        }

        public async Task StartVoiceSessionAsync(string conversationId, string agentId)
        {
            try
            {
                if (isSessionActive)
                {
                    LoggingService.LogWarning("Voice session already active");
                    return;
                }

                if (realtimeOrchestrator == null)
                {
                    string errorMsg = "RealtimeOrchestrator not configured";
                    LoggingService.LogError(errorMsg);
                    OnErrorOccurred?.Invoke(errorMsg);
                    return;
                }

                currentConversationId = conversationId;
                currentAgentId = agentId;

                await realtimeOrchestrator.StartSessionAsync(conversationId, agentId);
                isSessionActive = true;

                voiceView?.OnSessionStarted(conversationId, agentId);
                LoggingService.LogInfo($"Voice session started: {conversationId} with agent: {agentId}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to start voice session: {ex.Message}");
                OnErrorOccurred?.Invoke(ex.Message);
                voiceView?.ShowError($"Failed to start session: {ex.Message}");
            }
        }

        public async Task ProcessVoiceInputAsync(AudioData audioData)
        {
            if (!isSessionActive)
            {
                LoggingService.LogWarning("No active voice session");
                return;
            }

            try
            {
                await realtimeOrchestrator.ProcessVoiceInputAsync(audioData);
                LoggingService.LogDebug("Voice input processed");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to process voice input: {ex.Message}");
                OnErrorOccurred?.Invoke(ex.Message);
                voiceView?.ShowError($"Voice processing error: {ex.Message}");
            }
        }

        public async Task StopVoiceSessionAsync()
        {
            try
            {
                if (!isSessionActive)
                {
                    LoggingService.LogInfo("No active voice session to stop");
                    return;
                }

                if (audioService != null && audioService.IsRecording)
                {
                    await audioService.StopRecordingAsync();
                }

                if (realtimeOrchestrator != null)
                {
                    await realtimeOrchestrator.EndSessionAsync();
                }

                isSessionActive = false;
                currentConversationId = null;
                currentAgentId = null;

                voiceView?.OnSessionEnded();
                LoggingService.LogInfo("Voice session stopped");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error stopping voice session: {ex.Message}");
                OnErrorOccurred?.Invoke(ex.Message);
            }
        }

        public async Task SetActiveAgentAsync(string agentId)
        {
            try
            {
                if (agentId == currentAgentId)
                {
                    LoggingService.LogInfo("Same agent already active");
                    return;
                }

                if (!isSessionActive || realtimeOrchestrator == null)
                {
                    LoggingService.LogWarning("No active session to switch agent");
                    currentAgentId = agentId;
                    return;
                }

                await realtimeOrchestrator.SwitchAgentAsync(agentId);
                currentAgentId = agentId;

                voiceView?.OnAgentChanged(agentId);
                LoggingService.LogInfo($"Switched to agent: {agentId}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to switch agent: {ex.Message}");
                OnErrorOccurred?.Invoke(ex.Message);
                voiceView?.ShowError($"Agent switch error: {ex.Message}");
            }
        }

        public async Task StartRecordingAsync()
        {
            if (audioService == null)
            {
                string errorMsg = "AudioService not configured";
                LoggingService.LogError(errorMsg);
                OnErrorOccurred?.Invoke(errorMsg);
                return;
            }

            try
            {
                bool started = await audioService.StartRecordingAsync();
                if (started)
                {
                    voiceView?.OnRecordingStarted();
                    LoggingService.LogInfo("Recording started");
                }
                else
                {
                    string errorMsg = "Failed to start recording";
                    OnErrorOccurred?.Invoke(errorMsg);
                    voiceView?.ShowError(errorMsg);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to start recording: {ex.Message}");
                OnErrorOccurred?.Invoke(ex.Message);
                voiceView?.ShowError($"Recording error: {ex.Message}");
            }
        }

        public async Task StopRecordingAsync()
        {
            if (audioService == null || !audioService.IsRecording)
            {
                return;
            }

            try
            {
                await audioService.StopRecordingAsync();
                voiceView?.OnRecordingStopped();
                LoggingService.LogInfo("Recording stopped");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to stop recording: {ex.Message}");
                OnErrorOccurred?.Invoke(ex.Message);
            }
        }

        public async Task SendTextMessageAsync(string message)
        {
            if (!isSessionActive || realtimeOrchestrator == null)
            {
                LoggingService.LogWarning("No active session for text message");
                return;
            }

            try
            {
                await realtimeOrchestrator.SendTextMessageAsync(message);
                voiceView?.ShowMessage($"You: {message}");
                LoggingService.LogInfo($"Text message sent: {message}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to send text message: {ex.Message}");
                OnErrorOccurred?.Invoke(ex.Message);
                voiceView?.ShowError($"Text message error: {ex.Message}");
            }
        }

        private void SetupOrchestratorEvents()
        {
            if (realtimeOrchestrator == null) return;

            realtimeOrchestrator.OnTranscriptionReceived += HandleTranscriptionReceived;
            realtimeOrchestrator.OnResponseGenerated += HandleResponseGenerated;
            realtimeOrchestrator.OnToolExecuted += HandleToolExecuted;
            realtimeOrchestrator.OnAudioReceived += HandleAudioReceived;
            realtimeOrchestrator.OnErrorOccurred += HandleOrchestratorError;
        }

        private void SetupAudioServiceEvents()
        {
            if (audioService == null) return;

            audioService.OnAudioCaptured += HandleAudioCaptured;
            audioService.OnRecordingStarted += () => voiceView?.OnRecordingStarted();
            audioService.OnRecordingStopped += () => voiceView?.OnRecordingStopped();
            audioService.OnAudioError += HandleAudioError;
        }

        private void HandleTranscriptionReceived(string transcription)
        {
            OnTranscriptionReceived?.Invoke(transcription);
            voiceView?.ShowTranscription(transcription);
            LoggingService.LogInfo($"Transcription: {transcription}");
        }

        private void HandleResponseGenerated(string response)
        {
            OnResponseReceived?.Invoke(response);
            voiceView?.ShowMessage($"Assistant: {response}");
            LoggingService.LogInfo($"Response: {response}");
        }

        private void HandleToolExecuted(string toolInfo)
        {
            voiceView?.ShowToolExecution(toolInfo);
            LoggingService.LogInfo($"Tool executed: {toolInfo}");
        }

        private void HandleAudioReceived(string audioInfo)
        {
            voiceView?.ShowAudioStatus(audioInfo);
            LoggingService.LogDebug($"Audio received: {audioInfo}");
        }

        private void HandleOrchestratorError(string error)
        {
            OnErrorOccurred?.Invoke(error);
            voiceView?.ShowError(error);
            LoggingService.LogError($"Orchestrator error: {error}");
        }

        private void HandleAudioError(string error)
        {
            OnErrorOccurred?.Invoke(error);
            voiceView?.ShowError($"Audio error: {error}");
            LoggingService.LogError($"Audio error: {error}");
        }

        private async void HandleAudioCaptured(AudioData audioData)
        {
            if (isSessionActive && audioData.samples != null && audioData.samples.Length > 0)
            {
                await ProcessVoiceInputAsync(audioData);
            }
        }
    }
}