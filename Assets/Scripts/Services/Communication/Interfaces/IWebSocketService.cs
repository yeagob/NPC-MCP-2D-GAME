using System;
using System.Threading.Tasks;
using ChatSystem.Models.Communication;

namespace ChatSystem.Services.Communication.Interfaces
{
    public interface IWebSocketService : IDisposable
    {
        event Action OnConnected;
        event Action OnDisconnected;
        event Action<string> OnError;
        event Action<WebSocketEvent> OnEventReceived;
        
        Task ConnectAsync(string url, string apiKey);
        Task DisconnectAsync();
        Task SendEventAsync(WebSocketEvent eventData);
        Task SendTextAsync(string message);
        Task SendAudioAsync(byte[] audioData);
        
        bool IsConnected { get; }
        string ConnectionStatus { get; }
    }
}