namespace ChatSystem.Models.Communication
{
    [System.Serializable]
    public class WebSocketEvent
    {
        public string type;
        public string eventId;
        public object data;
        public long timestamp;
    }
}