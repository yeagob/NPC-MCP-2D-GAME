using System.Collections.Generic;
using ChatSystem.Models.Context;

namespace ChatSystem.Views.Interfaces
{
    public interface IChatView
    {
        void DisplayMessage(Message message);
        void DisplayMessages(List<Message> messages);
        void ClearMessages();
        void SetInputEnabled(bool enabled);
        void ShowLoadingIndicator(bool show);
    }
}
