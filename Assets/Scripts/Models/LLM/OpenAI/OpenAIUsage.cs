using System;

namespace ChatSystem.Models.LLM.OpenAI
{
    [Serializable]
    public struct OpenAIUsage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
        public OpenAIPromptTokensDetails prompt_tokens_details;
        public OpenAICompletionTokensDetails completion_tokens_details;
    }
}
