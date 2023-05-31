using FrigateSender.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrigateSender.Senders
{
    internal class TelegramSender : ISender
    {
        private readonly FrigateSenderConfiguration _configuration;

        public TelegramSender(FrigateSenderConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendText(string message, CancellationToken ct)
        {

        }

        public async Task SendPhoto(string message, string filePath, CancellationToken ct)
        {

        }

        public async Task SendVideo(string message, string filePath, CancellationToken ct)
        {

        }
    }
}