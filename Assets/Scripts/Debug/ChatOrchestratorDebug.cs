using UnityEngine;
using ChatSystem.Services.Orchestrators.Interfaces;

namespace ChatSystem.Debugging
{
    public class ChatOrchestratorDebug : MonoBehaviour
    {
        private IChatOrchestrator orchestrator;
        
        public void SetOrchestrator(IChatOrchestrator chatOrchestrator)
        {
            orchestrator = chatOrchestrator;
        }
        
        [ContextMenu("Test Process Message")]
        public async void TestProcessMessage()
        {
            if (orchestrator == null)
            {
                Debug.LogError("ChatOrchestrator not set");
                return;
            }
            
            try
            {
                var response = await orchestrator.ProcessUserMessageAsync("debug-test", "Hello, this is a test message");
                Debug.Log($"Response: {response.content}");
                Debug.Log($"Success: {response.success}");
                Debug.Log($"Tokens: Input={response.inputTokens}, Output={response.outputTokens}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Test failed: {ex.Message}");
            }
        }
        
        [ContextMenu("Get Context")]
        public void GetContext()
        {
            if (orchestrator == null)
            {
                Debug.LogError("ChatOrchestrator not set");
                return;
            }
            
            var context = orchestrator.GetConversationContext("debug-test");
            Debug.Log($"Context: {context}");
        }
        
        [ContextMenu("Clear Context")]
        public void ClearContext()
        {
            if (orchestrator == null)
            {
                Debug.LogError("ChatOrchestrator not set");
                return;
            }
            
            orchestrator.ClearConversation("debug-test");
            Debug.Log("Context cleared");
        }
    }
}
