namespace FrigateSender.Senders
{
    internal interface ISender
    {
        Task SendText(string message, CancellationToken ct, int? chatId = null);
        Task SendPhoto(string message, string filePath, CancellationToken ct);
        Task SendVideo(string message, string filePath, CancellationToken ct);
    }
}