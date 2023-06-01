using Serilog;
using System.Text.Json;

namespace FrigateSender.Models
{
    public enum EventType
    {
        Unknown = 0,
        
        /// <summary>
        /// Snapshot
        /// </summary>
        New = 1, 

        Update = 2,

        /// <summary>
        ///  Video
        /// </summary>
        End = 3
    }

    public class EventData
    {
        private string payloadString;
        private ILogger _logger;

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
            _logger = logger;
            this.payloadString = payloadString;
            
            var jsonObject = System.Text.Json.JsonDocument.Parse(payloadString);

            ReceivedDate = DateTime.Now;
            
            var hasAfter = jsonObject.RootElement.TryGetProperty("after", out JsonElement afterElement);
            if (hasAfter)
            {
                if (afterElement.TryGetProperty("id", out JsonElement eventId))
                {
                    EventId = eventId.ToString();
                }

                if (jsonObject.RootElement.TryGetProperty("type", out JsonElement eventType))
                {
                    var type = eventType.ToString();

                    if (type == "new")
                        EventType = EventType.New;

                    else if (type == "update")
                        EventType = EventType.Update;

                    else if (type == "end")
                        EventType = EventType.End;

                    else
                        EventType = EventType.Unknown;
                }

                if (afterElement.TryGetProperty("camera", out JsonElement cameraName))
                {
                    CameraName = cameraName.ToString();
                }

                if (afterElement.TryGetProperty("label", out JsonElement objectType))
                {
                    ObjectType = objectType.ToString();
                }

                if (afterElement.TryGetProperty("score", out JsonElement score))
                {
                    Score = Math.Round(score.GetDouble() * 100, 0);
                }

                if (afterElement.TryGetProperty("has_snapshot", out JsonElement hasSnapshot))
                {
                    HasSnapshot = hasSnapshot.GetBoolean();
                }

                if (afterElement.TryGetProperty("has_clip", out JsonElement hasClip))
                {
                    HasClip = hasClip.GetBoolean();
                }
            }
        }
    }
}

