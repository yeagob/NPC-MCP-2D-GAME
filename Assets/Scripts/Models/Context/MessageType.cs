using System;

namespace ChatSystem.Models.Context
{
    [Serializable]
    public enum MessageType
    {
        Text,
        ToolCall,
        ToolResponse,
        SystemPrompt
    }
}
