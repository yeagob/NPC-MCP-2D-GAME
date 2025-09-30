using System;
using System.Collections.Generic;

namespace ChatSystem.Models.LLM.OpenAI
{
    [Serializable]
    public struct OpenAIResponse
    {
        public long created;
        public string model;
        public List<OpenAIChoice> choices;
        public OpenAIUsage usage;
        public string service_tier;
        public string system_fingerprint;
    }
}
