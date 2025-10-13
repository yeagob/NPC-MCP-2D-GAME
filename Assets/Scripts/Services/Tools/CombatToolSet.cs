using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSystem.Characters;
using ChatSystem.Models.Tools;
using ChatSystem.Services.Logging;
using ChatSystem.Services.Tools.Interfaces;
using ChatSystem.Enums;
using CombatSystem.Components;
using CombatSystem.Models;
using MapSystem;
using MapSystem.Enums;
using MapSystem.Elements;

namespace CombatSystem.Services.Tools
{
    public class CombatToolSet : IToolSet
    {
        public string ToolSetId => "combat-toolset";
        
        public ToolType ToolSetType => ToolType.Custom;
        
        private readonly CharacterAgent _characterAgent;
        private readonly CombatComponent _combatComponent;
        private readonly MapSystem.MapSystem _mapSystem;
        
        public CombatToolSet(CharacterAgent characterAgent, MapSystem.MapSystem mapSystem)
        {
            _characterAgent = characterAgent;
            _mapSystem = mapSystem;
            _combatComponent = _characterAgent.GetComponent<CombatComponent>();
            
            if (_combatComponent == null)
            {
                LoggingService.LogError($"CombatComponent not found on {_characterAgent.name}");
            }
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
                    "attack" => await ExecuteAttackAsync(toolCall),
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

        private async Task<ToolResponse> ExecuteAttackAsync(ToolCall toolCall)
        {
            await Task.Delay(10);
            
            try
            {
                if (_combatComponent == null)
                {
                    return CreateErrorResponse(toolCall.id, "Combat component not available");
                }

                Dictionary<string, object> args = toolCall.arguments;
                string targetCharacterId = args["targetCharacterId"].ToString();
                 
                MapElement targetElement = _mapSystem.GetElementById(targetCharacterId);
                if (targetElement == null || targetElement.ElementType != MapElementType.Character)
                {
                    return CreateErrorResponse(toolCall.id, $"Character with id {targetCharacterId} not found");
                }

                CharacterElement targetCharacter = targetElement as CharacterElement;
                
                if (targetCharacter == null)
                {
                    return CreateErrorResponse(toolCall.id, $"Target {targetCharacterId} is not a character");
                }

                AttackResult result = _combatComponent.Attack(targetCharacter);
                
                if (!result.success)
                {
                    UniversalLogUI.Instance.Log($"{_characterAgent.name} failed to attack {targetCharacter.name}: {result.errorMessage}");
                    return CreateErrorResponse(toolCall.id, result.errorMessage);
                }

                string message = $"Successfully attacked {targetCharacter.name} for {result.damageDealt} damage";
                
                if (result.targetDied)
                {
                    message += $". {targetCharacter.name} has been defeated";
                    UniversalLogUI.Instance.Log($"{_characterAgent.name} defeated {targetCharacter.name}!");
                }
                else
                {
                    message += $". {targetCharacter.name} has {targetCharacter.HealthPoints}/{targetCharacter.MaxHealthPoints} HP remaining";
                    UniversalLogUI.Instance.Log($"{_characterAgent.name} attacked {targetCharacter.name} for {result.damageDealt} damage");
                }

                return CreateSuccessResponse(toolCall.id, message);
            }
            catch (Exception ex)
            {
                UniversalLogUI.Instance.Log($"{_characterAgent.name} ERROR during attack");
                return CreateErrorResponse(toolCall.id, $"Attack execution failed: {ex.Message}");
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
                "attack" => true,
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