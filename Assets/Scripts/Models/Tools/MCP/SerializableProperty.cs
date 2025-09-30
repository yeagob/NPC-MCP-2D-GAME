using System;
using UnityEngine;

namespace ChatSystem.Models.Tools.MCP
{
    [Serializable]
    public class SerializableProperty
    {
        public string key;
        public PropertyDefinition value;
        
        public SerializableProperty()
        {
            key = string.Empty;
            value = new PropertyDefinition();
        }
        
        public SerializableProperty(string key, PropertyDefinition value)
        {
            this.key = key;
            this.value = value;
        }
    }
}