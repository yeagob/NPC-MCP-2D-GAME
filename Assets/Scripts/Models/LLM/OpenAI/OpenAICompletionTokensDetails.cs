using System;

namespace ChatSystem.Models.LLM.OpenAI
{
    [Serializable]
    public struct OpenAICompletionTokensDetails
    {
        public int reasoning_tokens;
        public int audio_tokens;
        public int accepted_prediction_tokens;
        public int rejected_prediction_tokens;
    }
}
