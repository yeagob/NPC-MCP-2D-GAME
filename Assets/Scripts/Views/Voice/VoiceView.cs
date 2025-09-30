using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChatSystem.Views.Voice.Interfaces;
using ChatSystem.Controllers.Voice.Interfaces;
using ChatSystem.Services.Logging;

namespace ChatSystem.Views.Voice
{
    public class VoiceView : MonoBehaviour, IVoiceView
    {
        [Header("UI Components")]
        [SerializeField] private Button recordButton;
        [SerializeField] private Button startSessionButton;
        [SerializeField] private TMP_Dropdown agentDropdown;
        [SerializeField] private TMP_InputField textInputField;
        [SerializeField] private Button sendTextButton;
        
        [Header("Display Panels")]
        [SerializeField] private TextMeshProUGUI transcriptionText;
        [SerializeField] private TextMeshProUGUI conversationText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private ScrollRect conversationScrollRect;
        
        [Header("Visual Feedback")]
        [SerializeField] private Image recordingIndicator;
        [SerializeField] private Image connectionIndicator;
        [SerializeField] private Slider audioLevelSlider;
        [SerializeField] private GameObject loadingIndicator;
        
        [Header("Configuration")]
        [SerializeField] private string defaultConversationId = "voice-conversation";
        [SerializeField] private Color recordingColor = Color.red;
        [SerializeField] private Color connectedColor = Color.green;
        [SerializeField] private Color disconnectedColor = Color.red;
        [SerializeField] private Color errorColor = Color.red;

        private IVoiceController voiceController;
        private string currentConversationId;
        private bool isRecording;
        private bool isSessionActive;

        private void Awake()
        {
            SetupUIEvents();
            InitializeUI();
            currentConversationId = defaultConversationId;
        }

		private void SetupUIEvents()
        {
            if (recordButton != null)
            {
                recordButton.onClick.AddListener(ToggleRecording);
            }
            
            if (startSessionButton != null)
            {
                startSessionButton.onClick.AddListener(StartSession);
            }
            
            if (sendTextButton != null)
            {
                sendTextButton.onClick.AddListener(SendTextMessage);
            }
            
            if (textInputField != null)
            {
                textInputField.onEndEdit.AddListener(OnTextInputEndEdit);
            }
            
            if (agentDropdown != null)
            {
                agentDropdown.onValueChanged.AddListener(OnAgentChanged);
            }
        }

        private void InitializeUI()
        {
            if (transcriptionText != null)
                transcriptionText.text = "Transcription will appear here...";
            
            if (conversationText != null)
                conversationText.text = "Voice conversation will appear here...";
                
            if (statusText != null)
                statusText.text = "Ready to start voice session";
                
            if (errorText != null)
                errorText.text = "";
                
            if (recordingIndicator != null)
                recordingIndicator.color = Color.gray;
                
            if (connectionIndicator != null)
                connectionIndicator.color = disconnectedColor;
                
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);
                
            UpdateButtonStates();
        }

        public void SetController(IVoiceController controller)
        {
            voiceController = controller;
            LoggingService.LogInfo("VoiceController assigned to VoiceView");
        }

        public void OnSessionStarted(string conversationId, string agentId)
        {
            isSessionActive = true;
            currentConversationId = conversationId;
            
            if (statusText != null)
                statusText.text = $"Session active with {agentId}";
                
            if (connectionIndicator != null)
                connectionIndicator.color = connectedColor;
                
            UpdateButtonStates();
            ClearError();
            LoggingService.LogInfo($"UI updated for session start: {conversationId}");
        }

        public void OnSessionEnded()
        {
            isSessionActive = false;
            isRecording = false;
            
            if (statusText != null)
                statusText.text = "Session ended";
                
            if (connectionIndicator != null)
                connectionIndicator.color = disconnectedColor;
                
            if (recordingIndicator != null)
                recordingIndicator.color = Color.gray;
                
            UpdateButtonStates();
            LoggingService.LogInfo("UI updated for session end");
        }

        public void OnAgentChanged(string agentId)
        {
            if (statusText != null)
                statusText.text = $"Switched to agent: {agentId}";
                
            LoggingService.LogInfo($"UI updated for agent change: {agentId}");
        }

        public void OnRecordingStarted()
        {
            isRecording = true;
            
            if (recordingIndicator != null)
                recordingIndicator.color = recordingColor;
                
            if (statusText != null)
                statusText.text = "Recording...";
                
            UpdateButtonStates();
            LoggingService.LogInfo("UI updated for recording start");
        }

        public void OnRecordingStopped()
        {
            isRecording = false;
            
            if (recordingIndicator != null)
                recordingIndicator.color = Color.gray;
                
            if (statusText != null && isSessionActive)
                statusText.text = "Processing...";
                
            UpdateButtonStates();
            LoggingService.LogInfo("UI updated for recording stop");
        }

