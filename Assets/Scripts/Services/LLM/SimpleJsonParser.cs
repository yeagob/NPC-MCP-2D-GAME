using System;
using System.Collections.Generic;

namespace ChatSystem.Services.LLM
{
    public static class SimpleJsonParser
    {
        public static Dictionary<string, object> ParseArguments(string jsonString)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            
            if (string.IsNullOrEmpty(jsonString) || jsonString.Trim() == "{}")
            {
                return result;
            }
            
            try
            {
                string content = jsonString.Trim();
                if (!content.StartsWith("{") || !content.EndsWith("}"))
                {
                    return result;
                }
                
                content = content.Substring(1, content.Length - 2);
                
                List<string> keyValuePairs = SplitJsonContent(content);
                
                foreach (string pair in keyValuePairs)
                {
                    KeyValuePair<string, object> kvp = ParseKeyValuePair(pair);
                    if (!string.IsNullOrEmpty(kvp.Key))
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Failed to parse JSON arguments: {ex.Message}");
            }
            
            return result;
        }
        
        private static List<string> SplitJsonContent(string content)
        {
            List<string> pairs = new List<string>();
            int bracketLevel = 0;
            int quoteCount = 0;
            int start = 0;
            
            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];
                
                if (c == '"' && (i == 0 || content[i - 1] != '\\'))
                {
                    quoteCount++;
                }
                else if (quoteCount % 2 == 0)
                {
                    if (c == '{' || c == '[')
                    {
                        bracketLevel++;
                    }
                    else if (c == '}' || c == ']')
                    {
                        bracketLevel--;
                    }
                    else if (c == ',' && bracketLevel == 0)
                    {
                        string pair = content.Substring(start, i - start).Trim();
                        if (!string.IsNullOrEmpty(pair))
                        {
                            pairs.Add(pair);
                        }
                        start = i + 1;
                    }
                }
            }
            
            if (start < content.Length)
            {
                string lastPair = content.Substring(start).Trim();
                if (!string.IsNullOrEmpty(lastPair))
                {
                    pairs.Add(lastPair);
                }
            }
            
            return pairs;
        }
        
        private static KeyValuePair<string, object> ParseKeyValuePair(string pair)
        {
            int colonIndex = FindColonIndex(pair);
            if (colonIndex == -1)
            {
                return new KeyValuePair<string, object>();
            }
            
            string key = ExtractKey(pair.Substring(0, colonIndex));
            string valueStr = pair.Substring(colonIndex + 1).Trim();
            object value = ParseValue(valueStr);
            
            return new KeyValuePair<string, object>(key, value);
        }
        
        private static int FindColonIndex(string pair)
        {
            bool inQuotes = false;
            
            for (int i = 0; i < pair.Length; i++)
            {
                char c = pair[i];
                
                if (c == '"' && (i == 0 || pair[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                }
                else if (!inQuotes && c == ':')
                {
                    return i;
                }
            }
            
            return -1;
        }
        
        private static string ExtractKey(string keyStr)
        {
            keyStr = keyStr.Trim();
            
            if (keyStr.StartsWith("\"") && keyStr.EndsWith("\""))
            {
                return keyStr.Substring(1, keyStr.Length - 2);
            }
            
            return keyStr;
        }
        
        private static object ParseValue(string valueStr)
        {
            valueStr = valueStr.Trim();
            
            if (string.IsNullOrEmpty(valueStr))
            {
                return null;
            }
            
            if (valueStr.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            
            if (valueStr.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            if (valueStr.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            
            if (valueStr.StartsWith("\"") && valueStr.EndsWith("\""))
            {
                return UnescapeJsonString(valueStr.Substring(1, valueStr.Length - 2));
            }
            
            if (valueStr.StartsWith("[") && valueStr.EndsWith("]"))
            {
                return ParseArray(valueStr);
            }
            
            if (valueStr.StartsWith("{") && valueStr.EndsWith("}"))
            {
                return ParseArguments(valueStr);
            }
            
            if (int.TryParse(valueStr, out int intValue))
            {
                return intValue;
            }
            
            if (float.TryParse(valueStr, out float floatValue))
            {
                return floatValue;
            }
            
            return valueStr;
        }
        
        private static List<object> ParseArray(string arrayStr)
        {
            List<object> result = new List<object>();
            
            if (arrayStr.Length <= 2)
            {
                return result;
            }
            
            string content = arrayStr.Substring(1, arrayStr.Length - 2).Trim();
            if (string.IsNullOrEmpty(content))
            {
                return result;
            }
            
            List<string> elements = SplitArrayContent(content);
            
            foreach (string element in elements)
            {
                result.Add(ParseValue(element));
            }
            
            return result;
        }
        
        private static List<string> SplitArrayContent(string content)
        {
            List<string> elements = new List<string>();
            int bracketLevel = 0;
            int quoteCount = 0;
            int start = 0;
            
            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];
                
                if (c == '"' && (i == 0 || content[i - 1] != '\\'))
                {
                    quoteCount++;
                }
                else if (quoteCount % 2 == 0)
                {
                    if (c == '{' || c == '[')
                    {
                        bracketLevel++;
                    }
                    else if (c == '}' || c == ']')
                    {
                        bracketLevel--;
                    }
                    else if (c == ',' && bracketLevel == 0)
                    {
                        string element = content.Substring(start, i - start).Trim();
                        if (!string.IsNullOrEmpty(element))
                        {
                            elements.Add(element);
                        }
                        start = i + 1;
                    }
                }
            }
            
            if (start < content.Length)
            {
                string lastElement = content.Substring(start).Trim();
                if (!string.IsNullOrEmpty(lastElement))
                {
                    elements.Add(lastElement);
                }
            }
            
            return elements;
        }
        
        private static string UnescapeJsonString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            
            return input
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t");
        }
    }
}
