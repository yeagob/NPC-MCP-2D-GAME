using System.Threading.Tasks;
using ChatSystem.Models.Context;
using ChatSystem.Services.Orchestrators.Interfaces;
using ChatSystem.Services.Context.Interfaces;

namespace ChatSystem.Controllers.Interfaces
{
    public interface IChatController
    {
        Task ProcessUserMessageAsync(string message);
        void InitializeConversation(string conversationId);
        void SetResponseTarget(Views.Interfaces.IResponsable target);
        void SetChatOrchestrator(IChatOrchestrator orchestrator);
        void SetContextManager(IContextManager manager);
        ConversationContext GetCurrentContext();
        void ClearConversation();
    }
}
