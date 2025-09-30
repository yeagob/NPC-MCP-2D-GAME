using UnityEngine;

namespace ChatSystem.Configuration.ScriptableObjects
{
    [CreateAssetMenu(fileName = "PromptConfig", menuName = "LLM/Prompt Configuration", order = 2)]
    public class PromptConfig : ScriptableObject
    {
        [Header("Prompt Settings")]
        public string promptId;
        public string promptName;
        
        [TextArea(10, 30)]
        public string content;
        
        [Header("Configuration")]
        public bool enabled = true;
        public int priority = 0;
        
        [Header("Metadata")]
        public string category;
        public string version = "1.0";
        
        [TextArea(2, 5)]
        public string description;
    }
}