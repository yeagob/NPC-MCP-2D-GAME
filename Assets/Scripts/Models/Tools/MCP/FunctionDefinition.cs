using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChatSystem.Models.Tools.MCP
{
    [Serializable]
    public class FunctionDefinition
    {
        public string name;
        public string description;
        public ParameterDefinition parameters;
    }
}