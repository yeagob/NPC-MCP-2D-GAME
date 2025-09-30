using System;
using System.Collections.Generic;
using ChatSystem.Models.Tools;
using ChatSystem.Enums;

namespace ChatSystem.Models.Agents
{
    [Serializable]
    public class AgentResponse
    {
        public string agentId;
        public string responseId;
        public string content;
        public List<ToolCall> toolCalls;
        public List<ToolResponse> toolResponses;
        public AgentState finalState;
        public bool success;
        public string errorMessage;
        public int processingTimeMs;
        public DateTime timestamp;
        public AgentState state;
        public Dictionary<string, object> usage;


        public AgentResponse()
        {
            responseId = Guid.NewGuid().ToString();
            toolCalls = new List<ToolCall>();
            toolResponses = new List<ToolResponse>();
            usage = new Dictionary<string, object>();
            finalState = AgentState.Completed;
            success = true;
            timestamp = DateTime.UtcNow;
        }
        
        public AgentResponse(string agentIdentifier) : this()
        {
            agentId = agentIdentifier;
        }
        
        public bool HasToolCalls => toolCalls != null && toolCalls.Count > 0;
        public bool HasToolResponses => toolResponses != null && toolResponses.Count > 0;
        public bool HasError => !success || finalState == AgentState.Error;
    }
}
