using FrigateSender.Common;
using FrigateSender.Models;
using Serilog;

namespace FrigateSender
{
    public class MessageHandler
    {
        private EventQue _eventQue;
        private FrigateSenderConfiguration _config;
        private ILogger _logger;

        public MessageHandler(EventQue eventQue, FrigateSenderConfiguration config, ILogger logger)
        {
            _eventQue = eventQue;
            _config = config;
            _logger = logger;
        }

        public async Task Work(CancellationToken token)
        {
            _logger.Information("work.");
        }
    }
}