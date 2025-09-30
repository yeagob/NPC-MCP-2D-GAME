using System;
using ChatSystem.Models.Context;
using ChatSystem.Services.Tools.Interfaces;
using ChatSystem.Enums;

namespace ChatSystem.Services.Tools
{
    public class ConversationToolDebugHandler : IToolDebugHandler
    {
        private readonly ConversationContext context;
        
        public ConversationToolDebugHandler(ConversationContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }
        
        public void OnToolExecuted(string toolName, string toolSetName, string arguments, string response)
        {
            string debugMessage = $"🔧 Tool Executed: {toolName} from {toolSetName}\n" +
                                 $"Parameters: {arguments}\n" +
                                 $"Result: {TruncateResponse(response)}";
            
            context.AddMessage(MessageRole.System, debugMessage);
        }
        
        public void OnToolError(string toolName, string toolSetName, string error)
        {
            string debugMessage = $"❌ Tool Error: {toolName} from {toolSetName}\n" +
                                 $"Error: {error}";
            
            context.AddMessage(MessageRole.System, debugMessage);
        }
        
        private string TruncateResponse(string response)
        {
            const int maxLength = 200;
            if (string.IsNullOrEmpty(response) || response.Length <= maxLength)
                return response;
                
            return response.Substring(0, maxLength) + "...";
        }
    }
}
