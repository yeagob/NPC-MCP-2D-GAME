using UnityEngine;
using ChatSystem.Services.Orchestrators.Interfaces;
using ChatSystem.Models.Context;

namespace ChatSystem.Debugging
{
    public class LLMOrchestratorDebug : MonoBehaviour
    {
        private ILLMOrchestrator orchestrator;
        
        public void SetOrchestrator(ILLMOrchestrator llmOrchestrator)
        {
            orchestrator = llmOrchestrator;
        }
        
     
        [ContextMenu("Test Process Message")]
        public async void TestProcessMessage()
        {
            if (orchestrator == null)
            {
                Debug.LogError("LLMOrchestrator not set");
                return;
            }
            
            try
            {
                var context = new ConversationContext("debug-llm-test");
                context.AddUserMessage("Test message for LLM processing");
                
                var response = await orchestrator.ProcessMessageAsync(context);
                Debug.Log($"LLM Response: {response.content}");
                Debug.Log($"Success: {response.success}");
                Debug.Log($"Tool Calls: {response.toolCalls.Count}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"LLM Test failed: {ex.Message}");
            }
        }
    }
}
