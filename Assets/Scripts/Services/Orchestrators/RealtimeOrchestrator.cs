using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSystem.Services.Orchestrators.Interfaces;
using ChatSystem.Services.Communication.Interfaces;
using ChatSystem.Services.Audio.Interfaces;
using ChatSystem.Services.Agents.Interfaces;
using ChatSystem.Services.Context.Interfaces;
using ChatSystem.Services.Logging;
using ChatSystem.Services.LLM;
using ChatSystem.Services.Tools.Interfaces;
using ChatSystem.Models.Audio;
using ChatSystem.Models.Communication;
using ChatSystem.Models.Context;
using ChatSystem.Models.Tools;
using ChatSystem.Configuration.Voice;
using ChatSystem.Enums;
using ChatSystem.Services.Tools;

namespace ChatSystem.Services.Orchestrators
{
    public class RealtimeOrchestrator : IRealtimeOrchestrator
    {
        private readonly IWebSocketService webSocketService;
        private readonly IAudioService audioService;
        private readonly IAgentExecutor agentExecutor;
        private readonly IContextManager contextManager;
        private readonly Dictionary<string, IToolSet> registeredToolSets;
        
        private string currentSessionId;
        private string currentAgentId;
        private VoiceAgentConfig currentAgentConfig;
        private bool isSessionActive;

        public event Action<string> OnTranscriptionReceived;
        public event Action<string> OnResponseGenerated;
        public event Action<string> OnToolExecuted;
        public event Action<string> OnAudioReceived;
        public event Action<string> OnErrorOccurred;

        public bool IsSessionActive => isSessionActive;
        public string CurrentSessionId => currentSessionId;
        public string CurrentAgentId => currentAgentId;

        public RealtimeOrchestrator(
            IWebSocketService webSocketService,
            IAudioService audioService,
            IAgentExecutor agentExecutor,
            IContextManager contextManager,
            VoiceAgentConfig currentAgentConfig)
        {
            this.webSocketService = webSocketService;
            this.audioService = audioService;
            this.agentExecutor = agentExecutor;
            this.contextManager = contextManager;
            this.registeredToolSets = new Dictionary<string, IToolSet>();
            this.currentAgentConfig = currentAgentConfig;
            
            SetupWebSocketEvents();
            LoggingService.LogInfo("RealtimeOrchestrator initialized");
        }

        public void RegisterToolSet(IToolSet toolSet)
        {
            if (toolSet == null)
            {
                LoggingService.LogError("Cannot register null ToolSet in RealtimeOrchestrator");
                return;
            }
            
            string toolSetName = toolSet.GetType().Name;
            registeredToolSets[toolSetName] = toolSet;
            LoggingService.LogInfo($"ToolSet {toolSetName} registered in RealtimeOrchestrator");
        }

        public async Task StartSessionAsync(string conversationId, string agentId)
        {
            try
            {
                if (isSessionActive)
                {
                    LoggingService.LogWarning("Session already active, ending current session");
                    await EndSessionAsync();
                }

                currentSessionId = conversationId;
                currentAgentId = agentId;
                
                await ConnectWebSocket();
                await InitializeSessionWithOpenAI();
                
                audioService.SetVoiceSettings(currentAgentConfig.VoiceSettings);
                
                isSessionActive = true;
                LoggingService.LogInfo($"Realtime session started: {conversationId} with agent: {agentId}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to start session: {ex.Message}");
                OnErrorOccurred?.Invoke(ex.Message);
            }
        }

        public async Task ProcessVoiceInputAsync(AudioData audioData)
        {
            if (!isSessionActive)
            {
                LoggingService.LogWarning("No active session for voice input");
                return;
            }

            try
            {
                WebSocketEvent audioEvent = OpenAIService.CreateRealtimeAudioEvent(audioData.rawData);
                await webSocketService.SendEventAsync(audioEvent);
                LoggingService.LogDebug($"Sent audio data: {audioData.rawData.Length} bytes");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to process voice input: {ex.Message}");
                OnErrorOccurred?.Invoke(ex.Message);
            }
        }

        public async Task SendTextMessageAsync(string message)
        {
            if (!isSessionActive)
            {
                LoggingService.LogWarning("No active session for text message");
                return;
            }

            try
            {
                WebSocketEvent textEvent = OpenAIService.CreateRealtimeTextMessage(message);
                await webSocketService.SendEventAsync(textEvent);
                LoggingService.LogInfo($"Sent text message: {message}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to send text message: {ex.Message}");
                OnErrorOccurred?.Invoke(ex.Message);
            }
        }

