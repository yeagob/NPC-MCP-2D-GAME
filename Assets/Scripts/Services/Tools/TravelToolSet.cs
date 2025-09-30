using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSystem.Models.Tools;
using ChatSystem.Enums;
using ChatSystem.Services.Tools.Interfaces;
using ChatSystem.Services.Logging;

namespace ChatSystem.Services.Tools
{
    public class TravelToolSet : IToolSet
    {
        public string ToolSetId => "travel-search-toolset";
        public ToolType ToolSetType => ToolType.TravelSearch;
        
        private readonly Dictionary<string, object> travelData;
        
        public TravelToolSet()
        {
            travelData = new Dictionary<string, object>();
            InitializeMockTravelData();
        }
        
        public List<ToolConfiguration> GetAvailableTools()
        {
            return new List<ToolConfiguration>
            {
                CreateSearchTravelsByCountryTool(),
                CreateSearchTravelsAdvancedTool(),
                CreateGetTravelDetailsTool()
            };
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
                    "search_travels_by_country" => await ExecuteSearchByCountryAsync(toolCall),
                    "search_travels_advanced" => await ExecuteAdvancedSearchAsync(toolCall),
                    "get_travel_details" => await ExecuteGetTravelDetailsAsync(toolCall),
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
                "search_travels_by_country" or "search_travels_advanced" or "get_travel_details" => true,
                _ => false
            };
        }
        
