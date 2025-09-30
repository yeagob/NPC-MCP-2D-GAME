using UnityEngine;
using System.Collections.Generic;
using ChatSystem.Configuration.ScriptableObjects;
using ChatSystem.Configuration.Helpers;
using ChatSystem.Models.Tools;
using ChatSystem.Models.Tools.MCP;
using ChatSystem.Enums;

namespace ChatSystem.Configuration.ToolDefinitions
{
    public static class TravelToolDefinitions
    {
        private static readonly List<string> TravelInterests = new List<string>
        {
            "Aventura", "Cruceros", "Cultura", "Familia", "Invierno", "Lujo",
            "Luna de miel", "Multi-País", "Naturaleza", "Navidad", "Otoño",
            "Parejas", "Playa", "Primavera", "Safari", "Semana Santa",
            "Senior", "VIAJE EN GRUPO", "Verano", "Viaje individual"
        };
        
        public static ToolConfig CreateSearchDestinationsTool()
        {
            ToolConfig config = ScriptableObject.CreateInstance<ToolConfig>();
            
            config.toolId = "search_destinations";
            config.toolName = "Search Travel Destinations";
            config.toolType = ToolType.TravelSearch;
            config.enabled = true;
            config.timeoutMs = 5000;
            config.maxRetries = 3;
            
            Dictionary<string, PropertyDefinition> properties = new Dictionary<string, PropertyDefinition>
            {
                ["countries"] = ToolConfigBuilder.CreateArrayProperty(
                    "string", "Countries to search (Spanish names)"),
                
                ["continents"] = ToolConfigBuilder.CreateArrayProperty(
                    "string", "Continents to search (Spanish names)"),
                
                ["interests"] = new PropertyDefinition
                {
                    type = "array",
                    description = "Travel interests or themes",
                    items = new ItemDefinition
                    {
                        type = "string",
                        description = string.Join(", ", TravelInterests)
                    }
                }
            };
            
            config.function = ToolConfigBuilder.CreateFunction(
                "search_destinations",
                "Search travel destinations by multiple criteria like countries, interests",
                properties,
                new List<string>()
            );
            
            config.annotations = new ToolAnnotations
            {
                title = "Travel Destination Search",
                readOnlyHint = true,
                idempotentHint = true
            };
            
            return config;
        }
        
        public static ToolConfig CreateGetDestinationDetailsTool()
        {
            ToolConfig config = ScriptableObject.CreateInstance<ToolConfig>();
            
            config.toolId = "get_destination_details";
            config.toolName = "Get Destination Details";
            config.toolType = ToolType.TravelDetails;
            config.enabled = true;
            config.timeoutMs = 3000;
            config.maxRetries = 3;
            
            Dictionary<string, PropertyDefinition> properties = new Dictionary<string, PropertyDefinition>
            {
                ["travel_id"] = ToolConfigBuilder.CreateNumberProperty(
                    "Travel ID from the destinations catalog")
            };
            
            config.function = ToolConfigBuilder.CreateFunction(
                "get_destination_details",
                "Get complete details of a specific travel destination including link, itinerary, inclusions, and pictures",
                properties,
                new List<string> { "travel_id" }
            );
            
            config.annotations = new ToolAnnotations
            {
                title = "Travel Details Retrieval",
                readOnlyHint = true,
                idempotentHint = true
            };
            
            return config;
        }
        
        public static ToolConfig CreateGetDestinationsByCountryTool()
        {
            ToolConfig config = ScriptableObject.CreateInstance<ToolConfig>();
            
            config.toolId = "get_destinations_by_country";
            config.toolName = "Get Destinations by Country";
            config.toolType = ToolType.TravelSearch;
            config.enabled = true;
            config.timeoutMs = 5000;
            config.maxRetries = 3;
            
            Dictionary<string, PropertyDefinition> properties = new Dictionary<string, PropertyDefinition>
            {
                ["country_name"] = ToolConfigBuilder.CreateStringProperty(
                    "Country name in Spanish")
            };
            
            config.function = ToolConfigBuilder.CreateFunction(
                "get_destinations_by_country",
                "Get all destinations available for a specific country",
                properties,
                new List<string> { "country_name" }
            );
            
            config.annotations = new ToolAnnotations
            {
                title = "Country Destinations Search",
                readOnlyHint = true,
                idempotentHint = true
            };
            
            return config;
        }
    }
}