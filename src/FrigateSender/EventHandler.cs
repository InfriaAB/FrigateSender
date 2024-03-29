﻿using FrigateSender.Common;
using FrigateSender.Models;
using FrigateSender.Senders;
using Serilog;

namespace FrigateSender
{
    public class EventHandler
    {
        private EventQue _eventQue;
        private FrigateSenderConfiguration _config;
        private ILogger _logger;
        private List<ISender> _senders = new List<ISender>();

        public EventHandler(EventQue eventQue, FrigateSenderConfiguration config, ILogger logger)
        {
            _eventQue = eventQue;
            _config = config;
            _logger = logger;

            _senders.Add(new TelegramSender(config, _logger));

            if (Directory.Exists(_config.TemporaryFolder) == false)
                Directory.CreateDirectory(_config.TemporaryFolder);
        }

        public async Task Start(CancellationToken ct)
        {
            foreach (var sender in _senders)
            {
                await sender.SendText("FrigateSender is Online.", ct);
                if(_config.TelegramChatId != _config.TelegramVideoChatId)
                {
                    await sender.SendText("FrigateSender is Online.", ct, _config.TelegramVideoChatId);
                }
            }
        }

        public async Task Work(CancellationToken ct)
        {
            var nextEvent = _eventQue.GetNext();
            if (nextEvent != null)
            {
                _logger.Information("--Start Eventhandling: {0}, Type: {1} --", nextEvent.EventId, nextEvent.EventType);
                await HandledEvent(nextEvent, ct);
                _logger.Information("--Event handled: {0} --", nextEvent.EventId);
            }
        }

        private async Task HandledEvent(EventData ev, CancellationToken ct)
        {
            if(ev.EventType == EventType.New)
            {
                await SendSnapshot(ev, ct);
            }
            else if(ev.EventType == EventType.End)
            {
                await SendVideo(ev, ct);
            }
            else
            {
                _logger.Error("Unhandled event type in MessageHandler: {0}", ev.EventType);
            }
        }

        private async Task SendSnapshot(EventData ev, CancellationToken ct)
        {
            var snapshotURL = _config.SnapShotURL
                .Replace("{{base_url}}", _config.BaseURL)
                .Replace("{{id}}", ev.EventId);

            string? filePath = TryGetFile(snapshotURL, ".jpg", 10, 10, ct);
            if (filePath != null)
            {
                var message = $"Snapshot: {ev.ObjectType.FirstLetterToUpper()}({ev.Score}) in {ev.CameraName.FirstLetterToUpper()}, {ev.ReceivedDate.ToString("yyyy-MM-dd HH:mm:ss")}, id: {ev.EventId}.";
                foreach (var sender in _senders)
                {
                    await sender.SendPhoto(message, filePath, ct);
                }
            }

            Cleanup(filePath);
        }

        private async Task SendVideo(EventData ev, CancellationToken ct)
        {
            var videoURL = _config.VideoURL
                .Replace("{{base_url}}", _config.BaseURL)
                .Replace("{{camera}}", ev.CameraName)
                .Replace("{{id}}", ev.EventId);

            string? filePath = TryGetFile(videoURL, ".mp4", 10, 1000, ct);
            if (filePath != null)
            {
                var message = $"Video: {ev.ObjectType.FirstLetterToUpper()}({ev.Score}) in {ev.CameraName.FirstLetterToUpper()}, {ev.ReceivedDate.ToString("yyyy-MM-dd HH:mm:ss")}, id: {ev.EventId}.";
                foreach (var sender in _senders)
                {
                    await sender.SendVideo(message, filePath, ct);
                }
            }

            Cleanup(filePath);
        }

        private string? TryGetFile(string url, string fileType, int maxAttempts, int minFileSize, CancellationToken ct)
        {
            int attempts = 0;
            long fileSize = 0;
            string? filePath = null;

            while ((attempts < maxAttempts) && fileSize < minFileSize)
            {
                attempts++;
                filePath = DownloadFile(url, fileType, ct);

                if (filePath != null && File.Exists(filePath))
                    fileSize = (new FileInfo(filePath)).Length;

                _logger.Information("Download attempt {0}/{1}, FileSize: {2}Mb, Path: {3}", 
                    attempts, maxAttempts, Math.Round(fileSize.ConvertBytesToMegabytes(), 2), filePath);
            }

            return filePath;
        }

        private string? DownloadFile(string fileURL, string fileType, CancellationToken ct)
        {
            string? tempFile = null;
            try
            {
                var fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + Guid.NewGuid().ToString().Replace("-", "") + fileType;
                tempFile = Path.Join(_config.TemporaryFolder, fileName);

                _logger.Information("Downloading {0} from: {1}.", tempFile, fileURL);

                using (var handler = new HttpClientHandler() { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator })
                using (var client = new HttpClient(handler))
                {
                    using (var s = client.GetStreamAsync(fileURL, ct))
                    {
                        using (var fs = new FileStream(tempFile, FileMode.OpenOrCreate))
                        {
                            s.Result.CopyTo(fs);
                        }
                    }
                }

                return tempFile;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed downloading file.");
                Cleanup(tempFile);
            }

            return null;
        }

        private void Cleanup(string? filePath)
        {
            try
            {
                if (filePath != null)
                {
                    if(File.Exists(filePath))
                        File.Delete(filePath);
                    
                    _logger.Information("File deleted: {0}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Could not delete file: {0}.", filePath);
            }
        }
    }
}