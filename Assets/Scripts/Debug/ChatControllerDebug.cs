using UnityEngine;
using ChatSystem.Controllers.Interfaces;

namespace ChatSystem.Debugging
{
    public class ChatControllerDebug : MonoBehaviour
    {
        private IChatController controller;
        
        public void SetController(IChatController chatController)
        {
            controller = chatController;
        }
        
        [ContextMenu("Test Process Message")]
        public async void TestProcessMessage()
        {
            if (controller == null)
            {
                Debug.LogError("ChatController not set");
                return;
            }
            
            try
            {
                await controller.ProcessUserMessageAsync("Debug test message from controller");
                Debug.Log("Controller message processing completed");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Controller test failed: {ex.Message}");
            }
        }
        
        [ContextMenu("Send Multiple Test Messages")]
        public async void SendMultipleTestMessages()
        {
            if (controller == null)
            {
                Debug.LogError("ChatController not set");
                return;
            }
            
            string[] testMessages = {
                "Hello, how are you?",
                "What can you help me with?",
                "Tell me a joke",
                "What's the weather like?"
            };
            
            foreach (var message in testMessages)
            {
                try
                {
                    Debug.Log($"Sending: {message}");
                    await controller.ProcessUserMessageAsync(message);
                    await System.Threading.Tasks.Task.Delay(1000);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to send message '{message}': {ex.Message}");
                }
            }
            
            Debug.Log("Multiple message test completed");
        }
    }
}
