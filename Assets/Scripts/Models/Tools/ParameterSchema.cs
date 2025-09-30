using System;
using System.Collections.Generic;

namespace ChatSystem.Models.Tools
{
    [Serializable]
    public class ParameterSchema
    {
        public string type;
        public string description;
        public List<string> enumValues;
        public object defaultValue;
        
        public ParameterSchema()
        {
            enumValues = new List<string>();
        }
    }
}