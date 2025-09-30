using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSystem.Models.Context;
using ChatSystem.Enums;
using ChatSystem.Services.Context.Interfaces;
using ChatSystem.Services.Logging;

namespace ChatSystem.Services.Context
{
    public class ContextManager : IContextManager
    {
        private readonly Dictionary<string, ConversationContext> contexts;
        
        public ContextManager()
        {
            contexts = new Dictionary<string, ConversationContext>();
        }
        
        public async Task<ConversationContext> GetContextAsync(string conversationId)
        {
            if (!contexts.ContainsKey(conversationId))
            {
                await CreateConversationAsync(conversationId);
            }
            
            return contexts[conversationId];
        }
        
        public async Task UpdateContextAsync(string conversationId, ConversationContext context)
        {
            contexts[conversationId] = context;
            
            await Task.CompletedTask;
        }
        
        public async Task AddMessageAsync(string conversationId, Message message)
        {
            ConversationContext context = await GetContextAsync(conversationId);
            context.AddMessage(message.role, message.content);
            
            await UpdateContextAsync(conversationId, context);
        }
        
        public async Task AddUserMessageAsync(string conversationId, string content)
        {
            Message message = CreateMessage(conversationId, MessageRole.User, content);
            await AddMessageAsync(conversationId, message);
        }
        
        public async Task AddAssistantMessageAsync(string conversationId, string content)
        {
            Message message = CreateMessage(conversationId, MessageRole.Assistant, content);
            await AddMessageAsync(conversationId, message);
        }
        
        public async Task AddToolMessageAsync(string conversationId, string content, string toolCallId)
        {
            Message message = CreateMessage(conversationId, MessageRole.Tool, content, toolCallId);
            await AddMessageAsync(conversationId, message);
        }
        
        public async Task AddSystemMessageAsync(string conversationId, string content)
        {
            Message message = CreateMessage(conversationId, MessageRole.System, content);
            await AddMessageAsync(conversationId, message);
        }
        
        public async Task<List<Message>> GetMessagesAsync(string conversationId)
        {
            ConversationContext context = await GetContextAsync(conversationId);
            return context.GetAllMessages();
        }
        
        public async Task<bool> ExistsAsync(string conversationId)
        {
            await Task.CompletedTask;
            return contexts.ContainsKey(conversationId);
        }
        
        public async Task CreateConversationAsync(string conversationId)
        {
            ConversationContext newContext = new ConversationContext(conversationId);
            contexts[conversationId] = newContext;
            
            await Task.CompletedTask;
        }
        
        public async Task ClearContextAsync(string conversationId)
        {
            LoggingService.LogInfo($"Clearing context for conversation: {conversationId}");
            
            if (contexts.ContainsKey(conversationId))
            {
                contexts[conversationId].Clear();
            }
            
            await Task.CompletedTask;
        }
        
        public async Task<int> GetMessageCountAsync(string conversationId)
        {
            ConversationContext context = await GetContextAsync(conversationId);
            await Task.CompletedTask;
            return context.GetAllMessages().Count;
        }
        
        private Message CreateMessage(string conversationId, MessageRole role, string content, string toolCallId = null)
        {
            return new Message
            {
                id = Guid.NewGuid().ToString(),
                role = role,
                type = GetMessageType(role),
                content = content,
                toolCallId = toolCallId,
                timestamp = DateTime.UtcNow,
                conversationId = conversationId
            };
        }
        
        private MessageType GetMessageType(MessageRole role)
        {
            return role switch
            {
                MessageRole.User => MessageType.Text,
                MessageRole.Assistant => MessageType.Text,
                MessageRole.Tool => MessageType.ToolResponse,
                MessageRole.System => MessageType.SystemPrompt,
                _ => MessageType.Text
            };
        }
    }
}
