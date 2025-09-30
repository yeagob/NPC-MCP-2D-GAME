using System;
using System.Collections.Generic;
using ChatSystem.Enums;
using ChatSystem.Models.Tools;

namespace ChatSystem.Models.Context
{
    [Serializable]
    public struct Message
    {
        public string id;
        public MessageRole role;
        public MessageType type;
        public string content;
        public string toolCallId;
        public List<ToolCall> toolCalls;
        public DateTime timestamp;
        public string conversationId;

        public Message(string id, MessageRole role, MessageType type, string content, string conversationId)
        {
            this.id = id;
            this.role = role;
            this.type = type;
            this.content = content;
            this.toolCallId = string.Empty;
            this.toolCalls = null;
            this.timestamp = DateTime.UtcNow;
            this.conversationId = conversationId;
        }

        public Message(string id, MessageRole role, MessageType type, string content, string toolCallId, string conversationId)
        {
            this.id = id;
            this.role = role;
            this.type = type;
            this.content = content;
            this.toolCallId = toolCallId;
            this.toolCalls = null;
            this.timestamp = DateTime.UtcNow;
            this.conversationId = conversationId;
        }
        
        public Message(string id, MessageRole role, MessageType type, string content, List<ToolCall> toolCalls, string conversationId)
        {
            this.id = id;
            this.role = role;
            this.type = type;
            this.content = content;
            this.toolCallId = string.Empty;
            this.toolCalls = toolCalls;
            this.timestamp = DateTime.UtcNow;
            this.conversationId = conversationId;
        }
    }
}