using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSystem.Characters;
using ChatSystem.Models.Tools;
using ChatSystem.Services.Logging;
using ChatSystem.Services.Tools.Interfaces;
using Grid;
using MapSystem.Enums;

namespace ChatSystem.Services.Tools
{
    public class CharacterToolSet : IToolSet
    {
        public string ToolSetId => "character-toolset";
        
        public ToolType ToolSetType => ToolType.Custom;
        
        private readonly CharacterAgent _characterAgent;
        
        public CharacterToolSet(CharacterAgent characterAgent)
        {
            _characterAgent = characterAgent;
        }

        public async Task<ToolResponse> ExecuteToolAsync(ToolCall toolCall)
        {
            return await ExecuteToolAsync(toolCall, ToolDebugContext.Disabled);
        }
        
        public async Task<ToolResponse> ExecuteToolAsync(ToolCall toolCall, ToolDebugContext debugContext)
        {
            LoggingService.LogToolCall(toolCall.name, toolCall.arguments);
            
            try
            {
                ToolResponse response = toolCall.name switch
                {
                    "move" => await ExecuteAgentMoveAsync(toolCall),
                    "talk" => await ExecuteAgentTalkAsync(toolCall),
                    "flip" => await ExecuteAgentFlipAsync(toolCall),
                    _ => CreateErrorResponse(toolCall.id, $"Unknown tool: {toolCall.name}")
                };
                
                LoggingService.LogToolResponse(toolCall.name, response.content);
                
                if (response.success)
                {
                    debugContext.LogToolExecution(
                        toolCall.name, 
                        ToolSetId, 
                        SerializeArguments(toolCall.arguments), 
                        response.content
                    );
                }
                else
                {
                    debugContext.LogToolError(toolCall.name, ToolSetId, response.content);
                }
                
                return response;
            }
            catch (Exception ex)
            {
                debugContext.LogToolError(toolCall.name, ToolSetId, ex.Message);
                return CreateErrorResponse(toolCall.id, $"Tool execution failed: {ex.Message}");
            }
        }

        private async Task<ToolResponse> ExecuteAgentMoveAsync(ToolCall toolCall)
        {
            await Task.Delay(10);
            
            try
            {
                Dictionary<string, object> args = toolCall.arguments;
                int row = (int)args["row"];
                int col = (int)args["col"];

                int index = GridSystem.CoordToIndex(row, col);
                
                if (_characterAgent.Teleport(row, col))
                {
                    UniversalLogUI.Instance.Log($"{_characterAgent.name} Move to {index}");
                    return CreateSuccessResponse(toolCall.id, $"Tu posición ahora es: {row},{col}");
                }
                else
                {
                    UniversalLogUI.Instance.Log($"{_characterAgent.name} ERROR Move to {index}");

                    return CreateErrorResponse(toolCall.id, $"No puedes moverte a: {row}, {col}");
                }
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(toolCall.id, $"Ha habido un problema con esta tool: {ex.Message}");
            }
        }
        
        private async Task<ToolResponse> ExecuteAgentTalkAsync(ToolCall toolCall)
        {
            try
            {
                Dictionary<string, object> args = toolCall.arguments;
                string message = args["message"].ToString();

                _characterAgent.Talk(message);
                
                await Task.Delay(3000);
                
                UniversalLogUI.Instance.Log($"{_characterAgent.name} Talk");

                return CreateSuccessResponse(toolCall.id, $"Has dicho: {message}");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(toolCall.id, $"Ha habido un problema con esta tool: {ex.Message}");
            }
        }
        
        private async Task<ToolResponse> ExecuteAgentFlipAsync(ToolCall toolCall)
        {
            await Task.Delay(10);
            
            try
            {
                Dictionary<string, object> args = toolCall.arguments;

                ViewDirection direction = _characterAgent.Flip();
                
                UniversalLogUI.Instance.Log($"{_characterAgent.name} Flip");

                return CreateSuccessResponse(toolCall.id, $"Ahora estás mirando hacia " + direction);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(toolCall.id, $"Ha habido un problema con esta tool: {ex.Message}");
            }
        }

        public async Task<bool> ValidateToolCallAsync(ToolCall toolCall)
        {
            await Task.CompletedTask;
            
            if (!IsToolSupported(toolCall.name))
                return false;
                
            return toolCall.arguments != null;
        }
        
        public bool IsToolSupported(string toolName)
        {
            return toolName switch
            {
                "attack" or "move" or "talk" or "flip" => true,
                _ => false
            };
        }
        
        private string SerializeArguments(Dictionary<string, object> arguments)
        {
            if (arguments == null || arguments.Count == 0)
                return "{}";
                
            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, object> kvp in arguments)
            {
                parts.Add($"{kvp.Key}:{kvp.Value}");
            }
            return "{" + string.Join(", ", parts) + "}";
        }
        
        private ToolResponse CreateSuccessResponse(string toolCallId, string content)
        {
            return new ToolResponse
            {
                toolCallId = toolCallId,
                content = content,
                success = true,
                responseTimestamp = DateTime.UtcNow
            };
        }
        
        private ToolResponse CreateErrorResponse(string toolCallId, string error)
        {
            return new ToolResponse
            {
                toolCallId = toolCallId,
                content = error,
                success = false,
                responseTimestamp = DateTime.UtcNow
            };
        }
    }
}