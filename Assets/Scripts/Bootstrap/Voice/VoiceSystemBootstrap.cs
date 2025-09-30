using UnityEngine;
using ChatSystem.Bootstrap;
using ChatSystem.Services.Communication;
using ChatSystem.Services.Communication.Interfaces;
using ChatSystem.Services.Audio;
using ChatSystem.Services.Audio.Interfaces;
using ChatSystem.Services.Orchestrators;
using ChatSystem.Services.Orchestrators.Interfaces;
using ChatSystem.Controllers.Voice;
using ChatSystem.Controllers.Voice.Interfaces;
using ChatSystem.Views.Voice;
using ChatSystem.Configuration.Voice;
using ChatSystem.Services.Logging;
using ChatSystem.Bootstrap.Voice.Debug;

namespace ChatSystem.Bootstrap.Voice
{
    public class VoiceSystemBootstrap : ChatManager
    {
        [Header("Voice System Configuration")]
        [SerializeField] private VoiceAgentConfig[] voiceAgentConfigs;
        [SerializeField] private VoiceView voiceView;
        [SerializeField] private string defaultConversationId = "voice-conversation";
        [SerializeField] private string defaultVoiceAgentId = "voice-agent-default";
        
        [Header("Voice System Settings")]
        [SerializeField] private bool createVoiceDebugObjects = true;
        [SerializeField] private bool enableVoiceLogging = true;
        
        private IWebSocketService webSocketService;
        private IAudioService audioService;
        private IRealtimeOrchestrator realtimeOrchestrator;
        private IVoiceController voiceController;

        protected override void CreateServices()
        {
            base.CreateServices();
            
            if (enableVoiceLogging)
            {
                LoggingService.LogInfo("Creating Voice System services");
            }
            
            CreateVoiceServices();
        }

        protected override void RegisterAgentConfigurations()
        {
            base.RegisterAgentConfigurations();
            RegisterVoiceAgents();
        }

        protected override void CreateControllers()
        {
            base.CreateControllers();
            CreateVoiceControllers();
        }

        protected override void ConfigureServices()
        {
            base.ConfigureServices();
            ConfigureVoiceServices();
        }

        protected override void ConnectComponents()
        {
            base.ConnectComponents();
            ConnectVoiceComponents();
        }

        protected override void CreateDebugObjectsIfEnabled()
        {
            base.CreateDebugObjectsIfEnabled();
            
            if (createVoiceDebugObjects)
            {
                CreateVoiceDebugObjects();
            }
        }

        private void CreateVoiceServices()
        {
            audioService = FindObjectOfType<AudioService>();
            if (audioService == null)
            {
                GameObject audioServiceGO = new GameObject("[VOICE] AudioService");
                audioServiceGO.transform.SetParent(transform);
                audioService = audioServiceGO.AddComponent<AudioService>();
            }

            webSocketService = new WebSocketService();
            
            realtimeOrchestrator = new RealtimeOrchestrator(
                webSocketService,
                audioService,
                agentExecutor,
                contextManager,
                voiceAgentConfigs[0]
            );

            LoggingService.LogInfo("Voice services created successfully");
        }

        private void RegisterVoiceAgents()
        {
            if (voiceAgentConfigs == null || voiceAgentConfigs.Length == 0)
            {
                LoggingService.LogWarning("No VoiceAgentConfigs configured");
                return;
            }

            foreach (VoiceAgentConfig voiceAgent in voiceAgentConfigs)
            {
                if (voiceAgent != null)
                {
                    llmOrchestrator.RegisterAgentConfig(voiceAgent);
                    LoggingService.LogInfo($"Registered voice agent: {voiceAgent.agentName}");
                }
            }

            LoggingService.LogInfo($"Registered {voiceAgentConfigs.Length} voice agents");
        }

        private void CreateVoiceControllers()
        {
            voiceController = new VoiceController();
            LoggingService.LogInfo("VoiceController created");
        }

        private void ConfigureVoiceServices()
        {
            if (voiceController == null)
            {
                LoggingService.LogError("VoiceController not created");
                return;
            }

            voiceController.SetRealtimeOrchestrator(realtimeOrchestrator);
            voiceController.SetAudioService(audioService);
            voiceController.SetContextManager(contextManager);

            LoggingService.LogInfo("Voice services configured");
        }

        private void ConnectVoiceComponents()
        {
            if (voiceView != null && voiceController != null)
            {
                voiceView.SetController(voiceController);
                voiceController.SetVoiceView(voiceView);
                
                SetupVoiceAgentsInUI();
                LoggingService.LogInfo("Voice components connected");
            }
            else
            {
                LoggingService.LogWarning("VoiceView or VoiceController not available for connection");
            }
        }

        private void SetupVoiceAgentsInUI()
        {
            if (voiceView == null || voiceAgentConfigs == null || voiceAgentConfigs.Length == 0)
            {
                return;
            }

            string[] agentIds = new string[voiceAgentConfigs.Length];
            string[] agentNames = new string[voiceAgentConfigs.Length];

            for (int i = 0; i < voiceAgentConfigs.Length; i++)
            {
                agentIds[i] = voiceAgentConfigs[i].agentId.ToString();
                agentNames[i] = voiceAgentConfigs[i].agentName;
            }

            voiceView.SetAvailableAgents(agentIds, agentNames);
        }

        private void CreateVoiceDebugObjects()
        {
            GameObject debugParent = new GameObject("[DEBUG] Voice Components");
            debugParent.transform.SetParent(transform);

            CreateWebSocketDebugObject(debugParent.transform);
            CreateAudioDebugObject(debugParent.transform);
            CreateRealtimeOrchestratorDebugObject(debugParent.transform);
            CreateVoiceControllerDebugObject(debugParent.transform);

            LoggingService.LogInfo("Voice debug objects created");
        }

