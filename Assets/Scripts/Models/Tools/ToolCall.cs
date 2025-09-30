using System;
using System.Collections.Generic;

namespace ChatSystem.Models.Tools
{
    [Serializable]
    public class ToolCall
    {
        public string id;
        public string name;
        public Dictionary<string, object> arguments;
        public DateTime callTimestamp;
        
        public ToolCall()
        {
            id = Guid.NewGuid().ToString();
            arguments = new Dictionary<string, object>();
            callTimestamp = DateTime.UtcNow;
        }
        
        public ToolCall(string toolName, Dictionary<string, object> parameters) : this()
        {
            name = toolName;
            arguments = parameters ?? new Dictionary<string, object>();
        }
    }
}
