using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatSystem.Models.Context;
using ChatSystem.Models.Agents;
using ChatSystem.Services.Persistence.Interfaces;
using ChatSystem.Services.Logging;

namespace ChatSystem.Services.Persistence
{
    public class PersistenceService : IPersistenceService
    {
        private readonly Dictionary<string, ConversationContext> conversations;
        private readonly Dictionary<string, List<AgentResponse>> agentResponses;
        
        public PersistenceService()
        {
            conversations = new Dictionary<string, ConversationContext>();
            agentResponses = new Dictionary<string, List<AgentResponse>>();
        }
        
        public async Task SaveConversationAsync(ConversationContext conversation)
        {
            
            conversations[conversation.conversationId] = conversation;
            await Task.CompletedTask;
        }
        
        public async Task<ConversationContext> LoadConversationAsync(string conversationId)
        {
            await Task.CompletedTask;
            
            if (conversations.TryGetValue(conversationId, out ConversationContext conversation))
            {
                return conversation;
            }
            
            return null;
        }
        
        public async Task<List<ConversationContext>> LoadAllConversationsAsync()
        {
            await Task.CompletedTask;
            return conversations.Values.ToList();
        }
        
        public async Task DeleteConversationAsync(string conversationId)
        {
            conversations.Remove(conversationId);
            agentResponses.Remove(conversationId);
            
            await Task.CompletedTask;
        }
        
        public async Task<bool> ConversationExistsAsync(string conversationId)
        {
            await Task.CompletedTask;
            return conversations.ContainsKey(conversationId);
        }
        
        public async Task SaveAgentResponseAsync(string conversationId, AgentResponse response)
        {
            if (!agentResponses.ContainsKey(conversationId))
            {
                agentResponses[conversationId] = new List<AgentResponse>();
            }
            
            agentResponses[conversationId].Add(response);
            await Task.CompletedTask;
        }
        
        public async Task<List<AgentResponse>> LoadAgentResponsesAsync(string conversationId)
        {
            await Task.CompletedTask;
            
            if (agentResponses.TryGetValue(conversationId, out List<AgentResponse> responses))
            {
                return responses;
            }
            
            return new List<AgentResponse>();
        }
        
        public async Task ClearAllDataAsync()
        {
            LoggingService.LogWarning("Clearing allpersistence data");
            
            conversations.Clear();
            agentResponses.Clear();
            
            await Task.CompletedTask;
        }
        
        public async Task<long> GetStorageSizeAsync()
        {
            await Task.CompletedTask;
            
            long size = 0;
            size += conversations.Count * 1024;
            size += agentResponses.Values.Sum(list => list.Count * 512);
            
            LoggingService.LogDebug($"Estimated storage size: {size} bytes");
            return size;
        }
        
        public async Task BackupDataAsync(string backupPath)
        {
            LoggingService.LogInfo($"Backup requested to: {backupPath}");
            LoggingService.LogWarning("Backup functionality not implemented in in-memory mode");
            
            await Task.CompletedTask;
        }
        
        public async Task RestoreDataAsync(string backupPath)
        {
            LoggingService.LogInfo($"Restore requested from: {backupPath}");
            LoggingService.LogWarning("Restore functionality not implemented in in-memory mode");
            
            await Task.CompletedTask;
        }
    }
}
