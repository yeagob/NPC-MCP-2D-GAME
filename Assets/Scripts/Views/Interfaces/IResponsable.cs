using ChatSystem.Models.Context;

namespace ChatSystem.Views.Interfaces
{
    public interface IResponsable
    {
        void ReceiveResponse(Message message);
        void ReceiveError(string errorMessage);
    }
}
