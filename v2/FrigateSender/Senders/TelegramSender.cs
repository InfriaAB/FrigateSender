using FrigateSender.Models;
using Serilog;
using Telegram.Bot;

namespace FrigateSender.Senders
{
    internal class TelegramSender : ISender
    {
        private readonly ILogger _logger;
        private readonly FrigateSenderConfiguration _configuration;
        private readonly TelegramBotClient _client;

        public TelegramSender(FrigateSenderConfiguration configuration, ILogger logger)
        {
            _configuration = configuration;
            _logger = logger;
            _client = new TelegramBotClient(configuration.TelegramToken);
        }

        public async Task SendText(string message, CancellationToken ct)
        {
            try
            {
                await _client.SendTextMessageAsync(_configuration.TelegramChatId, message, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Telegram SendText failed.");
            }
        }

        public async Task SendPhoto(string message, string filePath, CancellationToken ct)
        {

        }

        public async Task SendVideo(string message, string filePath, CancellationToken ct)
        {

        }
    }
}