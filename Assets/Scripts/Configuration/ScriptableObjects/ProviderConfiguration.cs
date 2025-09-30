using UnityEngine;
using ChatSystem.Enums;

namespace ChatSystem.Configuration.ScriptableObjects
{
    [CreateAssetMenu(fileName = "ProviderConfig", menuName = "LLM/Provider Configuration", order = 1)]
    public class ProviderConfiguration : ScriptableObject
    {
        [Header("Provider Settings")]
        public ServiceProvider provider;
        public string token;
        public string serviceUrl;
        
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(serviceUrl);
        }
    }
}
