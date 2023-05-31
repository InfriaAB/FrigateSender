using FrigateSender.Common;
using FrigateSender.Models;
using FrigateSender.Senders;
using Serilog;
using System.ComponentModel;
using Telegram.Bot.Requests;

namespace FrigateSender
{
    public class MessageHandler
    {
        private EventQue _eventQue;
        private FrigateSenderConfiguration _config;
        private ILogger _logger;
        private List<ISender> _senders = new List<ISender>();

        public MessageHandler(EventQue eventQue, FrigateSenderConfiguration config, ILogger logger)
        {
            _eventQue = eventQue;
            _config = config;
            _logger = logger;

            _senders.Add(new TelegramSender(config, _logger));
        }

        public async Task Start(CancellationToken ct)
        {
            foreach (var sender in _senders)
            {
                await sender.SendText("FrigateSender is Online.", ct);
            }
        }

        public async Task Work(CancellationToken ct)
        {
            var nextEvent = _eventQue.GetNext();
            if (nextEvent != null)
            {
                _logger.Information($"Got new event to send: {nextEvent.EventId} - {nextEvent.EventType}");
                await HandledEvent(nextEvent, ct);
            }
        }

        private async Task HandledEvent(EventData ev, CancellationToken ct)
        {
            if(ev.EventType == EventType.New)
            {
                SendSnapshot(ev, ct);
            }
            else if(ev.EventType == EventType.End)
            {
                SendVideo(ev, ct);
            }
            else
            {
                _logger.Error($"Unhandled event type in MessageHandler: {ev.EventType}");
            }
        }

        private void SendSnapshot(EventData ev, CancellationToken ct)
        {
            // throw new NotImplementedException();
        }
        private void SendVideo(EventData ev, CancellationToken ct)
        {
            // throw new NotImplementedException();
        }
    }
}