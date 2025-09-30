using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSystem.Models.Context;
using ChatSystem.Models.Agents;
using ChatSystem.Configuration.ScriptableObjects;
using ChatSystem.Services.Tools.Interfaces;

namespace ChatSystem.Services.Agents.Interfaces
{
    public interface IAgentExecutor
    {
        Task<AgentResponse> ExecuteAgentAsync(string agentId, ConversationContext context);
        void RegisterAgent(AgentConfig agentConfig);
        void RegisterToolSet(IToolSet toolSet);
        void UnregisterToolSet(string toolSetName);
        List<string> GetRegisteredToolSets();
    }
}