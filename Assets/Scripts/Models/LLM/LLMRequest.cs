using System;
using System.Collections.Generic;
using ChatSystem.Models.Context;
using ChatSystem.Models.Tools;
using ChatSystem.Enums;

namespace ChatSystem.Models.LLM
{
    [Serializable]
    public class LLMRequest
    {
        public List<Message> messages;
        public List<ToolConfiguration> tools;
        public int maxTokens;
        public float temperature;
        public string model;
        public ServiceProvider provider;
        //TODO: Add More request control
        
        public LLMRequest()
        {
            messages = new List<Message>();
            tools = new List<ToolConfiguration>();
            maxTokens = 2048;
            temperature = 0.7f;
            model = "default";
            provider = ServiceProvider.Custom;
        }
    }
}