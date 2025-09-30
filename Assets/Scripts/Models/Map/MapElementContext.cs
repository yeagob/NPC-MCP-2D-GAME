using System;
using System.Collections.Generic;
using UnityEngine;

namespace MapSystem.Models.Context
{
    [Serializable]
    public class MapElementContext
    {
        [Header("Element Information")]
        public string elementId;
        public string displayName;
        public string description;
        
        [Header("Metadata")]
        public float creationTime;
        public Vector2 initialPosition;
        public Dictionary<string, object> customProperties;
        
        [Header("Interaction History")]
        public List<string> interactionHistory;
        
        public MapElementContext()
        {
            elementId = System.Guid.NewGuid().ToString();
            creationTime = 0;
            customProperties = new Dictionary<string, object>();
            interactionHistory = new List<string>();
        }
        
        public MapElementContext(string displayName, string description) : this()
        {
            this.displayName = displayName;
            this.description = description;
        }
        
        public void AddInteraction(string interaction)
        {
            string timestampedInteraction = $"[{Time.time:F2}] {interaction}";
            interactionHistory.Add(timestampedInteraction);
        }
        
        public void SetProperty(string key, object value)
        {
            customProperties[key] = value;
        }
        
        public T GetProperty<T>(string key, T defaultValue = default(T))
        {
            if (customProperties.TryGetValue(key, out object value) && value is T)
            {
                return (T)value;
            }
            return defaultValue;
        }
        
        public bool HasProperty(string key)
        {
            return customProperties.ContainsKey(key);
        }
        
        public void RemoveProperty(string key)
        {
            customProperties.Remove(key);
        }
        
        public string[] GetRecentInteractions(int count = 5)
        {
            int startIndex = Mathf.Max(0, interactionHistory.Count - count);
            int actualCount = Mathf.Min(count, interactionHistory.Count);
            
            string[] recentInteractions = new string[actualCount];
            for (int i = 0; i < actualCount; i++)
            {
                recentInteractions[i] = interactionHistory[startIndex + i];
            }
            
            return recentInteractions;
        }
        
        public void ClearHistory()
        {
            interactionHistory.Clear();
        }
        
        public int GetInteractionCount()
        {
            return interactionHistory.Count;
        }
    }
}