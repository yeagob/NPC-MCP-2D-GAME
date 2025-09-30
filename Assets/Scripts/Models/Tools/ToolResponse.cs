using System;

namespace ChatSystem.Models.Tools
{
    [Serializable]
    public class ToolResponse
    {
        public string toolCallId;
        public string toolName;
        public string content;
        public bool success;
        public string errorMessage;
        public DateTime responseTimestamp;
        
        public ToolResponse()
        {
            responseTimestamp = DateTime.UtcNow;
            success = true;
        }
        
        public ToolResponse(string callId, string name, string result) : this()
        {
            toolCallId = callId;
            toolName = name;
            content = result;
        }
        
        public static ToolResponse CreateError(string callId, string name, string error)
        {
            return new ToolResponse
            {
                toolCallId = callId,
                toolName = name,
                success = false,
                errorMessage = error,
                responseTimestamp = DateTime.UtcNow
            };
        }
    }
}
