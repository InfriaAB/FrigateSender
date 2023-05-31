namespace FrigateSender.Senders
{
    internal interface ISender
    {
        Task SendText(string message, CancellationToken ct);
        Task SendPhoto(string message, string filePath, CancellationToken ct);
        Task SendVideo(string message, string filePath, CancellationToken ct);
    }
}