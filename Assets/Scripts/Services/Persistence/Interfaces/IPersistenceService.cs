using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSystem.Models.Context;
using ChatSystem.Models.Agents;

namespace ChatSystem.Services.Persistence.Interfaces
{
    public interface IPersistenceService
    {
        Task SaveConversationAsync(ConversationContext conversation);
        Task<ConversationContext> LoadConversationAsync(string conversationId);
        Task<List<ConversationContext>> LoadAllConversationsAsync();
        Task DeleteConversationAsync(string conversationId);
        Task<bool> ConversationExistsAsync(string conversationId);
        Task SaveAgentResponseAsync(string conversationId, AgentResponse response);
        Task<List<AgentResponse>> LoadAgentResponsesAsync(string conversationId);
        Task ClearAllDataAsync();
        Task<long> GetStorageSizeAsync();
        Task BackupDataAsync(string backupPath);
        Task RestoreDataAsync(string backupPath);
    }
}
