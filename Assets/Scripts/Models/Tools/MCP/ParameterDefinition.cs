using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChatSystem.Models.Tools.MCP
{
    [Serializable]
    public class ParameterDefinition
    {
        public string type = "object";
        public List<SerializableProperty> properties;
        public List<string> required;
        
        public ParameterDefinition()
        {
            properties = new List<SerializableProperty>();
            required = new List<string>();
        }
        
        public Dictionary<string, PropertyDefinition> GetPropertiesDictionary()
        {
            Dictionary<string, PropertyDefinition> dict = new Dictionary<string, PropertyDefinition>();
            foreach (SerializableProperty prop in properties)
            {
                if (!string.IsNullOrEmpty(prop.key))
                {
                    dict[prop.key] = prop.value;
                }
            }
            return dict;
        }
        
        public void SetPropertiesFromDictionary(Dictionary<string, PropertyDefinition> dict)
        {
            properties.Clear();
            foreach (KeyValuePair<string, PropertyDefinition> kvp in dict)
            {
                properties.Add(new SerializableProperty(kvp.Key, kvp.Value));
            }
        }
        
        public void AddProperty(string key, PropertyDefinition property)
        {
            properties.Add(new SerializableProperty(key, property));
        }
        
        public bool HasProperty(string key)
        {
            foreach (SerializableProperty prop in properties)
            {
                if (prop.key == key) return true;
            }
            return false;
        }
        
        public PropertyDefinition GetProperty(string key)
        {
            foreach (SerializableProperty prop in properties)
            {
                if (prop.key == key) return prop.value;
            }
            return null;
        }
    }
}