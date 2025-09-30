using System;

namespace ChatSystem.Models.LLM.OpenAI
{
    [Serializable]
    public struct OpenAIFunction
    {
        public string name;
        public string arguments;
    }
}
