using System;

namespace ChatSystem.Services.Tools.Interfaces
{
    public interface IToolDebugHandler
    {
        void OnToolExecuted(string toolName, string toolSetName, string arguments, string response);
        void OnToolError(string toolName, string toolSetName, string error);
    }
    
    public class ToolDebugContext
    {
        public bool debugEnabled { get; private set; }
        public IToolDebugHandler debugHandler { get; private set; }
        
        public ToolDebugContext(bool debugEnabled, IToolDebugHandler debugHandler = null)
        {
            this.debugEnabled = debugEnabled;
            this.debugHandler = debugHandler;
        }
        
        public static ToolDebugContext Disabled => new ToolDebugContext(false);
        
        public void LogToolExecution(string toolName, string toolSetName, string arguments, string response)
        {
            if (debugEnabled && debugHandler != null)
            {
                debugHandler.OnToolExecuted(toolName, toolSetName, arguments, response);
            }
        }
        
        public void LogToolError(string toolName, string toolSetName, string error)
        {
            if (debugEnabled && debugHandler != null)
            {
                debugHandler.OnToolError(toolName, toolSetName, error);
            }
        }
    }
}
