using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSystem.Characters;
using ChatSystem.Models.Tools;
using ChatSystem.Services.Logging;
using ChatSystem.Services.Tools.Interfaces;
using ChatSystem.Enums;
using InventorySystem.Components;
using InventorySystem.Enums;
using MapSystem;
using MapSystem.Enums;
using MapSystem.Elements;

namespace InventorySystem.Services.Tools
{
    public class InventoryToolSet : IToolSet
    {
        public string ToolSetId => "inventory-toolset";
        
        public ToolType ToolSetType => ToolType.Custom;
        
        private readonly CharacterAgent _characterAgent;
        private readonly InventoryComponent _inventoryComponent;
        private readonly MapSystem.MapSystem _mapSystem;
        
        public InventoryToolSet(CharacterAgent characterAgent, MapSystem.MapSystem mapSystem)
        {
            _characterAgent = characterAgent;
            _mapSystem = mapSystem;
            _inventoryComponent = _characterAgent.GetComponent<InventoryComponent>();
            
            if (_inventoryComponent == null)
            {
                LoggingService.LogError($"InventoryComponent not found on {_characterAgent.name}");
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
                    "pickup_item" => await ExecutePickupItemAsync(toolCall),
                    "drop_item" => await ExecuteDropItemAsync(toolCall),
                    "give_item" => await ExecuteGiveItemAsync(toolCall),
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

        private async Task<ToolResponse> ExecutePickupItemAsync(ToolCall toolCall)
        {
            await Task.Delay(10);
            
            try
            {
                if (_inventoryComponent == null)
                {
                    return CreateErrorResponse(toolCall.id, "Inventory component not available");
                }

                Dictionary<string, object> args = toolCall.arguments;
                string itemId = args["itemId"].ToString();
                 
                MapElement targetElement = _mapSystem.GetElementById(itemId);
                if (targetElement == null || targetElement.ElementType != MapElementType.Item)
                {
                    return CreateErrorResponse(toolCall.id, $"Item with id {itemId} not found");
                }

                ItemElement itemElement = targetElement as ItemElement;
                if (itemElement == null || !itemElement.IsCollectable)
                {
                    return CreateErrorResponse(toolCall.id, $"Item {itemId} is not collectable");
                }

                float distance = _mapSystem.GetDistanceBetweenElements(_characterAgent.GetCharacterElement(), targetElement);
                if (distance > itemElement.PickupRange)
                {
                    return CreateErrorResponse(toolCall.id, $"Item {itemId} is out of pickup range");
                }

                ItemType itemType = itemElement.ItemType;
                
                if (_inventoryComponent.HasItem(itemType))
                {
                    return CreateErrorResponse(toolCall.id, $"Already have {itemType} in inventory");
                }

                bool added = _inventoryComponent.AddItem(itemId, itemType, itemElement);
                
                if (added)
                {
                    _mapSystem.UnregisterElement(targetElement);
                    targetElement.gameObject.SetActive(false);
                    
                    UniversalLogUI.Instance.Log($"{_characterAgent.name} Coge  {itemType} (id: {itemId})");

                    return CreateSuccessResponse(toolCall.id, $"Successfully picked up {itemType} (id: {itemId})");
                }

                UniversalLogUI.Instance.Log($"{_characterAgent.name} Fallo al coger  {itemType} (id: {itemId})");
                
                return CreateErrorResponse(toolCall.id, $"Failed to add {itemType} to inventory");
            }
            catch (Exception ex)
            {
             
                UniversalLogUI.Instance.Log($"{_characterAgent.name} ERROR al coger ");
                return CreateErrorResponse(toolCall.id, $"Ha habido un problema con esta tool: {ex.Message}");
            }
        }
        
        private async Task<ToolResponse> ExecuteDropItemAsync(ToolCall toolCall)
        {
            await Task.Delay(10);
            
            try
            {
                if (_inventoryComponent == null)
                {
                    return CreateErrorResponse(toolCall.id, "Inventory component not available");
                }

                Dictionary<string, object> args = toolCall.arguments;
                string itemTypeString = args["itemType"].ToString();
                
                if (!Enum.TryParse(itemTypeString, out ItemType itemType))
                {
                    return CreateErrorResponse(toolCall.id, $"Invalid item type: {itemTypeString}");
                }

                if (!_inventoryComponent.HasItem(itemType))
                {
                    return CreateErrorResponse(toolCall.id, $"No {itemType} in inventory");
                }

                bool removed = _inventoryComponent.RemoveItem(itemType);
                
                if (removed)
                {
                    return CreateSuccessResponse(toolCall.id, $"Successfully dropped {itemType} from inventory");
                }

                return CreateErrorResponse(toolCall.id, $"Failed to remove {itemType} from inventory");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(toolCall.id, $"Ha habido un problema con esta tool: {ex.Message}");
            }
        }
        
        private async Task<ToolResponse> ExecuteGiveItemAsync(ToolCall toolCall)
        {
            await Task.Delay(10);
            
            try
            {
                if (_inventoryComponent == null)
                {
                    return CreateErrorResponse(toolCall.id, "Inventory component not available");
                }

                Dictionary<string, object> args = toolCall.arguments;
                string targetCharacterId = args["targetCharacterId"].ToString();
                string itemTypeString = args["itemType"].ToString();
                
                if (!Enum.TryParse(itemTypeString, out ItemType itemType))
                {
                    return CreateErrorResponse(toolCall.id, $"Invalid item type: {itemTypeString}");
                }

                MapElement targetElement = _mapSystem.GetElementById(targetCharacterId);
                if (targetElement == null || targetElement.ElementType != MapElementType.Character)
                {
                    return CreateErrorResponse(toolCall.id, $"Character with id {targetCharacterId} not found");
                }

                CharacterElement targetCharacter = targetElement as CharacterElement;
                InventoryComponent targetInventory = targetCharacter.GetComponent<InventoryComponent>();
                
                if (targetInventory == null)
                {
                    return CreateErrorResponse(toolCall.id, $"Target character {targetCharacterId} has no inventory");
                }

                float distance = _mapSystem.GetDistanceBetweenElements(_characterAgent.GetCharacterElement(), targetCharacter);
                if (distance > 1)
                {
                    return CreateErrorResponse(toolCall.id, $"Target character is out of range (distance: {distance:F1})");
                }

                if (!_inventoryComponent.HasItem(itemType))
                {
                    return CreateErrorResponse(toolCall.id, $"No {itemType} in inventory");
                }

                if (targetInventory.HasItem(itemType))
                {
                    return CreateErrorResponse(toolCall.id, $"Target character already has {itemType}");
                }

                Models.InventoryItem item = _inventoryComponent.GetItem(itemType);
                bool removed = _inventoryComponent.RemoveItem(itemType);
                bool added = targetInventory.AddItem(item.itemId, itemType, item.itemElement);
                
                if (removed && added)
                {
                    return CreateSuccessResponse(toolCall.id, $"Successfully gave {itemType} to {targetCharacter.name}");
                }

                if (removed && !added)
                {
                    _inventoryComponent.AddItem(item.itemId, itemType, item.itemElement);
                }

                return CreateErrorResponse(toolCall.id, $"Failed to transfer {itemType} to target character");
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
                "pickup_item" or "drop_item" or "give_item" => true,
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