        public void ShowTranscription(string transcription)
        {
            if (transcriptionText != null)
            {
                transcriptionText.text = $"You: {transcription}";
            }
            
            AppendToConversation($"<color=blue>You:</color> {transcription}");
            LoggingService.LogInfo($"Transcription displayed: {transcription}");
        }

        public void ShowMessage(string message)
        {
            AppendToConversation(message);
            LoggingService.LogInfo($"Message displayed: {message}");
        }

        public void ShowToolExecution(string toolInfo)
        {
            AppendToConversation($"<color=orange>🔧 Tool:</color> {toolInfo}");
            LoggingService.LogInfo($"Tool execution displayed: {toolInfo}");
        }

        public void ShowAudioStatus(string status)
        {
            if (statusText != null)
                statusText.text = status;
        }

        public void ShowError(string error)
        {
            if (errorText != null)
            {
                errorText.text = error;
                errorText.color = errorColor;
            }
            
            AppendToConversation($"<color=red>❌ Error:</color> {error}");
            LoggingService.LogError($"Error displayed in UI: {error}");
        }

        public void SetAvailableAgents(string[] agentIds, string[] agentNames)
        {
            if (agentDropdown == null) return;
            
            agentDropdown.ClearOptions();
            
            for (int i = 0; i < agentNames.Length; i++)
            {
                agentDropdown.options.Add(new TMP_Dropdown.OptionData(agentNames[i]));
            }
            
            agentDropdown.RefreshShownValue();
            LoggingService.LogInfo($"Available agents updated: {agentNames.Length} agents");
        }

        public void UpdateConnectionStatus(string status)
        {
            if (statusText != null)
                statusText.text = status;
                
            bool connected = status.Contains("Connected") || status.Contains("Active");
            if (connectionIndicator != null)
                connectionIndicator.color = connected ? connectedColor : disconnectedColor;
        }

        public void UpdateAudioLevel(float level)
        {
            if (audioLevelSlider != null)
            {
                audioLevelSlider.value = Mathf.Clamp01(level);
            }
        }

        private async void ToggleRecording()
        {
            if (voiceController == null)
            {
                ShowError("Voice controller not configured");
                return;
            }

            try
            {
                if (isRecording)
                {
                    await voiceController.StopRecordingAsync();
                }
                else
                {
                    await voiceController.StartRecordingAsync();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Recording toggle error: {ex.Message}");
            }
        }

        private async void StartSession()
        {
            if (voiceController == null)
            {
                ShowError("Voice controller not configured");
                return;
            }

            try
            {
                string selectedAgent = GetSelectedAgent();
                await voiceController.StartVoiceSessionAsync(currentConversationId, selectedAgent);
                Debug.Log("SESION DE VOZ INICIADA");
            }
            catch (Exception ex)
            {
                ShowError($"Failed to start session: {ex.Message}");
            }
        }

        private async void StopSession()
        {
            if (voiceController == null) return;

            try
            {
                await voiceController.StopVoiceSessionAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Failed to stop session: {ex.Message}");
            }
        }

        private async void SendTextMessage()
        {
            if (voiceController == null || textInputField == null) return;
            
            string message = textInputField.text.Trim();
            if (string.IsNullOrEmpty(message)) return;

            try
            {
                await voiceController.SendTextMessageAsync(message);
                textInputField.text = "";
            }
            catch (Exception ex)
            {
                ShowError($"Failed to send text message: {ex.Message}");
            }
        }

        private void OnTextInputEndEdit(string text)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SendTextMessage();
            }
        }

        private async void OnAgentChanged(int index)
        {
            if (voiceController == null || !isSessionActive) return;
            
            try
            {
                string selectedAgent = GetSelectedAgent();
                await voiceController.SetActiveAgentAsync(selectedAgent);
            }
            catch (Exception ex)
            {
                ShowError($"Failed to change agent: {ex.Message}");
            }
        }

        private void AppendToConversation(string message)
        {
            if (conversationText == null) return;
            
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string formattedMessage = $"[{timestamp}] {message}\n";
            
            conversationText.text += formattedMessage;
            
            if (conversationScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                conversationScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void UpdateButtonStates()
        {
            if (startSessionButton != null)
                startSessionButton.interactable = !isSessionActive;
                
            if (recordButton != null)
                recordButton.interactable = isSessionActive;
                
            if (sendTextButton != null)
                sendTextButton.interactable = isSessionActive;
                
            if (textInputField != null)
                textInputField.interactable = isSessionActive;
        }

        private string GetSelectedAgent()
        {
            if (agentDropdown == null || agentDropdown.options.Count == 0)
                return "default-voice-agent";
                
            return agentDropdown.options[agentDropdown.value].text;
        }

        private void ClearError()
        {
            if (errorText != null)
                errorText.text = "";
        }

        private void OnDestroy()
        {
            if (recordButton != null)
                recordButton.onClick.RemoveAllListeners();
            if (startSessionButton != null)
                startSessionButton.onClick.RemoveAllListeners();
            if (sendTextButton != null)
                sendTextButton.onClick.RemoveAllListeners();

        }
    }
}