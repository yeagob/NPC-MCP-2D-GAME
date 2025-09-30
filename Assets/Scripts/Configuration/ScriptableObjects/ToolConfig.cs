using UnityEngine;
using ChatSystem.Models.Tools;
using ChatSystem.Models.Tools.MCP;
using ChatSystem.Enums;
using System;

namespace ChatSystem.Configuration.ScriptableObjects
{
    [CreateAssetMenu(fileName = "ToolConfig", menuName = "LLM/Tool Configuration", order = 1)]
    public class ToolConfig : ScriptableObject
    {
        [Header("Tool Identification")]
        public string toolId;
        public string toolName;
        public ToolType toolType;
        
        [Header("MCP Function Definition")]
        public FunctionDefinition function;
        
        [Header("Annotations (MCP)")]
        public ToolAnnotations annotations;
        
        [Header("Execution Settings")]
        public bool enabled = true;
        public bool requiresAuthentication;
        [Range(0, 30000)]
        public int timeoutMs = 5000;
        [Range(0, 10)]
        public int maxRetries = 3;
        
        [Header("Rate Limiting")]
        public bool hasRateLimit;
        [Range(1, 1000)]
        public int requestsPerMinute = 60;
        
        public string GetMCPFormat()
        {
            return "{ \"type\": \"function\", \"function\": " + 
                   SerializeFunction() + " }";
        }
        
        private string SerializeFunction()
        {
            string properties = "{}";
            if (function.parameters != null && function.parameters.properties != null)
            {
                properties = SerializeProperties();
            }
            
            string required = "[]";
            if (function.parameters != null && function.parameters.required != null)
            {
                required = "[" + string.Join(",", 
                    function.parameters.required.ConvertAll(r => "\"" + r + "\"")) + "]";
            }
            
            return "{ " +
                   "\"name\": \"" + function.name + "\", " +
                   "\"description\": \"" + function.description + "\", " +
                   "\"parameters\": { " +
                   "\"type\": \"object\", " +
                   "\"properties\": " + properties + ", " +
                   "\"required\": " + required + " } }";
        }
        
        private string SerializeProperties()
        {
            string result = "{";
            bool first = true;
            
            foreach (SerializableProperty prop in function.parameters.properties)
            {
                if (string.IsNullOrEmpty(prop.key)) continue;
                
                if (!first) result += ",";
                first = false;
                
                result += "\"" + prop.key + "\": {";
                result += "\"type\": \"" + prop.value.type + "\"";
                
                if (!string.IsNullOrEmpty(prop.value.description))
                    result += ", \"description\": \"" + prop.value.description + "\"";
                
                if (prop.value.items != null)
                {
                    result += ", \"items\": {";
                    result += "\"type\": \"" + prop.value.items.type + "\"";
                    if (!string.IsNullOrEmpty(prop.value.items.description))
                        result += ", \"description\": \"" + prop.value.items.description + "\"";
                    result += "}";
                }
                
                if (prop.value.enumValues != null && prop.value.enumValues.Count > 0)
                {
                    result += ", \"enum\": [";
                    result += string.Join(",", prop.value.enumValues.ConvertAll(e => "\"" + e + "\""));
                    result += "]";
                }
                
                result += "}";
            }
            
            result += "}";
            return result;
        }
    }
}