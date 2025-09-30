using System;
using System.Collections.Generic;
using ChatSystem.Enums;
using ChatSystem.Models.LLM;

namespace ChatSystem.Models.Agents
{
    [Serializable]
    public class Agent
    {
        public string agentId;
        public string agentName;
        public AgentState currentState;
        public LLMConfiguration llmConfiguration;
        public List<string> toolSetIds;
        public DateTime createdAt;
        public DateTime lastExecuted;
        
        public Agent()
        {
            agentId = Guid.NewGuid().ToString();
            currentState = AgentState.Idle;
            toolSetIds = new List<string>();
            createdAt = DateTime.UtcNow;
        }
        
        public Agent(string id, string name) : this()
        {
            agentId = id;
            agentName = name;
        }
        
        public bool IsActive => currentState == AgentState.Processing || currentState == AgentState.WaitingForTools;
        public bool HasError => currentState == AgentState.Error || currentState == AgentState.Timeout;
        public bool IsReady => currentState == AgentState.Idle || currentState == AgentState.Completed;
    }
}
