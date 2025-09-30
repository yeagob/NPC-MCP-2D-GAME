using System;

namespace ChatSystem.Models.LLM.OpenAI
{
    [Serializable]
    public struct OpenAIToolCall
    {
        public string id;
        public string type;
        public OpenAIFunction function;
    }
}
