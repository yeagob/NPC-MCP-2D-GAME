using UnityEngine;
using System.Collections.Generic;

namespace ChatSystem.Configuration.ScriptableObjects
{
    [CreateAssetMenu(fileName = "AgentConfig", menuName = "LLM/Agent Configuration", order = 0)]
    public class AgentConfig : ScriptableObject
    {
        [Header("Agent Identification")]
        public string agentId;
        public string agentName;
        [TextArea(3, 5)]
        public string description;
        
        [Header("Provider Configuration")]
        public ProviderConfiguration providerConfig;
        
        [Header("Model Configuration")]
        public ModelConfig modelConfig;
        
        [Header("Prompt Configuration")]
        public PromptConfig systemPrompt;
        public List<PromptConfig> contextPrompts;
        
        [Header("Tool Configuration")]
        public List<ToolConfig> availableTools;
        public bool debugTools = true;
        public bool canExecuteTools = true;
        public int maxToolCalls = 5;
        
        [Header("Response Settings")]
        [Range(100, 8192)]
        public int maxResponseTokens = 2048;
        public bool streamResponses;
        
        [Header("Error Handling")]
        [Range(0, 10)]
        public int maxRetries = 3;
        [Range(1000, 30000)]
        public int timeoutMs = 10000;
        public bool fallbackToSimulation;
        
        [Header("Advanced Settings")]
        public bool enabled = true;
        public int priority = 0;
        public List<string> requiredCapabilities;
        
        public string token => providerConfig?.token ?? string.Empty;
        public string serviceUrl => providerConfig?.serviceUrl ?? string.Empty;
        
        public AgentConfig()
        {
            contextPrompts = new List<PromptConfig>();
            availableTools = new List<ToolConfig>();
            requiredCapabilities = new List<string>();
        }
        
        public string GetFullSystemPrompt()
        {
            if (systemPrompt == null) return string.Empty;
            
            string fullPrompt = systemPrompt.content;
            
            foreach (PromptConfig contextPrompt in contextPrompts)
            {
                if (contextPrompt != null && contextPrompt.enabled)
                {
                    fullPrompt += "\n\n" + contextPrompt.content;
                }
            }
            
            return fullPrompt;
        }
        
        public List<string> GetToolDefinitions()
        {
            List<string> definitions = new List<string>();
            
            if (!canExecuteTools) return definitions;
            
            foreach (ToolConfig tool in availableTools)
            {
                if (tool != null && tool.enabled)
                {
                    definitions.Add(tool.GetMCPFormat());
                }
            }
            
            return definitions;
        }
    }
}
