using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ChatSystem.Controllers.Interfaces;
using ChatSystem.Models.Context;
using ChatSystem.Models.LLM;
using ChatSystem.Views.Interfaces;
using ChatSystem.Services.Orchestrators.Interfaces;
using ChatSystem.Services.Context.Interfaces;
using ChatSystem.Services.Logging;
using ChatSystem.Enums;

namespace ChatSystem.Examples
{
    public class ChatController : IChatController
    {
        private string defaultConversationId;
        private IResponsable responseTarget;
        private IChatOrchestrator chatOrchestrator;
        private IContextManager contextManager;
        private int lastDisplayedMessageCount;

        public ChatController(string conversationId = "default-conversation")
        {
            defaultConversationId = conversationId;
            lastDisplayedMessageCount = 0;
        }

        public void SetChatOrchestrator(IChatOrchestrator orchestrator)
        {
            chatOrchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        public void SetContextManager(IContextManager manager)
        {
            contextManager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        public void InitializeConversation(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                conversationId = Guid.NewGuid().ToString();
            }

            defaultConversationId = conversationId;
            lastDisplayedMessageCount = 0;
        }

        public void SetResponseTarget(IResponsable target)
        {
            responseTarget = target;
            LoggingService.LogDebug($"[ChatController] Response target set: {target?.GetType().Name ?? "null"}");
        }

        public async Task ProcessUserMessageAsync(string messageText)
        {
            if (string.IsNullOrWhiteSpace(messageText))
            {
                LoggingService.LogWarning("[ChatController] Invalid message");
                return;
            }

            try
            {
                await HandleUserMessage(messageText);
            }
            catch (Exception ex)
            {
                HandleProcessingError(ex);
            }
        }

        public ConversationContext GetCurrentContext()
        {
            if (contextManager == null) return null;
            
            try
            {
                return contextManager.GetContextAsync(defaultConversationId).Result;
            }
            catch
            {
                return null;
            }
        }

        public void ClearConversation()
        {
            if (chatOrchestrator != null)
            {
                chatOrchestrator.ClearConversation(defaultConversationId);
                lastDisplayedMessageCount = 0;
            }
        }

        private async Task HandleUserMessage(string messageText)
        {
            DisplayUserMessage(messageText);
            
            if (chatOrchestrator != null)
            {
                await ProcessWithOrchestrator(messageText);
            }
            else
            {
                LoggingService.LogError($"[ChatController] ChatOrchestrator is null");
            }
        }

        private void DisplayUserMessage(string messageText)
        {
            Message userMessage = new Message
            {
                id = Guid.NewGuid().ToString(),
                role = MessageRole.User,
                type = MessageType.Text,
                content = messageText,
                timestamp = DateTime.UtcNow,
                conversationId = defaultConversationId
            };
            
            responseTarget?.ReceiveResponse(userMessage);
            LogUserMessage(messageText);
        }

        private async Task ProcessWithOrchestrator(string messageText)
        {
            try
            {
                LLMResponse response = await chatOrchestrator.ProcessUserMessageAsync(
                    defaultConversationId, 
                    messageText
                );

                await DisplayNewMessages(response.context);

                if (!response.success)
                {
                    string errorMsg = response.errorMessage ?? "Unknown orchestrator error";
                    LoggingService.LogError($"[ChatController] Orchestrator failed. Error: {errorMsg}");
                    HandleOrchestratorError(errorMsg);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[ChatController] Exception in ProcessWithOrchestrator: {ex.Message}");
                HandleProcessingError(ex);
            }
        }

        private async Task DisplayNewMessages(ConversationContext context)
        {
            if (responseTarget == null)
            {
                return;
            }

            try
            {
                if (context == null)
                {
                    return;
                }

                List<Message> allMessages = context.GetAllMessages();
                int currentMessageCount = allMessages.Count;

                if (currentMessageCount > lastDisplayedMessageCount)
                {
                    for (int i = lastDisplayedMessageCount; i < currentMessageCount; i++)
                    {
                        Message message = allMessages[i];
                        
                        if (message.role != MessageRole.User)
                        {
                            responseTarget.ReceiveResponse(message);
                            LoggingService.LogDebug($"[ChatController] Displayed message: {message.role} - {GetMessagePreview(message.content)}");
                        }
                    }
                    lastDisplayedMessageCount = currentMessageCount;
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[ChatController] Error displaying messages: {ex.Message}");
            }
        }

        private string GetMessagePreview(string content)
        {
            if (string.IsNullOrEmpty(content)) return "";
            return content.Length > 50 ? content.Substring(0, 50) + "..." : content;
        }

        private void HandleOrchestratorError(string errorMessage)
        {
            string fullError = $"Orchestrator error: {errorMessage}";
            responseTarget?.ReceiveError(fullError);
            LoggingService.LogError($"[ChatController] {fullError}");
        }

        private void HandleProcessingError(Exception ex)
        {
            string errorMessage = $"Error processing message: {ex.Message}";
            responseTarget?.ReceiveError(errorMessage);
            LoggingService.LogError($"[ChatController] {errorMessage}");
        }

        private void LogUserMessage(string message)
        {
            LoggingService.LogInfo($"[ChatController] User message received: {message}");
        }
    }
}
