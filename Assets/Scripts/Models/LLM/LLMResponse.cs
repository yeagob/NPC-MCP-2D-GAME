using System;
using System.Collections.Generic;
using ChatSystem.Models.Context;
using ChatSystem.Models.Tools;

namespace ChatSystem.Models.LLM
{
    //TODO: Esta clase se podria optimizar/refactorizar!
    [Serializable]
    public class LLMResponse
    {
        public string responseId;
        public string requestId;
        public string content;
        public List<ToolCall> toolCalls;
        public List<ToolResponse> toolResponses;
        public List<string> promptsUsed;
        public Dictionary<string, object> usage;
        public int inputTokens;
        public int outputTokens;
        public decimal totalCost;
        public bool success;
        public string errorMessage;
        public DateTime timestamp;
        public string model;
        public ConversationContext context;
        
        
        public LLMResponse()
        {
            responseId = Guid.NewGuid().ToString();
            toolCalls = new List<ToolCall>();
            toolResponses = new List<ToolResponse>();
            usage = new Dictionary<string, object>();
            promptsUsed = new List<string>();
            timestamp = DateTime.UtcNow;
            success = true;
        }
        
        public bool HasToolCalls => toolCalls != null && toolCalls.Count > 0;
        public bool HasError => !success || !string.IsNullOrEmpty(errorMessage);
    }
}
