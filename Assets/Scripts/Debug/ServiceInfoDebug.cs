using UnityEngine;

namespace ChatSystem.Debugging
{
    public class ServiceInfoDebug : MonoBehaviour
    {
        [SerializeField] private string serviceName;
        [SerializeField] private string serviceType;
        [SerializeField] private bool isInitialized;
        
        public void Initialize(string name, string type)
        {
            serviceName = name;
            serviceType = type;
            isInitialized = true;
        }
        
        [ContextMenu("Show Service Info")]
        public void ShowServiceInfo()
        {
            Debug.Log($"Service: {serviceName}");
            Debug.Log($"Type: {serviceType}");
            Debug.Log($"Initialized: {isInitialized}");
        }
    }
}
