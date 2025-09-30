using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChatSystem.Models.Context;
using ChatSystem.Views.Interfaces;
using ChatSystem.Controllers.Interfaces;

namespace ChatSystem.Views.Chat
{
    public class ChatView : MonoBehaviour, IChatView, IResponsable
    {
        [Header("UI Components")]
        [SerializeField] private ScrollRect chatScrollRect;
        [SerializeField] private Transform messageContainer;
        [SerializeField] private TMP_InputField messageInputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private GameObject loadingIndicator;

        [Header("Message Prefabs")]
        [SerializeField] private GameObject messageViewPrefab;

        private IChatController chatController;
        private List<MessageView> displayedMessages;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            SetupEventListeners();
            FocusInputField();
        }

        public void SetController(IChatController controller)
        {
            chatController = controller;
            chatController.SetResponseTarget(this);
        }

        private void InitializeComponents()
        {
            displayedMessages = new List<MessageView>();
            
            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(false);
            }
        }

        private void SetupEventListeners()
        {
            if (sendButton != null)
            {
                sendButton.onClick.AddListener(OnSendButtonClicked);
            }

            if (messageInputField != null)
            {
                messageInputField.onSubmit.AddListener(OnMessageSubmitted);
                messageInputField.onValueChanged.AddListener(OnInputValueChanged);
            }
        }

        private void OnSendButtonClicked()
        {
            SendCurrentMessage();
        }

        private void OnMessageSubmitted(string message)
        {
            SendCurrentMessage();
        }

        private void OnInputValueChanged(string text)
        {
            if (sendButton != null)
            {
                sendButton.interactable = !string.IsNullOrWhiteSpace(text);
            }
        }

        private async void SendCurrentMessage()
        {
            if (string.IsNullOrWhiteSpace(messageInputField.text) || chatController == null)
                return;

            string messageText = messageInputField.text.Trim();
            
            ClearInputAndMaintainFocus();
            SetInputEnabled(false);
            ShowLoadingIndicator(true);

            await chatController.ProcessUserMessageAsync(messageText);
        }

        private void ClearInputAndMaintainFocus()
        {
            if (messageInputField != null)
            {
                messageInputField.text = string.Empty;
                messageInputField.ActivateInputField();
                messageInputField.Select();
            }
        }

        private void FocusInputField()
        {
            if (messageInputField != null)
            {
                messageInputField.ActivateInputField();
                messageInputField.Select();
            }
        }

        public void DisplayMessage(Message message)
        {
            CreateMessageView(message);
            ScrollToBottom();
        }

        public void DisplayMessages(List<Message> messages)
        {
            ClearMessages();
            
            foreach (Message message in messages)
            {
                CreateMessageView(message);
            }
            
            ScrollToBottom();
        }

        public void ClearMessages()
        {
            foreach (MessageView messageView in displayedMessages)
            {
                if (messageView != null)
                {
                    DestroyImmediate(messageView.gameObject);
                }
            }
            
            displayedMessages.Clear();
        }

        public void SetInputEnabled(bool enabled)
        {
            if (messageInputField != null)
            {
                messageInputField.interactable = enabled;
                
                if (enabled)
                {
                    FocusInputField();
                }
            }
            
            if (sendButton != null)
            {
                bool hasText = !string.IsNullOrWhiteSpace(messageInputField?.text);
                sendButton.interactable = enabled && hasText;
            }
        }

        public void ShowLoadingIndicator(bool show)
        {
            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(show);
            }
        }

        public void ReceiveResponse(Message message)
        {
            DisplayMessage(message);
            SetInputEnabled(true);
            ShowLoadingIndicator(false);
        }

        public void ReceiveError(string errorMessage)
        {
            Debug.LogError($"Chat Error: {errorMessage}");
            SetInputEnabled(true);
            ShowLoadingIndicator(false);
        }

        private void CreateMessageView(Message message)
        {
            if (messageViewPrefab == null || messageContainer == null)
                return;

            GameObject messageObject = Instantiate(messageViewPrefab, messageContainer);
            MessageView messageView = messageObject.GetComponent<MessageView>();
            
            if (messageView != null)
            {
                messageView.Initialize(message);
                displayedMessages.Add(messageView);
            }
        }

        private void ScrollToBottom()
        {
            if (chatScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                chatScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void OnDestroy()
        {
            if (sendButton != null)
            {
                sendButton.onClick.RemoveAllListeners();
            }
            
            if (messageInputField != null)
            {
                messageInputField.onSubmit.RemoveAllListeners();
                messageInputField.onValueChanged.RemoveAllListeners();
            }
        }
    }
}
