using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapSystem.Elements;
using CombatSystem.Components;
using CombatSystem.Models;
using LLMSystem.Models.Tools;
using LLMSystem.Services.Tools.Interfaces;
using LLMSystem.Enums;
using Agents;
using Logging;

namespace CombatSystem.Tools
{
    public class CombatToolSet : IToolSet
    {
        private readonly CharacterAgent _characterAgent;
        private readonly CombatComponent _combatComponent;
        private readonly MapSystem.MapSystem _mapSystem;
        
        public string ToolSetId => "combat-toolset";
        public ToolType ToolSetType => ToolType.Custom;
        
        public CombatToolSet(CharacterAgent characterAgent, MapSystem.MapSystem mapSystem)
        {
            _characterAgent = characterAgent ?? throw new ArgumentNullException(nameof(characterAgent));
            _mapSystem = mapSystem ?? throw new ArgumentNullException(nameof(mapSystem));
            _combatComponent = characterAgent.GetComponent<CombatComponent>();
            
            if (_combatComponent == null)
            {
                throw new InvalidOperationException("CharacterAgent must have CombatComponent");
            }
        }
        
        public List<ToolConfiguration> GetAvailableTools()
        {
            List<ToolConfiguration> tools = new List<ToolConfiguration>
            {
                CreateAttackToolConfiguration()
            };
            
            return tools;
        }
        
        private ToolConfiguration CreateAttackToolConfiguration()
        {
            ToolConfiguration config = new ToolConfiguration
            {
                toolId = "attack",
                toolName = "attack",
                description = "Attack a nearby character. Target must be adjacent (1 cell or less) and you must be facing them.",
                enabled = true
            };
            
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                { "type", "object" },
                { 
                    "properties", new Dictionary<string, object>
                    {
                        {
                            "targetCharacterId", new Dictionary<string, object>
                            {
                                { "type", "string" },
                                { "description", "The ID of the character to attack" }
                            }
                        }
                    }
                },
                { "required", new List<string> { "targetCharacterId" } }
            };
            
            config.parameters = parameters;
            
            return config;
        }
        
        public async Task<ToolResponse> ExecuteToolAsync(ToolCall toolCall, ToolDebugContext debugContext)
        {
            LoggingService.ToolCall(toolCall.name, ParseArguments(toolCall.arguments));
            
            ToolResponse response = toolCall.name switch
            {
                "attack" => await ExecuteAttackAsync(toolCall),
                _ => ToolResponse.Failure($"Unknown tool: {toolCall.name}")
            };
            
            LoggingService.ToolResponse(toolCall.name, response);
            
            return response;
        }
        
        private Dictionary<string, object> ParseArguments(string arguments)
        {
            Dictionary<string, object> parsed = new Dictionary<string, object>();
            
            try
            {
                parsed = SimpleJsonParser.Parse(arguments);
            }
            catch
            {
                parsed["raw"] = arguments;
            }
            
            return parsed;
        }
        
        private async Task<ToolResponse> ExecuteAttackAsync(ToolCall toolCall)
        {
            Dictionary<string, object> args = ParseArguments(toolCall.arguments);
            
            if (!args.ContainsKey("targetCharacterId"))
            {
                return ToolResponse.Failure("Missing targetCharacterId parameter");
            }
            
            string targetId = args["targetCharacterId"].ToString();
            
            MapElement targetElement = _mapSystem.GetElementById(targetId);
            
            if (targetElement == null)
            {
                return ToolResponse.Failure($"Target character not found: {targetId}");
            }
            
            CharacterElement targetCharacter = targetElement as CharacterElement;
            
            if (targetCharacter == null)
            {
                return ToolResponse.Failure($"Target is not a character: {targetId}");
            }
            
            AttackResult result = _combatComponent.Attack(targetCharacter);
            
            if (!result.success)
            {
                return ToolResponse.Failure(result.errorMessage);
            }
            
            string message = BuildAttackSuccessMessage(targetCharacter, result);
            
            return await Task.FromResult(ToolResponse.Success(message));
        }
        
        private string BuildAttackSuccessMessage(CharacterElement target, AttackResult result)
        {
            string message = $"Successfully attacked {target.name} for {result.damageDealt} damage.";
            
            if (result.targetDied)
            {
                message += $" {target.name} has been defeated.";
            }
            else
            {
                message += $" {target.name} has {target.HealthPoints}/{target.MaxHealthPoints} HP remaining.";
            }
            
            return message;
        }
    }
}