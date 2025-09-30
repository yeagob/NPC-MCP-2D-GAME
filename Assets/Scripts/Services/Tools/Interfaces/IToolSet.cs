using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSystem.Models.Tools;
using ChatSystem.Enums;
using ChatSystem.Services.Tools.Interfaces;

namespace ChatSystem.Services.Tools.Interfaces
{
    public interface IToolSet
    {
        string ToolSetId { get; }
        ToolType ToolSetType { get; }
        Task<ToolResponse> ExecuteToolAsync(ToolCall toolCall);
        Task<ToolResponse> ExecuteToolAsync(ToolCall toolCall, ToolDebugContext debugContext);
        Task<bool> ValidateToolCallAsync(ToolCall toolCall);
        bool IsToolSupported(string toolName);
    }
}