        private void CreateWebSocketDebugObject(Transform parent)
        {
            GameObject debugGO = new GameObject("[DEBUG] WebSocketService");
            debugGO.transform.SetParent(parent);
            
            WebSocketDebugComponent debugComponent = debugGO.AddComponent<WebSocketDebugComponent>();
            debugComponent.webSocketService = webSocketService;
        }

        private void CreateAudioDebugObject(Transform parent)
        {
            GameObject debugGO = new GameObject("[DEBUG] AudioService");
            debugGO.transform.SetParent(parent);
            
            AudioDebugComponent debugComponent = debugGO.AddComponent<AudioDebugComponent>();
            debugComponent.audioService = audioService;
        }

        private void CreateRealtimeOrchestratorDebugObject(Transform parent)
        {
            GameObject debugGO = new GameObject("[DEBUG] RealtimeOrchestrator");
            debugGO.transform.SetParent(parent);
            
            RealtimeOrchestratorDebugComponent debugComponent = debugGO.AddComponent<RealtimeOrchestratorDebugComponent>();
            debugComponent.realtimeOrchestrator = realtimeOrchestrator;
        }

        private void CreateVoiceControllerDebugObject(Transform parent)
        {
            GameObject debugGO = new GameObject("[DEBUG] VoiceController");
            debugGO.transform.SetParent(parent);
            
            VoiceControllerDebugComponent debugComponent = debugGO.AddComponent<VoiceControllerDebugComponent>();
            debugComponent.voiceController = voiceController;
        }
    }
}

namespace ChatSystem.Bootstrap.Voice.Debug
{
    public class WebSocketDebugComponent : MonoBehaviour
    {
        public IWebSocketService webSocketService;

        [ContextMenu("Test Connection")]
        private void TestConnection()
        {
            LoggingService.LogInfo($"WebSocket Status: {webSocketService?.ConnectionStatus ?? "Not Available"}");
        }

        [ContextMenu("Show Connection Info")]
        private void ShowConnectionInfo()
        {
            if (webSocketService != null)
            {
                LoggingService.LogInfo($"Is Connected: {webSocketService.IsConnected}");
                LoggingService.LogInfo($"Status: {webSocketService.ConnectionStatus}");
            }
        }
    }

    public class AudioDebugComponent : MonoBehaviour
    {
        public IAudioService audioService;

        [ContextMenu("Test Audio Info")]
        private void TestAudioInfo()
        {
            if (audioService != null)
            {
                LoggingService.LogInfo($"Is Recording: {audioService.IsRecording}");
                LoggingService.LogInfo($"Is Playing: {audioService.IsPlaying}");
                LoggingService.LogInfo($"Available Mics: {string.Join(", ", audioService.AvailableMicrophones)}");
            }
        }

        [ContextMenu("Start Test Recording")]
        private async void StartTestRecording()
        {
            if (audioService != null)
            {
                bool started = await audioService.StartRecordingAsync();
                LoggingService.LogInfo($"Recording started: {started}");
            }
        }

        [ContextMenu("Stop Test Recording")]
        private async void StopTestRecording()
        {
            if (audioService != null)
            {
                await audioService.StopRecordingAsync();
                LoggingService.LogInfo("Recording stopped");
            }
        }
    }

    public class RealtimeOrchestratorDebugComponent : MonoBehaviour
    {
        public IRealtimeOrchestrator realtimeOrchestrator;

        [ContextMenu("Test Session Status")]
        private void TestSessionStatus()
        {
            if (realtimeOrchestrator != null)
            {
                LoggingService.LogInfo($"Session Active: {realtimeOrchestrator.IsSessionActive}");
                LoggingService.LogInfo($"Session ID: {realtimeOrchestrator.CurrentSessionId}");
                LoggingService.LogInfo($"Agent ID: {realtimeOrchestrator.CurrentAgentId}");
            }
        }

        [ContextMenu("Start Test Session")]
        private async void StartTestSession()
        {
            if (realtimeOrchestrator != null)
            {
                await realtimeOrchestrator.StartSessionAsync("debug-session", "debug-agent");
                LoggingService.LogInfo("Debug session started");
            }
        }

        [ContextMenu("End Test Session")]
        private async void EndTestSession()
        {
            if (realtimeOrchestrator != null)
            {
                await realtimeOrchestrator.EndSessionAsync();
                LoggingService.LogInfo("Debug session ended");
            }
        }
    }

    public class VoiceControllerDebugComponent : MonoBehaviour
    {
        public IVoiceController voiceController;

        [ContextMenu("Test Controller Status")]
        private void TestControllerStatus()
        {
            if (voiceController != null)
            {
                LoggingService.LogInfo($"Session Active: {voiceController.IsSessionActive}");
                LoggingService.LogInfo($"Conversation ID: {voiceController.CurrentConversationId}");
                LoggingService.LogInfo($"Agent ID: {voiceController.CurrentAgentId}");
            }
        }

        [ContextMenu("Start Test Voice Session")]
        private async void StartTestVoiceSession()
        {
            if (voiceController != null)
            {
                await voiceController.StartVoiceSessionAsync("debug-conversation", "debug-voice-agent");
                LoggingService.LogInfo("Debug voice session started");
            }
        }

        [ContextMenu("Send Test Text Message")]
        private async void SendTestTextMessage()
        {
            if (voiceController != null)
            {
                await voiceController.SendTextMessageAsync("This is a test message from debug");
                LoggingService.LogInfo("Debug text message sent");
            }
        }
    }
}