        private async Task<ToolResponse> ExecuteSearchByCountryAsync(ToolCall toolCall)
        {
            await Task.Delay(200);
            
            try
            {
                Dictionary<string, object> args = new Dictionary<string, object>( toolCall.arguments);
                string country = args["country"].ToString();
                
                List<Dictionary<string, object>> mockResults = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object> { ["id"] = "travel_001", ["name"] = $"Adventure in {country}", ["duration"] = "7 days", ["price"] = "$1200" },
                    new Dictionary<string, object> { ["id"] = "travel_002", ["name"] = $"Cultural Tour {country}", ["duration"] = "5 days", ["price"] = "$800" },
                    new Dictionary<string, object> { ["id"] = "travel_003", ["name"] = $"Luxury Experience {country}", ["duration"] = "10 days", ["price"] = "$2500" }
                };
                
                string result = SerializeToJson(mockResults);
                return CreateSuccessResponse(toolCall.id, result);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(toolCall.id, $"Error searching travels by country: {ex.Message}");
            }
        }
        
        private async Task<ToolResponse> ExecuteAdvancedSearchAsync(ToolCall toolCall)
        {
            await Task.Delay(300);
            
            try
            {
                Dictionary<string, object> args = new Dictionary<string, object>(toolCall.arguments);
                
                int days = args.ContainsKey("days") ? Convert.ToInt32(args["days"]) : 7;
                int budget = args.ContainsKey("budget") ? Convert.ToInt32(args["budget"]) : 1000;
                string interests = args.ContainsKey("interests") ? args["interests"].ToString() : "general";
                
                List<Dictionary<string, object>> mockResults = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["id"] = "travel_004",
                        ["name"] = "Custom Adventure",
                        ["duration"] = $"{days} days",
                        ["price"] = $"${budget}",
                        ["interests"] = interests
                    },
                    new Dictionary<string, object>
                    {
                        ["id"] = "travel_005",
                        ["name"] = "Tailored Experience",
                        ["duration"] = $"{days + 2} days",
                        ["price"] = $"${budget + 300}",
                        ["interests"] = interests == "general" ? "adventure" : interests
                    }
                };
                
                string result = SerializeToJson(mockResults);
                return CreateSuccessResponse(toolCall.id, result);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(toolCall.id, $"Error in advanced search: {ex.Message}");
            }
        }
        
        private async Task<ToolResponse> ExecuteGetTravelDetailsAsync(ToolCall toolCall)
        {
            await Task.Delay(150);
            
            try
            {
                Dictionary<string, object> args = new Dictionary<string, object>(toolCall.arguments);
                string travelId = args["travelId"].ToString();
                
                Dictionary<string, object> mockDetails = new Dictionary<string, object>
                {
                    ["id"] = travelId,
                    ["name"] = "Amazing Travel Experience",
                    ["description"] = "A comprehensive travel package with amazing destinations",
                    ["duration"] = "7 days",
                    ["price"] = "$1500",
                    ["destinations"] = new[] { "City A", "City B", "City C" },
                    ["activities"] = new[] { "Sightseeing", "Adventure sports", "Cultural visits" },
                    ["includes"] = new[] { "Accommodation", "Meals", "Transportation", "Guide" },
                    ["difficulty"] = "Moderate",
                    ["group_size"] = "8-12 people"
                };
                
                string result = SerializeToJson(mockDetails);
                return CreateSuccessResponse(toolCall.id, result);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(toolCall.id, $"Error getting travel details: {ex.Message}");
            }
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
        
        private void InitializeMockTravelData()
        {
            travelData["countries"] = new[] { "Spain", "France", "Italy", "Japan", "Thailand", "Peru" };
            travelData["interests"] = new[] { "adventure", "culture", "luxury", "nature", "history", "food" };
        }
        
        private ToolConfiguration CreateSearchTravelsByCountryTool()
        {
            return new ToolConfiguration
            {
                toolId = "search_travels_by_country",
                toolName = "search_travels_by_country",
                toolType = ToolType.TravelSearch,
                _inputSchema = new ToolSchema
                {
                    type = "object",
                    properties = new Dictionary<string, ParameterSchema>
                    {
                        ["country"] = new ParameterSchema { type = "string", description = "Destination country" }
                    },
                    required = new List<string> { "country" }
                },
                annotations = new ToolAnnotations
                {
                    title = "Search Travels by Country",
                    readOnlyHint = true,
                    destructiveHint = false,
                    idempotentHint = true,
                    openWorldHint = false
                }
            };
        }
        
        private ToolConfiguration CreateSearchTravelsAdvancedTool()
        {
            return new ToolConfiguration
            {
                toolId = "search_travels_advanced",
                toolName = "search_travels_advanced",
                toolType = ToolType.TravelSearch,
                _inputSchema = new ToolSchema
                {
                    type = "object",
                    properties = new Dictionary<string, ParameterSchema>
                    {
                        ["days"] = new ParameterSchema { type = "integer", description = "Number of days (optional)" },
                        ["budget"] = new ParameterSchema { type = "integer", description = "Budget in USD (optional)" },
                        ["interests"] = new ParameterSchema { type = "string", description = "Travel interests (optional)" }
                    },
                    required = new List<string>()
                },
                annotations = new ToolAnnotations
                {
                    title = "Advanced Travel Search",
                    readOnlyHint = true,
                    destructiveHint = false,
                    idempotentHint = true,
                    openWorldHint = true
                }
            };
        }
        
        private ToolConfiguration CreateGetTravelDetailsTool()
        {
            return new ToolConfiguration
            {
                toolId = "get_travel_details",
                toolName = "get_travel_details",
                toolType = ToolType.TravelDetails,
                _inputSchema = new ToolSchema
                {
                    type = "object",
                    properties = new Dictionary<string, ParameterSchema>
                    {
                        ["travelId"] = new ParameterSchema { type = "string", description = "Travel identifier" }
                    },
                    required = new List<string> { "travelId" }
                },
                annotations = new ToolAnnotations
                {
                    title = "Get Travel Details",
                    readOnlyHint = true,
                    destructiveHint = false,
                    idempotentHint = true,
                    openWorldHint = false
                }
            };
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
        
        private string SerializeToJson(object obj)
        {
            if (obj is List<Dictionary<string, object>> list)
            {
                List<string> items = new List<string>();
                foreach (Dictionary<string, object> item in list)
                {
                    items.Add(SerializeDictionary(item));
                }
                return $"[{string.Join(",", items)}]";
            }
            
            if (obj is Dictionary<string, object> dict)
            {
                return SerializeDictionary(dict);
            }
            
            return "{}";
        }
        
        private string SerializeDictionary(Dictionary<string, object> dict)
        {
            List<string> pairs = new List<string>();
            
            foreach (KeyValuePair<string, object> kvp in dict)
            {
                string value = SerializeValue(kvp.Value);
                pairs.Add($"\"{kvp.Key}\":{value}");
            }
            
            return $"{{{string.Join(",", pairs)}}}";
        }
        
        private string SerializeValue(object value)
        {
            if (value is string)
                return $"\"{value}\"";
            if (value is string[] array)
                return $"[{string.Join(",", Array.ConvertAll(array, s => $"\"{s}\""))}]";
            if (value is int || value is double || value is float)
                return value.ToString();
            if (value is bool)
                return value.ToString().ToLower();
                
            return $"\"{value}\"";
        }
    }
}
