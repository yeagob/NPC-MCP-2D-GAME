using System;

namespace ChatSystem.Configuration
{
    [Serializable]
    public static class ApplicationConfig
    {
        public static class Logging
        {
            public const string DEBUG_PREFIX = "[DEBUG]";
            public const string INFO_PREFIX = "[INFO]";
            public const string WARNING_PREFIX = "[WARNING]";
            public const string ERROR_PREFIX = "[ERROR]";
            public const string CRITICAL_PREFIX = "[CRITICAL]";
            public const string AGENT_PREFIX = "[AGENT:{0}]";
            public const string TOOL_CALL_PREFIX = "[TOOL_CALL:{0}]";
            public const string TOOL_RESPONSE_PREFIX = "[TOOL_RESPONSE:{0}]";
            public const string MESSAGE_PREFIX = "[MESSAGE:{0}]";
            public const string PROMPT_PREFIX = "[PROMPT:{0}]";
            public const string TIMESTAMP_FORMAT = "HH:mm:ss.fff";
        }
        
        public static class ToolSets
        {
            public const string USER_TOOLSET_ID = "user-management-toolset";
            public const string TRAVEL_TOOLSET_ID = "travel-search-toolset";
            
            public static class UserTools
            {
                public const string UPDATE_USER_TAG = "update_user_tag";
                public const string UPDATE_USER_NAME = "update_user_name";
                public const string ADD_USER_COMMENT = "add_user_comment";
            }
            
            public static class TravelTools
            {
                public const string SEARCH_BY_COUNTRY = "search_travels_by_country";
                public const string SEARCH_ADVANCED = "search_travels_advanced";
                public const string GET_TRAVEL_DETAILS = "get_travel_details";
            }
        }
        
        public static class LLM
        {
            public const float DEFAULT_TEMPERATURE = 0.7f;
            public const int DEFAULT_MAX_TOKENS = 300;
            public const int MOCK_DELAY_MS = 1000;
            public const int MOCK_PROMPT_TOKENS = 150;
            public const int MOCK_COMPLETION_TOKENS = 80;
            public const int MOCK_TOTAL_TOKENS = 230;
        }
        
        public static class Tools
        {
            public const int USER_TOOL_DELAY_MS = 100;
            public const int TRAVEL_SEARCH_DELAY_MS = 200;
            public const int TRAVEL_ADVANCED_DELAY_MS = 300;
            public const int TRAVEL_DETAILS_DELAY_MS = 150;
        }
        
        public static class Storage
        {
            public const int ESTIMATED_CONVERSATION_SIZE = 1024;
            public const int ESTIMATED_AGENT_RESPONSE_SIZE = 512;
        }
        
        public static class MockData
        {
            public static readonly string[] COUNTRIES = { "Spain", "France", "Italy", "Japan", "Thailand", "Peru" };
            public static readonly string[] INTERESTS = { "adventure", "culture", "luxury", "nature", "history", "food" };
            public static readonly string[] TRAVEL_ACTIVITIES = { "Sightseeing", "Adventure sports", "Cultural visits" };
            public static readonly string[] TRAVEL_INCLUDES = { "Accommodation", "Meals", "Transportation", "Guide" };
            public static readonly string[] DESTINATIONS = { "City A", "City B", "City C" };
        }
        
        public static class Messages
        {
            public const string LOGGING_INITIALIZED = "LoggingService initialized";
            public const string CONTEXT_MANAGER_INITIALIZED = "ContextManager initialized";
            public const string USER_TOOLSET_INITIALIZED = "UserToolSet initialized";
            public const string TRAVEL_TOOLSET_INITIALIZED = "TravelToolSet initialized";
            public const string AGENT_EXECUTOR_INITIALIZED = "AgentExecutor initialized";
            public const string PERSISTENCE_INITIALIZED = "PersistenceService initialized (in-memory mode)";
            
            public const string USER_TAG_UPDATED = "User tag updated successfully for user {0}";
            public const string USER_NAME_UPDATED = "User name updated successfully for user {0}";
            public const string COMMENT_ADDED = "Comment added successfully for user {0} on travel {1}";
            
            public const string AGENT_EXECUTION_STARTED = "Starting agent execution";
            public const string AGENT_EXECUTION_COMPLETED = "Agent execution completed successfully";
            public const string AGENT_EXECUTION_FAILED = "Agent execution failed for {0}: {1}";
            
            public const string TOOLSET_REGISTERED = "ToolSet registered: {0}";
            public const string TOOLSET_UNREGISTERED = "ToolSet unregistered: {0}";
            public const string TOOLSET_NOT_FOUND = "ToolSet not found: {0}";
            
            public const string CONVERSATION_CREATED = "Creating new conversation: {0}";
            public const string CONVERSATION_CLEARED = "Clearing context for conversation: {0}";
            public const string CONVERSATION_NOT_FOUND = "Conversation not found: {0}";
            
            public const string BACKUP_REQUESTED = "Backup requested to: {0}";
            public const string RESTORE_REQUESTED = "Restore requested from: {0}";
            public const string BACKUP_NOT_IMPLEMENTED = "Backup functionality not implemented in in-memory mode";
            public const string RESTORE_NOT_IMPLEMENTED = "Restore functionality not implemented in in-memory mode";
        }
        
        public static class Errors
        {
            public const string UNKNOWN_TOOL = "Unknown tool: {0}";
            public const string TOOL_NOT_FOUND = "No ToolSet found for tool: {0}";
            public const string TOOL_EXECUTION_FAILED = "Tool execution failed: Tool {0} not found";
            public const string AGENT_EXECUTION_FAILED = "Agent execution failed: {0}";
            public const string ERROR_UPDATING_USER_TAG = "Error updating user tag: {0}";
            public const string ERROR_UPDATING_USER_NAME = "Error updating user name: {0}";
            public const string ERROR_ADDING_COMMENT = "Error adding comment: {0}";
            public const string ERROR_SEARCHING_BY_COUNTRY = "Error searching travels by country: {0}";
            public const string ERROR_ADVANCED_SEARCH = "Error in advanced search: {0}";
            public const string ERROR_GETTING_TRAVEL_DETAILS = "Error getting travel details: {0}";
        }
        
        public static class Responses
        {
            public const string USER_MANAGEMENT_RESPONSE = "I'll help you with user management. Let me update your information.";
            public const string TRAVEL_SEARCH_RESPONSE = "I'll search for travel options for you. Let me find the best matches.";
            public const string DEFAULT_RESPONSE = "I understand your request and I'm here to help you with that.";
            public const string LLM_SERVICE_CALL = "Calling LLM service: {0}";
        }
    }
}
