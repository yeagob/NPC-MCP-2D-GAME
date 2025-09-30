using System;

namespace ChatSystem.Models.LLM.OpenAI
{
    [Serializable]
    public struct OpenAIPromptTokensDetails
    {
        public int cached_tokens;
        public int audio_tokens;
    }
}
