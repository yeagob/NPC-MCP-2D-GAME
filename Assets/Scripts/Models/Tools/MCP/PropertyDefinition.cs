using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChatSystem.Models.Tools.MCP
{
    [Serializable]
    public class PropertyDefinition
    {
        public string type;
        public string description;
        public List<string> enumValues;
        public ItemDefinition items;
        public object defaultValue;
        
        public PropertyDefinition()
        {
            enumValues = new List<string>();
        }
    }
}