using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChatSystem.Models.Context;
using ChatSystem.Enums;

namespace ChatSystem.Views.Chat
{
    public class MessageView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private RectTransform messageContainer;
        [SerializeField] private Image backgroundImage;
        
        [Header("User Message Colors")]
        [SerializeField] private Color userTextColor = Color.white;
        [SerializeField] private Color userBackgroundColor = new Color(0.2f, 0.4f, 0.8f, 0.8f);
        
        [Header("Assistant Message Colors")]
        [SerializeField] private Color assistantTextColor = Color.cyan;
        [SerializeField] private Color assistantBackgroundColor = new Color(0.3f, 0.6f, 0.3f, 0.8f);
        
        [Header("Tool Message Colors")]
        [SerializeField] private Color toolTextColor = Color.yellow;
        [SerializeField] private Color toolBackgroundColor = new Color(0.8f, 0.6f, 0.2f, 0.8f);
        
        [Header("System Message Colors")]
        [SerializeField] private Color systemTextColor = Color.gray;
        [SerializeField] private Color systemBackgroundColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);

        private Message currentMessage;

        public void Initialize(Message message)
        {
            currentMessage = message;
            SetupMessage();
        }

        private void SetupMessage()
        {
            ConfigureMessageAppearance();
            SetMessageContent();
            ConfigureAlignment();
        }

        private void ConfigureMessageAppearance()
        {
            switch (currentMessage.role)
            {
                case MessageRole.User:
                    ConfigureUserMessage();
                    break;
                case MessageRole.Assistant:
                    ConfigureAssistantMessage();
                    break;
                case MessageRole.Tool:
                    ConfigureToolMessage();
                    break;
                case MessageRole.System:
                    ConfigureSystemMessage();
                    break;
            }
        }

        private void ConfigureUserMessage()
        {
            SetTextColor(userTextColor);
            SetBackgroundColor(userBackgroundColor);
            SetAnchors(0f, 0f, 0.8f, 1f);
        }

        private void ConfigureAssistantMessage()
        {
            SetTextColor(assistantTextColor);
            SetBackgroundColor(assistantBackgroundColor);
            SetAnchors(0.2f, 0f, 1f, 1f);
        }

        private void ConfigureToolMessage()
        {
            SetTextColor(toolTextColor);
            SetBackgroundColor(toolBackgroundColor);
            SetAnchors(0.1f, 0f, 0.9f, 1f);
        }

        private void ConfigureSystemMessage()
        {
            SetTextColor(systemTextColor);
            SetBackgroundColor(systemBackgroundColor);
            SetAnchors(0f, 0f, 1f, 1f);
        }

        private void SetTextColor(Color textColor)
        {
            if (messageText != null)
            {
                messageText.color = textColor;
            }
        }

        private void SetBackgroundColor(Color backgroundColor)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = backgroundColor;
            }
        }

        private void SetAnchors(float minX, float minY, float maxX, float maxY)
        {
            if (messageContainer != null)
            {
                messageContainer.anchorMin = new Vector2(minX, minY);
                messageContainer.anchorMax = new Vector2(maxX, maxY);
            }
        }

        private void SetMessageContent()
        {
            string formattedContent = FormatMessageContent();
            if (messageText != null)
            {
                messageText.text = formattedContent;
            }
        }

        private string FormatMessageContent()
        {
            string prefix = GetRolePrefix();
            string timestamp = currentMessage.timestamp.ToString("HH:mm");
            return $"<size=80%>[{timestamp}]</size> {prefix}{currentMessage.content}";
        }

        private string GetRolePrefix()
        {
            switch (currentMessage.role)
            {
                case MessageRole.User:
                    return "<b>You:</b> ";
                case MessageRole.Assistant:
                    return "<b>Assistant:</b> ";
                case MessageRole.Tool:
                    return "<i>[Tool Response]</i> ";
                case MessageRole.System:
                    return "<i>[System]</i> ";
                default:
                    return "";
            }
        }

        private void ConfigureAlignment()
        {
            if (messageContainer != null)
            {
                float preferredHeight = GetPreferredHeight();
                messageContainer.sizeDelta = new Vector2(messageContainer.sizeDelta.x, preferredHeight);
            }
        }

        private float GetPreferredHeight()
        {
            if (messageText != null)
            {
                return messageText.preferredHeight + 20f;
            }
            return 50f;
        }
    }
}
