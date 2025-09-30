using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSystem.Models.Context;
using ChatSystem.Models.Agents;
using ChatSystem.Models.LLM;
using ChatSystem.Configuration.ScriptableObjects;
using ChatSystem.Services.Orchestrators.Interfaces;
using ChatSystem.Services.Agents.Interfaces;
using ChatSystem.Services.Logging;

namespace ChatSystem.Services.Orchestrators
{
    public class LLMOrchestrator : ILLMOrchestrator
    {
        private readonly IAgentExecutor agentExecutor;
        private readonly List<string> activeAgentIds;
        private readonly Dictionary<string, AgentConfig> loadedConfigs;
        
        public LLMOrchestrator(IAgentExecutor agentExecutor)
        {
            this.agentExecutor = agentExecutor ?? throw new ArgumentNullException(nameof(agentExecutor));
            activeAgentIds = new List<string>();
            loadedConfigs = new Dictionary<string, AgentConfig>();
        }
        
        public void RegisterAgentConfig(AgentConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.agentId))
            {
                LoggingService.LogError("Invalid agent configuration");
                return;
            }
            
            loadedConfigs[config.agentId] = config;
            agentExecutor.RegisterAgent(config);
            
            if (config.enabled)
            {
                activeAgentIds.Add(config.agentId);
            }
        }
        
        public async Task<LLMResponse> ProcessMessageAsync(ConversationContext context)
        {
            if (activeAgentIds.Count == 0)
            {
                LoggingService.LogError("[LLMOrchestator] No active agents available!");
                return CreateDefaultResponse("No active agents configured");
            }
            
            try
            {
                List<AgentResponse> agentResponses = new List<AgentResponse>();
                
                foreach (string agentId in activeAgentIds)
                {
                    AgentResponse response = await agentExecutor.ExecuteAgentAsync(agentId, context);
                    
                    if (response.success)
                    {
                        agentResponses.Add(response);
                        
                        //Si response.toolResponses no llegara vacío, esto sería redundante
                        if (response.toolCalls != null && response.toolCalls.Count > 0)
                        {
                            context.AddAssistantMessage(response.content);
                        }
                    }
                }
                
//                LoggingService.LogDebug($"[LLMOrchestrator] Agent responses: {agentResponses.Count}");  
                
                return MergeAgentResponses(agentResponses);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"LLMOrchestrator error: {ex.Message}");
                return CreateDefaultResponse($"Error processing message: {ex.Message}");
            }
        }
        
        public List<string> GetActiveAgents()
        {
            return new List<string>(activeAgentIds);
        }
        
        public void EnableAgent(string agentId)
        {
            if (loadedConfigs.ContainsKey(agentId) && !activeAgentIds.Contains(agentId))
            {
                activeAgentIds.Add(agentId);
            }
        }
        
        public void DisableAgent(string agentId)
        {
            if (activeAgentIds.Remove(agentId))
            {
                LoggingService.LogInfo($"Agent {agentId} disabled");
            }
        }
        
        private LLMResponse MergeAgentResponses(List<AgentResponse> responses)
        {
            if (responses.Count == 0)
            {
                LoggingService.LogError("[LLMOrchestrator] No responses were merged!");
                return CreateDefaultResponse("No agent responses received");
            }
            
            AgentResponse primaryResponse = responses[0];
            
            string mergedContent = primaryResponse.content;
            if (responses.Count > 1)
            {
                for (int i = 1; i < responses.Count; i++)
                {
                    if (!string.IsNullOrEmpty(responses[i].content))
                    {
                        mergedContent += "\n\n" + responses[i].content;
                    }
                }
            }
            
            return new LLMResponse
            {
                content = mergedContent,
                toolCalls = primaryResponse.toolCalls,
                model = loadedConfigs.ContainsKey(primaryResponse.agentId) ? 
                    loadedConfigs[primaryResponse.agentId].modelConfig?.modelName ?? "default" : 
                    "default",
                timestamp = DateTime.UtcNow,
                success = true
            };
        }
        
        private LLMResponse CreateDefaultResponse(string message)
        {
            return new LLMResponse
            {
                content = message,
                model = "default",
                timestamp = DateTime.UtcNow,
                success = false
            };
        }
    }
}
