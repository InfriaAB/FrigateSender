using FrigateSender;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace FrigateSender.Models
{
    internal class FrigateSenderConfiguration
    {
        /// <summary>
        /// Set timout interval.
        /// </summary>
        public int RateLimitTimeout { get; set; } = 20;

        /// <summary>
        /// Max of how many do you want to receive over a X second period.
        /// </summary>
        public int RateLimit { get; set; } = 6;

        /// <summary>
        /// Max file (video) size per upload, files larger than this will be split apart.
        /// This depends on the service you are using, Telegram has max limit of 50Mb per file.
        /// </summary>
        public int MyProperty { get; set; } = 48;

        /// <summary>
        /// Base URL of Home Assistant
        /// </summary>
        public string BaseURL { get; set; } = "https://homeassistant.local:8123";

        /// <summary>
        /// Frigate URL in Home Assitant.
        /// </summary>
        public string SnapShotURL { get; set; } = "{{base_url}}/api/frigate/notifications/{{id}}/snapshot.jpg?quality=90&bbox=1";

        public string VideoURL { get; set; } = "{{base_url}}/api/frigate/notifications/{{id}}/{{camera}}/clip.mp4";
    }
}
