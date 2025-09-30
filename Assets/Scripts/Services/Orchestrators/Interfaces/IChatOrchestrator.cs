using System.Threading.Tasks;
using ChatSystem.Models.LLM;
using ChatSystem.Services.Context.Interfaces;
using ChatSystem.Services.Persistence.Interfaces;

namespace ChatSystem.Services.Orchestrators.Interfaces
{
    public interface IChatOrchestrator
    {
        Task<LLMResponse> ProcessUserMessageAsync(string conversationId, string userMessage);
        void SetLLMOrchestrator(ILLMOrchestrator llmOrchestrator);
        void SetContextManager(IContextManager contextManager);
        void SetPersistenceService(IPersistenceService persistenceService);
        Task<string> GetConversationContextAsync(string conversationId);
        Task ClearConversationAsync(string conversationId);
        string GetConversationContext(string conversationId);
        void ClearConversation(string conversationId);
    }
}
