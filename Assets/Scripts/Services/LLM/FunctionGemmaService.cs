using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine;
using ChatSystem.Models.LLM;
using ChatSystem.Models.Context;
using ChatSystem.Models.Tools;
using ChatSystem.Services.Logging;
using ChatSystem.Enums;

namespace ChatSystem.Services.LLM
{
    public class FunctionGemmaService
    {
        private const string DEFAULT_BASE_URL = "http://localhost:11434/api/generate"; // Default to Ollama generic endpoint, but adjustable
        
        public static async Task<LLMResponse> CompleteChatAsync(LLMRequest request, string apiKey, string baseUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(baseUrl)) baseUrl = DEFAULT_BASE_URL;

                LoggingService.LogInfo($"Making FunctionGemma API call to model: {request.model}");
                
                string jsonPayload = BuildFunctionGemmaPayload(request);
                
                UnityWebRequest webRequest = new UnityWebRequest(baseUrl, "POST");
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                
                webRequest.SetRequestHeader("Content-Type", "application/json");
                if (!string.IsNullOrEmpty(apiKey))
                {
                    webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                }
                
                await SendWebRequestAsync(webRequest);
                
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string responseText = webRequest.downloadHandler.text;
                    LoggingService.LogInfo("FunctionGemma API call successful");
                    return ParseFunctionGemmaResponse(responseText, request.model);
                }
                else
                {
                    string error = $"FunctionGemma API Error: {webRequest.error} - {webRequest.downloadHandler.text}";
                    LoggingService.LogError(error);
                    return CreateErrorResponse(request.model, error);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"FunctionGemma API Exception: {ex.Message}");
                return CreateErrorResponse(request.model, ex.Message);
            }
        }
        
        private static string BuildFunctionGemmaPayload(LLMRequest request)
        {
            // FunctionGemma generally expects a raw prompt if using /api/generate in Ollama, 
            // OR if using a chat compatible endpoint it handles messages.
            // Assuming this service connects to an endpoint that accepts standard Chat formats or raw prompts.
            // Given the specific tag requirements, we might want to construction the prompt manually if the backend doesn't handle the template.
            // However, typically we use a chat completion endpoint if available.
            // Let's assume a generic chat completion structure but we insert the definitions in the System/Developer prompt.

            // Since we are likely using Ollama or similar local inference for FunctionGemma:
            // IF using /api/chat (Ollama/OpenAI compatible):
            // We should put the tool definitions in the system message or first user message.
            
            // Let's try to construct a standard Chat payload but inject the tools specially.
            
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"model\":\"{request.model}\",");
            sb.Append($"\"temperature\":{request.temperature},");
            sb.Append($"\"stream\":false,"); // Ensure no streaming for easier parsing
            
            sb.Append("\"messages\":[");
            
            // 1. System/Developer Prompt with Tool Definitions
            sb.Append("{");
            sb.Append($"\"role\":\"system\","); // Or developer
            
            StringBuilder systemContent = new StringBuilder();
            if (request.messages.Count > 0 && request.messages[0].role == MessageRole.System)
            {
                systemContent.Append(request.messages[0].content);
                systemContent.Append("\n\n");
            }
            else
            {
                systemContent.Append("You are a helpful AI assistant.\n\n");
            }

            if (request.tools != null && request.tools.Count > 0)
            {
                systemContent.Append("Available tools:\n");
                foreach (var tool in request.tools)
                {
                    systemContent.Append(tool.ToFunctionGemmaFormat());
                }
            }
            
            sb.Append($"\"content\":\"{EscapeJsonString(systemContent.ToString())}\"");
            sb.Append("}");

            // 2. Conversation History
            for (int i = 0; i < request.messages.Count; i++)
            {
                Message msg = request.messages[i];
                if (msg.role == MessageRole.System) continue; // Already handled

                sb.Append(",");
                sb.Append("{");
                sb.Append($"\"role\":\"{GetFunctionGemmaRole(msg.role)}\",");
                sb.Append($"\"content\":\"{EscapeJsonString(msg.content)}\"");
                sb.Append("}");
            }
            
            sb.Append("]");
            
            // Note: We do NOT pass "tools" array in the JSON if we are manually injecting them into the prompt 
            // for a model that expects raw tokens, UNLESS the backend supports automatic formatting.
            // FunctionGemma is often raw. Let's stick to the prompt injection above.
            
            sb.Append("}");
            return sb.ToString();
        }
        
        private static LLMResponse ParseFunctionGemmaResponse(string responseText, string model)
        {
            try
            {
                // Unwrapping the response format depends on the endpoint (Ollama vs generic).
                // Assuming Ollama /api/chat format or similar wrapping
                
                string content = ExtractContentFromResponse(responseText);
                
                List<ToolCall> toolCalls = new List<ToolCall>();
                
                // Parse <start_function_call>...<end_function_call>
                // Format: <start_function_call>{"name": "function_name", "arguments": { ... }}<end_function_call>
                
                string tagStart = "<start_function_call>";
                string tagEnd = "<end_function_call>";
                
                int startIndex = content.IndexOf(tagStart);
                while (startIndex != -1)
                {
                    int endIndex = content.IndexOf(tagEnd, startIndex);
                    if (endIndex != -1)
                    {
                        string jsonCall = content.Substring(startIndex + tagStart.Length, endIndex - (startIndex + tagStart.Length));
                        
                        // Clean up potentially wrapped markdown
                        jsonCall = jsonCall.Replace("```json", "").Replace("```", "").Trim();
                        
                        try 
                        {
                            // Need a simplistic JSON parser or standard one. 
                            // Assuming SimpleJsonParser similar to QWENService usage
                            // We need to parse: { "name": "...", "arguments": { ... } }
                            
                            // Let's assume we can parse this structure. 
                            // IF the model outputs name separately or arguments heavily nested, we adjust.
                            // Standard FunctionGemma: {"name": "foo", "arguments": {"bar": "baz"}}
                            
                             var parsedCall = SimpleJsonParser.ParseArguments(jsonCall); // Reuse argument parser if it handles general dicts
                             if (parsedCall.ContainsKey("name") && parsedCall.ContainsKey("arguments"))
                             {
                                 string functionName = parsedCall["name"].ToString();
                                 var argumentsObj = parsedCall["arguments"];
                                 Dictionary<string, object> args = null;
                                 
                                 if (argumentsObj is Dictionary<string, object> dictArgs)
                                 {
                                     args = dictArgs;
                                 }
                                 else if (argumentsObj is string strArgs)
                                 {
                                     // Double encoded?
                                     // Try to parse string
                                 }

                                 if (args != null)
                                 {
                                     toolCalls.Add(new ToolCall(functionName, args)
                                     {
                                         id = Guid.NewGuid().ToString() // Generate a local ID
                                     });
                                 }
                             }
                        }
                        catch (Exception ex)
                        {
                             LoggingService.LogError($"Error parsing individual tool call: {ex.Message}");
                        }
                        
                        // Helper to remove the processed tag to find next or just clean output?
                        // Usually we return the content WITH the tags or we strip them.
                        // Agent executor puts content in history.
                    }
                    
                    startIndex = content.IndexOf(tagStart, startIndex + 1);
                }

                return new LLMResponse
                {
                    content = content,
                    toolCalls = toolCalls,
                    model = model,
                    success = true,
                    timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to parse FunctionGemma response: {ex.Message}");
                return CreateErrorResponse(model, "Failed to parse API response");
            }
        }
        
        private static string ExtractContentFromResponse(string json)
        {
            // Simple extraction for standard Chat response {"message": {"content": "..."}} or {"choices": [...]}
            // Adapting to probable Ollama response
            int contentIndex = json.IndexOf("\"content\":\"");
            if (contentIndex != -1)
            {
                contentIndex += 11;
                int endIndex = json.IndexOf("\"", contentIndex);
                // Handle escaped quotes? simpler parser needed for robustness, but here acts as placeholder
                // Ideally we use a robust JSON parser available in the project.
                
                // Let's use the QWEN parser logic or similar if available, otherwise basic substring
                // Assuming SimpleJsonParser fails or is too specific, let's try a heuristic:
                
                // Better: find message object
                 int messageStart = json.IndexOf("\"message\"");
                 if (messageStart != -1)
                 {
                     int contentStart = json.IndexOf("\"content\":\"", messageStart);
                     if (contentStart != -1)
                     {
                         contentStart += 11;
                         // Locate end considering escapes... simplifying for this step
                         // Just searching for the next unescaped quote
                         
                         int current = contentStart;
                         while (current < json.Length)
                         {
                             int nextQuote = json.IndexOf("\"", current);
                             if (nextQuote == -1) break;
                             
                             if (json[nextQuote - 1] != '\\')
                             {
                                 return UnescapeJsonString(json.Substring(contentStart, nextQuote - contentStart));
                             }
                             current = nextQuote + 1;
                         }
                     }
                 }
            }
            return json; // Fallback or empty?
        }

        private static string GetFunctionGemmaRole(MessageRole role)
        {
            return role switch
            {
                MessageRole.User => "user",
                MessageRole.Assistant => "model", // Gemma uses 'model' typically
                MessageRole.System => "system", // or developer
                MessageRole.Tool => "tool", // Should ideally separate tool outputs
                _ => "user"
            };
        }
        
        private static string EscapeJsonString(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
        
        private static string UnescapeJsonString(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return input.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }
        
        private static LLMResponse CreateErrorResponse(string model, string error)
        {
            return new LLMResponse
            {
                content = $"Error: {error}",
                model = model,
                success = false,
                timestamp = DateTime.UtcNow
            };
        }
        
        private static async Task SendWebRequestAsync(UnityWebRequest request)
        {
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();
        }
    }
}
