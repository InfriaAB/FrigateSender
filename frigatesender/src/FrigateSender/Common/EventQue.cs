using FrigateSender.Models;
using Serilog;

namespace FrigateSender.Common
{
    public class EventQue
    {
        private readonly object _lockObject = new object();
        private readonly List<EventData> _events = new List<EventData>();
        private readonly FrigateSenderConfiguration _configuration;
        private ILogger _logger;

        Dictionary<string, DateTime> _RateLimitCache = new Dictionary<string, DateTime>();

        public EventQue(ILogger logger, FrigateSenderConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void Add(EventData eventData)
        {
            // only care about new and end as they are snapshots and videos
            if (eventData.EventType != EventType.Update)
            {
                if(eventData.EventType == EventType.New)
                {
                    var secondsSinceLastAddByCamera = GetTimeSinceCameraAdd(eventData.CameraName);
                    if(secondsSinceLastAddByCamera != null && secondsSinceLastAddByCamera?.TotalSeconds < _configuration.RateLimit)
                    {
                        _logger.Information($"Skipping snapshot from: {eventData.CameraName} since it posted an image only {((int)(secondsSinceLastAddByCamera?.TotalSeconds ?? -1))} seconds ago. Skipped snapshot Id: {eventData.EventId}.");
                        return;
                    }
                }

                _logger.Information("Event Queued, Is of type: " + eventData.EventType);

                lock (_lockObject)
                {
                    _events.Add(eventData);

                    var eventsCount = _events
                        .GroupBy(e => e.EventType)
                        .Select(e => $"{e.Key}: {e.Count()}");
                    _logger.Information("EventQue: Current event que: " + string.Join(", ", eventsCount));

                    _RateLimitCache[eventData.CameraName] = DateTime.Now;
                }
            }
            else
            {
                _logger.Information("EventQue: Skipping event. Is of type: " + eventData.EventType);
            }
        }

        private TimeSpan? GetTimeSinceCameraAdd(string cameraName)
        {
            if (cameraName == null)
                return null;

            if(_RateLimitCache.ContainsKey(cameraName) == false)
                return null;

            return DateTime.UtcNow - _RateLimitCache[cameraName];
        }

        public EventData? GetNext()
        {
            lock (_lockObject) {
                if (_events.Any() == false)
                    return null;
            }

            // get snapshots first before video.
            lock (_lockObject) {
                var oldestSnapshot = _events
                    .Where(e => e.EventType == EventType.New)
                    .OrderBy(o => o.ReceivedDate)
                    .FirstOrDefault();

                if (oldestSnapshot != null)
                {
                    _logger.Information("EventQue: Found Snapshot to handle.");
                    _events.Remove(oldestSnapshot);
                    return oldestSnapshot;
                }
            }

            // if all snapshots are sent, get videos.
            // only get videos that are older than 1025 seconds
            // as frigate takes up to 25 seconds to save video segments.
            lock (_lockObject)
            {
                var waitedForVideos = _events
                    .Where(e => e.EventType == EventType.End)
                    .Where(e => e.ReceivedDate < DateTime.Now.AddSeconds(-_configuration.FrigateVideoSendDelay)) // frigate writes slowly, let files save to avoid incomplete videos.
                    .OrderBy(o => o.ReceivedDate)
                    .FirstOrDefault();

                if (waitedForVideos != null)
                {
                    _logger.Information("EventQue: Found Video to handle.");
                    _events.Remove(waitedForVideos);
                }

                return waitedForVideos;
            }
        }
    }
}