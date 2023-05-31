using Serilog;

namespace FrigateSender.Models
{
    public enum EventType
    {
        Snapshot = 0, 
        Video = 1
    }

    public class EventData
    {
        private string payloadString;
        private ILogger logger;

        public string EventId {get; private set; }
        public EventType EventType { get; private set; }
        public string CameraName { get; private set; }
        public string ObjectType { get; private set; }
        public double Score { get; private set; }
        public bool HasClip { get; private set; }
        public bool  HasSnapshot { get; private set; }

        public DateTime ReceivedDate { get; set; }

        public EventData(string payloadString, ILogger logger)
        {
            this.payloadString = payloadString;
            this.logger = logger;
        }
    }
}

