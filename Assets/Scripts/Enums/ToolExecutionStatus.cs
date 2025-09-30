using System;

namespace ChatSystem.Enums
{
    [Serializable]
    public enum ToolExecutionStatus
    {
        Pending,
        Executing,
        Success,
        Error,
        Timeout
    }
}
