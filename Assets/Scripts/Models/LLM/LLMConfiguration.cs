using System;

namespace ChatSystem.Models.LLM
{
    [Serializable]
    public class LLMConfiguration
    {
        public string model;
        public string token;
        public string serviceProvider;
        public string serviceUrl;
        public float temperature;
        public int maxTokens;
        public float topP;
        public float topK;
        public decimal inputTokenCost;
        public decimal outputTokenCost;
        public int timeoutSeconds;
        
        public LLMConfiguration()
        {
            temperature = 0.7f;
            maxTokens = 1000;
            topP = 1.0f;
            topK = 0f;
            timeoutSeconds = 30;
        }
    }
}
