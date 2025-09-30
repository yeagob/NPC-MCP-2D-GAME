using System;
using System.Collections.Generic;

namespace ChatSystem.Models.LLM.OpenAI
{
    [Serializable]
    public struct OpenAIMessage
    {
        public string role;
        public string content;
        public List<OpenAIToolCall> tool_calls;
        public string refusal;
        public List<object> annotations;
    }
}
