using FrigateSender.Models;
using Serilog;

namespace FrigateSender.Common
{
    public class EventQue
    {
        private readonly object _lockObject = new object();
        private readonly List<EventData> _events = new List<EventData>();
        private ILogger _logger;

        public EventQue(ILogger logger)
        {
            _logger = logger;
        }

        public void Add(EventData eventData)
        {
            // only care about new and end as they are snapshots and videos
            if (eventData.EventType != EventType.Update)
            {
                _logger.Information("Event Queued, Is of type: " + eventData.EventType);

                lock (_lockObject)
                {
                    _events.Add(eventData);

                    var eventsCount = _events
                        .GroupBy(e => e.EventType)
                        .Select(e => $"{e.Key}: {e.Count()}");
                    _logger.Information("EventQue: Current event que: " + string.Join(", ", eventsCount));
                }
            }
            else
            {
                _logger.Information("EventQue: Skipping event. Is of type: " + eventData.EventType);
            }
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
                    .OrderByDescending(o => o.ReceivedDate)
                    .FirstOrDefault();

                if (oldestSnapshot != null)
                {
                    _logger.Information("EventQue: Found Snapshot to handle.");
                    _events.Remove(oldestSnapshot);
                    return oldestSnapshot;
                }
            }

            // if all snapshots are sent, get videos.
            // only get videos that are older than 10 seconds
            // as frigate takes up to 10 seconds to save video segments.
            lock (_lockObject)
            {
                var videosOlderThanTenSeconds = _events
                    .Where(e => e.EventType == EventType.End)
                    .Where(e => e.ReceivedDate < DateTime.Now.AddSeconds(-10))
                    .OrderByDescending(o => o.ReceivedDate)
                    .FirstOrDefault();

                if (videosOlderThanTenSeconds != null)
                {
                    _logger.Information("EventQue: Found Video to handle.");
                    _events.Remove(videosOlderThanTenSeconds);
                }

                return videosOlderThanTenSeconds;
            }
        }
    }
}