        public async Task EndSessionAsync()
        {
            try
            {
                isSessionActive = false;
                
                if (webSocketService != null && webSocketService.IsConnected)
                {
                    await webSocketService.DisconnectAsync();
                }
                
                if (audioService != null)
                {
                    if (audioService.IsRecording)
                    {
                        await audioService.StopRecordingAsync();
                    }
                    audioService.StopPlayback();
                }
                
                currentSessionId = null;
                currentAgentId = null;
                currentAgentConfig = null;
                
                LoggingService.LogInfo("Realtime session ended");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error ending session: {ex.Message}");
                OnErrorOccurred?.Invoke(ex.Message);
            }
        }

        public async Task SwitchAgentAsync(string newAgentId)
        {
            try
            {
                if (newAgentId == currentAgentId)
                {
                    LoggingService.LogInfo("Same agent already active");
                    return;
                }

                currentAgentId = newAgentId;
                audioService.SetVoiceSettings(currentAgentConfig.VoiceSettings);
                
                await UpdateSessionWithOpenAI();
                
                LoggingService.LogInfo($"Switched to agent: {newAgentId}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to switch agent: {ex.Message}");
                OnErrorOccurred?.Invoke(ex.Message);
            }
        }

        private async Task ConnectWebSocket()
        {
            string apiKey = GetApiKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("OpenAI API key not configured");
            }

            string realtimeUrl = BuildRealtimeUrl();
            LoggingService.LogInfo($"Connecting to OpenAI Realtime API: {realtimeUrl}");
            
            await webSocketService.ConnectAsync(realtimeUrl, apiKey);
        }

        private string BuildRealtimeUrl()
        {
            string baseUrl = currentAgentConfig.RealtimeEndpoint;
            string model = currentAgentConfig.Model;
            
            if (baseUrl.Contains("?"))
            {
                return $"{baseUrl}&model={model}";
            }
            else
            {
                return $"{baseUrl}?model={model}";
            }
        }

        private async Task InitializeSessionWithOpenAI()
        {
            List<ToolConfiguration> toolConfigurations = GetCurrentAgentToolConfigurations();
            WebSocketEvent sessionUpdate = OpenAIService.CreateRealtimeSessionUpdate(currentAgentConfig, toolConfigurations);
            await webSocketService.SendEventAsync(sessionUpdate);
            LoggingService.LogInfo("OpenAI Realtime session initialized with tools and configuration");
        }

        private async Task UpdateSessionWithOpenAI()
        {
            List<ToolConfiguration> toolConfigurations = GetCurrentAgentToolConfigurations();
            WebSocketEvent sessionUpdate = OpenAIService.CreateRealtimeSessionUpdate(currentAgentConfig, toolConfigurations);
            await webSocketService.SendEventAsync(sessionUpdate);
            LoggingService.LogInfo("OpenAI Realtime session updated with new agent configuration");
        }

        private List<ToolConfiguration> GetCurrentAgentToolConfigurations()
        {
            List<ToolConfiguration> toolConfigurations = new List<ToolConfiguration>();
            
            if (currentAgentConfig == null || currentAgentConfig.availableTools == null)
            {
                LoggingService.LogWarning("No agent config or tools available");
                return toolConfigurations;
            }

            foreach (var toolConfig in currentAgentConfig.availableTools)
            {
                if (toolConfig != null && toolConfig.enabled)
                {
                    ToolConfiguration toolConfiguration = new ToolConfiguration(toolConfig);
                    toolConfigurations.Add(toolConfiguration);
                }
            }
            
            LoggingService.LogInfo($"Generated {toolConfigurations.Count} tool configurations from agent");
            return toolConfigurations;
        }

        private void SetupWebSocketEvents()
        {
            webSocketService.OnEventReceived += HandleWebSocketEvent;
            webSocketService.OnError += (error) => OnErrorOccurred?.Invoke(error);
            webSocketService.OnDisconnected += () => isSessionActive = false;
        }

