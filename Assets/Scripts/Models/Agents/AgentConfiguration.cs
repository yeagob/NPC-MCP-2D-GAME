using System;
using System.Collections.Generic;
using ChatSystem.Configuration.ScriptableObjects;
using ChatSystem.Models.Tools;
using ChatSystem.Enums;

namespace ChatSystem.Models.Agents
{
    [Serializable]
    public class AgentConfiguration
    {
        public string agentId;
        public string agentName;
        public string description;
        public string systemPrompt;
        public List<ToolConfiguration> toolConfigurations;
        public bool canExecuteTools;
        public int maxToolCalls;
        public int maxResponseTokens;
        public bool streamResponses;
        public int maxRetries;
        public int timeoutMs;
        public bool fallbackToSimulation;
        public bool enabled;
        public int priority;
        public ServiceProvider provider;
        public string modelName;
        public float temperature;
        
        public AgentConfiguration()
        {
            toolConfigurations = new List<ToolConfiguration>();
            canExecuteTools = true;
            maxToolCalls = 5;
            maxResponseTokens = 2048;
            maxRetries = 3;
            timeoutMs = 10000;
            enabled = true;
            priority = 0;
            provider = ServiceProvider.Custom;
            temperature = 0.7f;
        }
        
        public AgentConfiguration(AgentConfig config)
        {
            if (config == null) return;
            
            agentId = config.agentId;
            agentName = config.agentName;
            description = config.description;
            systemPrompt = config.GetFullSystemPrompt();
            canExecuteTools = config.canExecuteTools;
            maxToolCalls = config.maxToolCalls;
            maxResponseTokens = config.maxResponseTokens;
            streamResponses = config.streamResponses;
            maxRetries = config.maxRetries;
            timeoutMs = config.timeoutMs;
            fallbackToSimulation = config.fallbackToSimulation;
            enabled = config.enabled;
            priority = config.priority;

            if (config.modelConfig != null)
            {
                provider = config.modelConfig.provider;
                modelName = config.modelConfig.modelName;
                temperature = config.modelConfig.temperature;
            }
            else
            {
                provider = ServiceProvider.Custom;
                modelName = "default";
                temperature = 0.7f;
            }
            
            toolConfigurations = new List<ToolConfiguration>();
            if (config.availableTools != null)
            {
                foreach (ToolConfig toolConfig in config.availableTools)
                {
                    if (toolConfig != null)
                    {
                        toolConfigurations.Add(new ToolConfiguration(toolConfig));
                    }
                }
            }
        }
    }
}