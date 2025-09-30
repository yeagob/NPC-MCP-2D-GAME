using System;

namespace ChatSystem.Models.LLM.OpenAI
{
    [Serializable]
    public struct OpenAIChoice
    {
        public int index;
        public OpenAIMessage message;
        public string finish_reason;
    }
}
