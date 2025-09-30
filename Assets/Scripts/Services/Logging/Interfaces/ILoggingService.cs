using ChatSystem.Enums;

namespace ChatSystem.Services.Logging.Interfaces
{
    public interface ILoggingService
    {
        void LogDebug(string message);
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogCritical(string message);
        void LogAgentExecution(string agentId, string message);
        void LogToolCall(string toolName, string parameters);
        void LogToolResponse(string toolName, string response);
        void LogMessageReceived(string conversationId, string role);
        void LogPromptConstruction(string agentId, int tokenCount);
        void SetLogLevel(LogLevel level);
        bool IsLogLevelEnabled(LogLevel level);
    }
}
