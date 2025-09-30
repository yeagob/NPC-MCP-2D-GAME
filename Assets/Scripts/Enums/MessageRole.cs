using System;

namespace ChatSystem.Enums
{
    [Serializable]
    public enum MessageRole
    {
        User,
        Assistant,
        Tool,
        System
    }
}