        private async void HandleWebSocketEvent(WebSocketEvent wsEvent)
        {
            try
            {
                switch (wsEvent.type)
                {
                    case "conversation.item.input_audio_transcription.completed":
                        await HandleTranscription(wsEvent);
                        break;
                    case "response.audio.delta":
                        await HandleAudioDelta(wsEvent);
                        break;
                    case "response.function_call_arguments.done":
                        await HandleToolCall(wsEvent);
                        break;
                    case "response.text.done":
                        await HandleTextResponse(wsEvent);
                        break;
                    case "session.created":
                        LoggingService.LogInfo("OpenAI Realtime session created successfully");
                        break;
                    case "session.updated":
                        LoggingService.LogInfo("OpenAI Realtime session updated successfully");
                        break;
                    case "error":
                        HandleError(wsEvent);
                        break;
                    default:
                        LoggingService.LogDebug($"Unhandled WebSocket event: {wsEvent.type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error handling WebSocket event: {ex.Message}");
                OnErrorOccurred?.Invoke(ex.Message);
            }
        }

        private async Task HandleTranscription(WebSocketEvent wsEvent)
        {
            string transcription = ExtractTranscriptionText(wsEvent.data.ToString());
            if (!string.IsNullOrEmpty(transcription))
            {
                OnTranscriptionReceived?.Invoke(transcription);
                
                await contextManager.AddUserMessageAsync(currentSessionId, transcription);
                LoggingService.LogInfo($"Transcription received: {transcription}");
            }
        }

        private async Task HandleAudioDelta(WebSocketEvent wsEvent)
        {
            byte[] audioChunk = ExtractAudioData(wsEvent.data.ToString());
            if (audioChunk != null && audioChunk.Length > 0)
            {
                await audioService.PlayAudioChunkAsync(audioChunk);
                OnAudioReceived?.Invoke($"Audio chunk: {audioChunk.Length} bytes");
            }
        }

        private async Task HandleToolCall(WebSocketEvent wsEvent)
        {
            string toolName = ExtractToolName(wsEvent.data.ToString());
            string toolArguments = ExtractToolArguments(wsEvent.data.ToString());
            string toolCallId = ExtractToolCallId(wsEvent.data.ToString());
            
            if (!string.IsNullOrEmpty(toolName))
            {
                LoggingService.LogInfo($"Tool call received: {toolName}");
                OnToolExecuted?.Invoke($"Executing: {toolName}");
                
                try
                {
                    ToolResponse toolResult = await ExecuteToolDirectly(toolName, toolArguments, toolCallId);
                    
                    WebSocketEvent toolResponse = OpenAIService.CreateRealtimeToolResponse(toolCallId, toolResult.content);
                    await webSocketService.SendEventAsync(toolResponse);
                    
                    await contextManager.AddToolMessageAsync(currentSessionId, toolResult.content, toolCallId);
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"Tool execution failed: {ex.Message}");
                    WebSocketEvent errorResponse = OpenAIService.CreateRealtimeToolResponse(toolCallId, $"Error: {ex.Message}");
                    await webSocketService.SendEventAsync(errorResponse);
                }
            }
        }

        private async Task<ToolResponse> ExecuteToolDirectly(string toolName, string toolArgumentsJson, string toolCallId)
        {
            Dictionary<string, object> arguments = ParseToolArguments(toolArgumentsJson);
            ToolCall toolCall = new ToolCall(toolName, arguments) { id = toolCallId };
            
            foreach (IToolSet toolSet in registeredToolSets.Values)
            {
                if (toolSet.IsToolSupported(toolName))
                {
                    ToolDebugContext debugContext = CreateDebugContext();
                    return await toolSet.ExecuteToolAsync(toolCall, debugContext);
                }
            }
            
            throw new InvalidOperationException($"Tool {toolName} not found in any registered ToolSet");
        }

        private Dictionary<string, object> ParseToolArguments(string argumentsJson)
        {
            if (string.IsNullOrEmpty(argumentsJson) || argumentsJson == "{}")
            {
                return new Dictionary<string, object>();
            }
            
            try
            {
                return SimpleJsonParser.ParseArguments(argumentsJson);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to parse tool arguments: {ex.Message}");
                return new Dictionary<string, object>();
            }
        }

        private ToolDebugContext CreateDebugContext()
        {
            if (currentAgentConfig == null || !currentAgentConfig.debugTools)
                return ToolDebugContext.Disabled;
                
            ConversationContext context = contextManager.GetContextAsync(currentSessionId).Result;
            ConversationToolDebugHandler debugHandler = new ConversationToolDebugHandler(context);
            return new ToolDebugContext(true, debugHandler);
        }

        private async Task HandleTextResponse(WebSocketEvent wsEvent)
        {
            string responseText = ExtractResponseText(wsEvent.data.ToString());
            if (!string.IsNullOrEmpty(responseText))
            {
                OnResponseGenerated?.Invoke(responseText);
                
                await contextManager.AddAssistantMessageAsync(currentSessionId, responseText);
                LoggingService.LogInfo($"Text response: {responseText}");
            }
        }

