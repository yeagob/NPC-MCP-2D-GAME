using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSystem.Models.Context;

namespace ChatSystem.Services.Context.Interfaces
{
    public interface IContextManager
    {
        Task<ConversationContext> GetContextAsync(string conversationId);
        Task UpdateContextAsync(string conversationId, ConversationContext context);
        Task AddMessageAsync(string conversationId, Message message);
        Task AddUserMessageAsync(string conversationId, string content);
        Task AddAssistantMessageAsync(string conversationId, string content);
        Task AddToolMessageAsync(string conversationId, string content, string toolCallId);
        Task AddSystemMessageAsync(string conversationId, string content);
        Task<List<Message>> GetMessagesAsync(string conversationId);
        Task<bool> ExistsAsync(string conversationId);
        Task CreateConversationAsync(string conversationId);
        Task ClearContextAsync(string conversationId);
        Task<int> GetMessageCountAsync(string conversationId);
    }
}
