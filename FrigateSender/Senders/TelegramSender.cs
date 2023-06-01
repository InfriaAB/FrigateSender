using FrigateSender.Common;
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
        private readonly VideoHandler _videoHandler;

        public TelegramSender(FrigateSenderConfiguration configuration, ILogger logger)
        {
            _configuration = configuration;
            _logger = logger;
            _client = new TelegramBotClient(configuration.TelegramToken);
            _videoHandler = new VideoHandler(logger);
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
            try
            {
                using (FileStream fsSource = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var inputFile = Telegram.Bot.Types.InputFile.FromStream(fsSource);
                    await _client.SendPhotoAsync(_configuration.TelegramChatId, inputFile, caption: message, cancellationToken: ct);
                    _logger.Information("Telegram Photo sent.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Telegram SendPhoto failed.");
            }
        }

        public async Task SendVideo(string message, string filePath, CancellationToken ct)
        {
            var fileSizeMb = (new FileInfo(filePath)).Length.ConvertBytesToMegabytes();
            var filesToSend = new List<string>();

            if(fileSizeMb <= _configuration.FileSizeMaxPerSend)
            {
                filesToSend.Add(filePath);
            }
            else
            {
                var files = await _videoHandler.SplitVideoToSize(filePath, _configuration.FileSizeMaxPerSend, ct);
                filesToSend.AddRange(files);
            }

            int i = 0;
            foreach(var file in filesToSend)
            {
                i++;
                var sendMessage = message + $" part: {i}/{filesToSend.Count}.";
                using (FileStream fsSource = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    var inputFile = Telegram.Bot.Types.InputFile.FromStream(fsSource);
                    await _client.SendVideoAsync(_configuration.TelegramChatId, inputFile, caption: sendMessage, cancellationToken: ct);
                }
            }

            try
            {
                foreach (var file in filesToSend)
                    if (File.Exists(filePath))
                        File.Delete(file);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Could not delete video files.");
            }

            _logger.Information("Telegram Video sent.");
        }
    }
}