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

        public async Task SendText(string message, CancellationToken ct, int? chatId = null)
        {
            var targetChat = chatId ?? _configuration.TelegramChatId;
            try
            {
                await _client.SendTextMessageAsync(targetChat, message, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Telegram SendText failed, target chat: {0}", targetChat);
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
                _logger.Error(ex, "Telegram SendPhoto failed. Message: {0}", message);
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
                _logger.Information("Sending file {0}/{1}.", i, filesToSend.Count());
                var sendMessage = message + $" part: {i}/{filesToSend.Count}.";
                using (FileStream fsSource = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    var inputFile = Telegram.Bot.Types.InputFile.FromStream(fsSource);
                    try
                    {
                        await _client.SendVideoAsync(_configuration.TelegramVideoChatId, inputFile, caption: sendMessage, cancellationToken: ct);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Failed to send video to chat {0}, message: '{1}'", _configuration.TelegramVideoChatId, message);
                    }
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