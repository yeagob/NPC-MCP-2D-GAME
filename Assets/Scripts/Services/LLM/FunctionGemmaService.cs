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
        private const string DEFAULT_BASE_URL = "http://localhost:11434/api/generate";
        
        public static async Task<LLMResponse> CompleteChatAsync(LLMRequest request, string apiKey, string baseUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(baseUrl)) baseUrl = DEFAULT_BASE_URL;

                LoggingService.LogInfo($"Making FunctionGemma API call to model: {request.model}");
                
                string jsonPayload = BuildFunctionGemmaPayload(request);
                LoggingService.LogInfo($"FunctionGemma Payload: {jsonPayload}");
                
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
                    LoggingService.LogInfo($"FunctionGemma Response: {responseText}");
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
            // Using n8n/Ollama compatible structure as requested:
            // "contents": [ { "role": "developer", "parts": [ { "text": ... }] }, ... ]
            // Note: If using Ollama /api/chat directly, the keys are "role" and "content".
            // The user provided structure looks like Google Vertex AI / Gemini API structure ("contents", "parts", "text")
            // BUT also mentioned "n8n and Render" and "HTTP Request node".
            // If it's a generic endpoint, we should probably stick to OpenAI compatible fields IF the endpoint is OpenAI compatible,
            // OR use the structure the user explicitly pasted:
            /*
            {
              "contents": [
                {
                  "role": "developer",
                  "parts": [
                    {
                      "text": "Eres un asistente..."
                    }
                  ]
                }
              ]
            }
            */
            // Since the user provided a specific JSON payload example, I will assume the endpoint expects this Google/Gemini-like format.
            // HOWEVER, standard Ollama usually uses { "model": "...", "messages": [...] }.
            // The user said "Si estás enviando esto desde un nodo HTTP Request en n8n...".
            // I will implement the Google-style "contents"/"parts" format if that's what's implied, 
            // BUT `DEFAULT_BASE_URL` was localhost:11434 (Ollama). Ollama does NOT support "contents"/"parts" generally (unless using a specific adapter).
            // Ollama supports /api/chat with "messages": [{"role": "...", "content": "..."}].
            // To be safe and compatible with typical local setups (like Ollama running FunctionGemma), I should probably use the STANDARD "messages" format
            // but carefully map the roles as requested.
            // User said: "Turno del Desarrollador (developer)..."
            // Let's stick to the "messages" format for Ollama compatibility but use "developer" role and the prompt injection.
            // PROMPT_FORMAT:
            // Developer message text: "Eres un asistente con acceso a las siguientes funciones: [JSON_SCHEMA]. Si decides llamar a una función, usa el formato: <start_function_call>call:nombre_funcion. {argumentos}<end_function_call>"
            
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"model\":\"{request.model}\",");
            sb.Append($"\"temperature\":{request.temperature.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
            sb.Append($"\"stream\":false,");
            
            sb.Append("\"messages\":[");
            
            // 1. Developer Prompt
            sb.Append("{");
            // If the endpoint doesn't support 'developer', we might need to fallback to 'system', but user stressed 'developer'.
            sb.Append($"\"role\":\"developer\","); 
            
            StringBuilder devContent = new StringBuilder();
            devContent.Append("Eres un asistente con acceso a las siguientes funciones: ");
            
            // Insert JSON Schema of tools
            devContent.Append("[");
            if (request.tools != null && request.tools.Count > 0)
            {
                for (int i = 0; i < request.tools.Count; i++)
                {
                    if (i > 0) devContent.Append(",");
                    // ToFunctionGemmaFormat now returns the raw JSON object string
                    devContent.Append(request.tools[i].ToFunctionGemmaFormat());
                }
            }
            devContent.Append("]. ");
            
            devContent.Append("Si decides llamar a una función, usa el formato: <start_function_call>call:nombre_funcion. {argumentos}<end_function_call>");
            
            // Append ALL system messages (SystemPrompt + ContextPrompts from AgentConfig)
            foreach (Message msg in request.messages)
            {
                if (msg.role == MessageRole.System && !string.IsNullOrEmpty(msg.content))
                {
                    devContent.Append("\n\n");
                    devContent.Append(msg.content);
                }
            }

            sb.Append($"\"content\":\"{EscapeJsonString(devContent.ToString())}\"");
            sb.Append("}");

            // 2. Conversation History
            for (int i = 0; i < request.messages.Count; i++)
            {
                Message msg = request.messages[i];
                if (msg.role == MessageRole.System) continue; // Merged into developer prompt

                sb.Append(",");
                sb.Append("{");
                sb.Append($"\"role\":\"{GetFunctionGemmaRole(msg.role)}\",");
                sb.Append($"\"content\":\"{EscapeJsonString(msg.content)}\"");
                sb.Append("}");
            }
            
            sb.Append("]");
            sb.Append("}");
            return sb.ToString();
        }
        
        private static LLMResponse ParseFunctionGemmaResponse(string responseText, string model)
        {
            try
            {
                string content = ExtractContentFromResponse(responseText);
                
                // Remove <escape> tokens if present
                content = content.Replace("<escape>", "");
                
                List<ToolCall> toolCalls = new List<ToolCall>();
                
                // Parse: <start_function_call>call:name. {args}<end_function_call>
                string tagStart = "<start_function_call>";
                string tagEnd = "<end_function_call>";
                
                int startIndex = content.IndexOf(tagStart);
                while (startIndex != -1)
                {
                    int endIndex = content.IndexOf(tagEnd, startIndex);
                    if (endIndex != -1)
                    {
                        string callContent = content.Substring(startIndex + tagStart.Length, endIndex - (startIndex + tagStart.Length)).Trim();
                        // callContent example: "call:ver. {}" or "call:ir. {x:10, y:20}"
                        
                        if (callContent.StartsWith("call:"))
                        {
                            // Expected formats: 
                            // 1. call:ver. {} 
                            // 2. call:pickup_item{itemId:1}
                            
                            int argsStartIndex = callContent.IndexOf("{");
                            if (argsStartIndex != -1)
                            {
                                int nameStartFn = 5; // "call:" length
                                string functionName = callContent.Substring(nameStartFn, argsStartIndex - nameStartFn).Trim();
                                
                                // Remove trailing dot if present (from "call:ver.")
                                if (functionName.EndsWith("."))
                                {
                                    functionName = functionName.Substring(0, functionName.Length - 1).Trim();
                                }
                                
                                string argsJson = callContent.Substring(argsStartIndex).Trim();
                                
                                try 
                                {
                                    // Parse args (potentially unquoted keys)
                                    // SimpleJsonParser has been updated to handle unquoted keys
                                    Dictionary<string, object> args = SimpleJsonParser.ParseArguments(argsJson);
                                    
                                     toolCalls.Add(new ToolCall(functionName, args)
                                     {
                                         id = Guid.NewGuid().ToString()
                                     });
                                }
                                catch (Exception ex)
                                {
                                     LoggingService.LogError($"Error parsing args for {functionName}: {ex.Message}");
                                }
                            }
                        }
                    }
                    startIndex = content.IndexOf(tagStart, startIndex + 1);
                }

                // If tool calls found, we might want to strip them from content or keep them?
                // Usually for an assistant message we keep the text thought.
                // But if it's purely a function call, content might be empty or just the tags.
                // Let's keep content as is for debugging/history.

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
            // Basic extraction for typical Chat response format
            // {"message": {"content": "..."}} or {"choices": [{"message": {"content": "..."}}]}
            // or "content": "..." 
            // We'll use a precise lookup
            
            // Try OAI Choices format first
            int choicesIndex = json.IndexOf("\"choices\"");
            if (choicesIndex != -1)
            {
                 int contentIdx = json.IndexOf("\"content\":\"", choicesIndex);
                 if (contentIdx != -1)
                 {
                     return ExtractStringValue(json, contentIdx + 10); // 10 = len of "content":"
                 }
            }
            
            // Try direct message format (Ollama streaming=false sometimes)
            int messageIndex = json.IndexOf("\"message\"");
            if (messageIndex != -1)
            {
                int contentIdx = json.IndexOf("\"content\":\"", messageIndex);
                if (contentIdx != -1)
                {
                     return ExtractStringValue(json, contentIdx + 10);
                }
            }
            
            return json; // Fallback
        }
        
        private static string ExtractStringValue(string json, int startQuoteIndex)
        {
             // startQuoteIndex should point to the opening quote of the value
             if (json[startQuoteIndex] != '"') 
             {
                 // Maybe spaces?
                 startQuoteIndex = json.IndexOf("\"", startQuoteIndex);
                 if (startQuoteIndex == -1) return "";
             }
             
             startQuoteIndex++; // Move past opening quote
             
             StringBuilder sb = new StringBuilder();
             bool escaped = false;
             
             for (int i = startQuoteIndex; i < json.Length; i++)
             {
                 char c = json[i];
                 if (escaped)
                 {
                     sb.Append(c); // Simple unescape, proper one needed later if complex
                     escaped = false;
                 }
                 else
                 {
                     if (c == '\\')
                     {
                         escaped = true;
                     }
                     else if (c == '"')
                     {
                         break; // End of string
                     }
                     else
                     {
                         sb.Append(c);
                     }
                 }
             }
             
             return UnescapeJsonString(sb.ToString());
        }

        private static string GetFunctionGemmaRole(MessageRole role)
        {
            return role switch
            {
                MessageRole.User => "user",
                MessageRole.Assistant => "model",
                MessageRole.System => "developer",
                MessageRole.Tool => "tool", // User might need to define tool return format too?
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