        private void HandleError(WebSocketEvent wsEvent)
        {
            string errorMessage = ExtractErrorMessage(wsEvent.data.ToString());
            LoggingService.LogError($"WebSocket error: {errorMessage}");
            OnErrorOccurred?.Invoke(errorMessage);
        }

        private string GetApiKey()
        {
            return Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? 
                   UnityEngine.PlayerPrefs.GetString("OPENAI_API_KEY", "");
        }

        private string ExtractTranscriptionText(string data)
        {
            if (string.IsNullOrEmpty(data))
                return string.Empty;
                
            int transcriptIndex = data.IndexOf("\"transcript\":");
            if (transcriptIndex == -1)
                return string.Empty;
                
            int startQuote = data.IndexOf('"', transcriptIndex + 13);
            if (startQuote == -1)
                return string.Empty;
                
            int endQuote = data.IndexOf('"', startQuote + 1);
            if (endQuote == -1)
                return string.Empty;
                
            return data.Substring(startQuote + 1, endQuote - startQuote - 1);
        }

        private byte[] ExtractAudioData(string data)
        {
            if (string.IsNullOrEmpty(data))
                return new byte[0];
                
            int audioIndex = data.IndexOf("\"audio\":");
            if (audioIndex == -1)
                return new byte[0];
                
            int startQuote = data.IndexOf('"', audioIndex + 8);
            if (startQuote == -1)
                return new byte[0];
                
            int endQuote = data.IndexOf('"', startQuote + 1);
            if (endQuote == -1)
                return new byte[0];
                
            string base64Audio = data.Substring(startQuote + 1, endQuote - startQuote - 1);
            
            try
            {
                return Convert.FromBase64String(base64Audio);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to decode base64 audio: {ex.Message}");
                return new byte[0];
            }
        }

        private string ExtractToolName(string data)
        {
            if (string.IsNullOrEmpty(data))
                return string.Empty;
                
            int nameIndex = data.IndexOf("\"name\":");
            if (nameIndex == -1)
                return string.Empty;
                
            int startQuote = data.IndexOf('"', nameIndex + 7);
            if (startQuote == -1)
                return string.Empty;
                
            int endQuote = data.IndexOf('"', startQuote + 1);
            if (endQuote == -1)
                return string.Empty;
                
            return data.Substring(startQuote + 1, endQuote - startQuote - 1);
        }

        private string ExtractToolArguments(string data)
        {
            if (string.IsNullOrEmpty(data))
                return "{}";
                
            int argsIndex = data.IndexOf("\"arguments\":");
            if (argsIndex == -1)
                return "{}";
                
            int startQuote = data.IndexOf('"', argsIndex + 12);
            if (startQuote == -1)
                return "{}";
                
            int endQuote = data.IndexOf('"', startQuote + 1);
            if (endQuote == -1)
                return "{}";
                
            return data.Substring(startQuote + 1, endQuote - startQuote - 1);
        }

        private string ExtractToolCallId(string data)
        {
            if (string.IsNullOrEmpty(data))
                return string.Empty;
                
            int callIdIndex = data.IndexOf("\"call_id\":");
            if (callIdIndex == -1)
                return string.Empty;
                
            int startQuote = data.IndexOf('"', callIdIndex + 10);
            if (startQuote == -1)
                return string.Empty;
                
            int endQuote = data.IndexOf('"', startQuote + 1);
            if (endQuote == -1)
                return string.Empty;
                
            return data.Substring(startQuote + 1, endQuote - startQuote - 1);
        }

        private string ExtractResponseText(string data)
        {
            if (string.IsNullOrEmpty(data))
                return string.Empty;
                
            int textIndex = data.IndexOf("\"text\":");
            if (textIndex == -1)
                return string.Empty;
                
            int startQuote = data.IndexOf('"', textIndex + 7);
            if (startQuote == -1)
                return string.Empty;
                
            int endQuote = data.IndexOf('"', startQuote + 1);
            if (endQuote == -1)
                return string.Empty;
                
            return data.Substring(startQuote + 1, endQuote - startQuote - 1);
        }

        private string ExtractErrorMessage(string data)
        {
            if (string.IsNullOrEmpty(data))
                return "Unknown error";
                
            int messageIndex = data.IndexOf("\"message\":");
            if (messageIndex == -1)
                return "Unknown error";
                
            int startQuote = data.IndexOf('"', messageIndex + 10);
            if (startQuote == -1)
                return "Unknown error";
                
            int endQuote = data.IndexOf('"', startQuote + 1);
            if (endQuote == -1)
                return "Unknown error";
                
            return data.Substring(startQuote + 1, endQuote - startQuote - 1);
        }
    }
}