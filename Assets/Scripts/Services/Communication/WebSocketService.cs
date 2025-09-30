using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using NativeWebSocket;
using ChatSystem.Services.Communication.Interfaces;
using ChatSystem.Services.Logging;
using ChatSystem.Models.Communication;

namespace ChatSystem.Services.Communication
{
    public class WebSocketService : IWebSocketService
    {
        private WebSocket webSocket;
        private bool isDisposed;
        private string lastError = string.Empty;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
        public event Action<WebSocketEvent> OnEventReceived;

        public bool IsConnected => webSocket?.State == WebSocketState.Open;
        public string ConnectionStatus => webSocket?.State.ToString() ?? "Disconnected";

        public async Task ConnectAsync(string url, string apiKey)
        {
            try
            {
                if (webSocket != null && IsConnected)
                {
                    LoggingService.LogWarning("WebSocket already connected");
                    return;
                }

                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    {"Authorization", $"Bearer {apiKey}"},
                    {"OpenAI-Beta", "realtime=v1"}
                };

                webSocket = new WebSocket(url, headers);
                
                webSocket.OnOpen += HandleOnOpen;
                webSocket.OnError += HandleOnError;
                webSocket.OnClose += HandleOnClose;
                webSocket.OnMessage += HandleOnMessage;

                LoggingService.LogInfo($"Connecting to WebSocket: {url}");
                await webSocket.Connect();
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to connect WebSocket: {ex.Message}");
                OnError?.Invoke(ex.Message);
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                if (webSocket != null && IsConnected)
                {
                    LoggingService.LogInfo("Disconnecting WebSocket");
                    await webSocket.Close();
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error disconnecting WebSocket: {ex.Message}");
            }
        }

        public async Task SendEventAsync(WebSocketEvent eventData)
        {
            if (!IsConnected)
            {
                LoggingService.LogWarning("WebSocket not connected, cannot send event");
                return;
            }

            try
            {
                string jsonData = CreateEventJson(eventData);
                await webSocket.SendText(jsonData);
                LoggingService.LogDebug($"Sent WebSocket event: {eventData.type}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to send event: {ex.Message}");
                OnError?.Invoke(ex.Message);
            }
        }

        public async Task SendTextAsync(string message)
        {
            if (!IsConnected)
            {
                LoggingService.LogWarning("WebSocket not connected, cannot send text");
                return;
            }

            try
            {
                await webSocket.SendText(message);
                LoggingService.LogDebug("Sent text message via WebSocket");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to send text: {ex.Message}");
                OnError?.Invoke(ex.Message);
            }
        }

        public async Task SendAudioAsync(byte[] audioData)
        {
            if (!IsConnected)
            {
                LoggingService.LogWarning("WebSocket not connected, cannot send audio");
                return;
            }

            try
            {
                string base64Audio = Convert.ToBase64String(audioData);
                WebSocketEvent audioEvent = new WebSocketEvent
                {
                    type = "input_audio_buffer.append",
                    eventId = Guid.NewGuid().ToString(),
                    data = new { audio = base64Audio },
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                await SendEventAsync(audioEvent);
                LoggingService.LogDebug($"Sent audio data: {audioData.Length} bytes");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to send audio: {ex.Message}");
                OnError?.Invoke(ex.Message);
            }
        }

        private void HandleOnOpen()
        {
            LoggingService.LogInfo("WebSocket connection opened");
            OnConnected?.Invoke();
        }

        private void HandleOnError(string errorMsg)
        {
            lastError = errorMsg;
            LoggingService.LogError($"WebSocket error: {errorMsg}");
            OnError?.Invoke(errorMsg);
        }

        private void HandleOnClose(WebSocketCloseCode closeCode)
        {
            LoggingService.LogInfo($"WebSocket connection closed: {closeCode}");
            OnDisconnected?.Invoke();
        }

        private void HandleOnMessage(byte[] data)
        {
            try
            {
                string message = Encoding.UTF8.GetString(data);
                LoggingService.LogDebug($"Received WebSocket message: {message.Substring(0, Math.Min(100, message.Length))}...");
                
                WebSocketEvent receivedEvent = ParseMessage(message);
                if (receivedEvent != null)
                {
                    OnEventReceived?.Invoke(receivedEvent);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to handle WebSocket message: {ex.Message}");
                OnError?.Invoke(ex.Message);
            }
        }

        private string CreateEventJson(WebSocketEvent eventData)
        {
            return $@"{{
                ""type"": ""{eventData.type}"",
                ""event_id"": ""{eventData.eventId}""
                {(eventData.data != null ? $@",""data"": {ConvertToJson(eventData.data)}" : "")}
            }}";
        }

        private WebSocketEvent ParseMessage(string message)
        {
            try
            {
                return new WebSocketEvent
                {
                    type = ExtractJsonValue(message, "type"),
                    eventId = ExtractJsonValue(message, "event_id"),
                    data = message,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to parse WebSocket message: {ex.Message}");
                return null;
            }
        }

        private string ExtractJsonValue(string json, string key)
        {
            string searchKey = $@"""{key}"":""";
            int startIndex = json.IndexOf(searchKey);
            if (startIndex == -1) return string.Empty;
            
            startIndex += searchKey.Length;
            int endIndex = json.IndexOf('"', startIndex);
            if (endIndex == -1) return string.Empty;
            
            return json.Substring(startIndex, endIndex - startIndex);
        }

        private string ConvertToJson(object data)
        {
            if (data == null) return "null";
            if (data is string str) return $@"""{str}""";
            return data.ToString();
        }

        public void Dispose()
        {
            if (isDisposed) return;
            
            try
            {
                webSocket?.Close();
                webSocket = null;
                isDisposed = true;
                LoggingService.LogInfo("WebSocketService disposed");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error disposing WebSocketService: {ex.Message}");
            }
        }
    }
}