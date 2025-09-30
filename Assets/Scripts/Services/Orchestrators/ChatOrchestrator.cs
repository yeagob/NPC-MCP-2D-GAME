using System;
using System.Threading.Tasks;
using ChatSystem.Models.Context;
using ChatSystem.Models.LLM;
using ChatSystem.Services.Orchestrators.Interfaces;
using ChatSystem.Services.Context.Interfaces;
using ChatSystem.Services.Persistence.Interfaces;
using ChatSystem.Services.Logging;

namespace ChatSystem.Services.Orchestrators
{
    public class ChatOrchestrator : IChatOrchestrator
    {
        private ILLMOrchestrator llmOrchestrator;
        private IContextManager contextManager;
        private IPersistenceService persistenceService;
        
        public void SetLLMOrchestrator(ILLMOrchestrator orchestrator)
        {
            llmOrchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        public Task<LLMResponse> ProcessMessageAsync(string conversationId, Message message)
        {
            throw new NotImplementedException();
        }

        public void SetContextManager(IContextManager manager)
        {
            contextManager = manager ?? throw new ArgumentNullException(nameof(manager));
        }
        
        public void SetPersistenceService(IPersistenceService service)
        {
            persistenceService = service ?? throw new ArgumentNullException(nameof(service));
        }
        
        public async Task<LLMResponse> ProcessUserMessageAsync(string conversationId, string userMessage)
        {
            ValidateOrchestrator();
            
            await contextManager.AddUserMessageAsync(conversationId, userMessage);
            ConversationContext context = await contextManager.GetContextAsync(conversationId);
            
            LLMResponse response = await ExecuteLLMProcessing(context);
            await ProcessLLMResponse(conversationId, response);
            
            //WIP
            if (persistenceService != null)
            {
                await persistenceService.SaveConversationAsync(context);
            }
            
            response.context = context;
            
//            LoggingService.LogDebug($"[ChatOrchestrator] Response Content: {response.content}");
            
            return response;
        }
        
        public async Task<string> GetConversationContextAsync(string conversationId)
        {
            if (contextManager == null)
            {
                LoggingService.LogWarning("ContextManager not available");
                return string.Empty;
            }
            
            ConversationContext context = await contextManager.GetContextAsync(conversationId);
            return context?.ToString() ?? string.Empty;
        }
        
        public async Task ClearConversationAsync(string conversationId)
        {
            LoggingService.LogInfo($"Clearing conversation: {conversationId}");
            
            if (contextManager != null)
            {
                await contextManager.ClearContextAsync(conversationId);
            }
            
            if (persistenceService != null)
            {
                await persistenceService.DeleteConversationAsync(conversationId);
            }
        }
        
        public string GetConversationContext(string conversationId)
        {
            return GetConversationContextAsync(conversationId).Result;
        }
        
        public void ClearConversation(string conversationId)
        {
            ClearConversationAsync(conversationId).Wait();
        }
        
        private void ValidateOrchestrator()
        {
            if (llmOrchestrator == null)
            {
                LoggingService.LogCritical("[ChatOrchestrator] No LLM orchestrator configured");
                throw new InvalidOperationException("LLMOrchestrator not set. Call SetLLMOrchestrator first.");
            }
            
            if (contextManager == null)
            {
                LoggingService.LogCritical("[ChatOrchestrator] No context manager configured");
                throw new InvalidOperationException("ContextManager not set. Call SetContextManager first.");
            }
        }
        
        private void ValidateMessage(Message message)
        {
            if (string.IsNullOrWhiteSpace(message.content))
            {
                LoggingService.LogError($"[ChatOrchestrator] Message content is empty");
                throw new ArgumentException("Message content cannot be empty", nameof(message));
            }
        }
        
        private async Task<LLMResponse> ExecuteLLMProcessing(ConversationContext context)
        {
            try
            {
                return await llmOrchestrator.ProcessMessageAsync(context);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"LLM processing failed: {ex.Message}");
                return CreateErrorResponse(ex);
            }
        }
        
        private async Task ProcessLLMResponse(string conversationId, LLMResponse response)
        {
            if (response.success && !string.IsNullOrEmpty(response.content))
            {
                await contextManager.AddAssistantMessageAsync(conversationId, response.content);
            }
            
            if (response.toolResponses != null)
            {
                foreach (var toolResponse in response.toolResponses)
                {
                    await contextManager.AddToolMessageAsync(conversationId, toolResponse.content, toolResponse.toolCallId);
                }
            }
        }
        
        private LLMResponse CreateErrorResponse(Exception exception)
        {
            return new LLMResponse
            {
                success = false,
                errorMessage = exception.Message,
                content = "I apologize, but I encountered an error processing your request.",
                timestamp = DateTime.UtcNow
            };
        }
    }
}
