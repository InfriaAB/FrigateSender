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
                // only rate limit new as this is when snapshots are sent.
                if (eventData.EventType == EventType.New) 
                {
                    _logger.Information("is new event");
                    var secondsSinceLastAddByCamera = GetTimeSinceCameraAdd(eventData.CameraName);
                    if(secondsSinceLastAddByCamera != null && secondsSinceLastAddByCamera < _configuration.RateLimitTimeout)
                    {
                        _logger.Information("Skipping Snapshot from: {0} since it posted an image only {1} seconds ago. Skipped snapshot Id: {2}.", eventData.CameraName, ((int)(secondsSinceLastAddByCamera ?? -1)), eventData.EventId);
                        return;
                    }
                    else 
                        _logger.Information("{0} passed rate limit test, seconds: {1}", eventData.CameraName, secondsSinceLastAddByCamera);
                }

                _logger.Information("Event Queued from {0}, Is of type: {1}.", eventData.CameraName, eventData.EventType);

                lock (_lockObject)
                {
                    _events.Add(eventData);

                    var eventsCount = _events
                        .GroupBy(e => e.EventType)
                        .Select(e => $"{e.Key}: {e.Count()}");
                    _logger.Information("EventQue: Current event que: " + string.Join(", ", eventsCount));

                    // save last time snapshot was sent (new == snapshot).
                    if (eventData.EventType == EventType.New)
                    {
                        _logger.Information("Updating Snapshottime for {0}", eventData.CameraName);
                        _RateLimitCache[eventData.CameraName] = DateTime.Now;
                    }
                }
            }
            else
            {
                _logger.Information("EventQue: Skipping event. Is of type: " + eventData.EventType);
            }
        }

        private int? GetTimeSinceCameraAdd(string cameraName)
        {
            if (cameraName == null)
                return null;

            if(_RateLimitCache.ContainsKey(cameraName) == false)
                return null;

            return (int) Math.Round((DateTime.Now - _RateLimitCache[cameraName]).TotalSeconds, 0);
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