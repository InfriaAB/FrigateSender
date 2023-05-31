namespace FrigateSender.Models
{
    public class FrigateSenderConfiguration
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
        public int FileSizeMaxPerSend { get; set; } = 48;

        /// <summary>
        /// Base URL of Home Assistant
        /// </summary>
        public string BaseURL { get; set; } = "https://homeassistant.local:8123";

        /// <summary>
        /// Frigate URL in HomeAssistant for Snapshots.
        /// </summary>
        public string SnapShotURL { get; set; } = "{{base_url}}/api/frigate/notifications/{{id}}/snapshot.jpg?quality=90&bbox=1";

        /// <summary>
        /// Frigate URL in HomeAssistant for Videos.
        /// </summary>
        public string VideoURL { get; set; } = "{{base_url}}/api/frigate/notifications/{{id}}/{{camera}}/clip.mp4";

        /// <summary>
        /// Where to store log files.
        /// </summary>
        public string LoggingPath { get; set; } = "logs/MiniNVR.log";

        /// <summary>
        /// What level of logging to do.
        /// 10 = Debug, 20 = Info. Debug = more spam and details.
        /// </summary>
        public int LoggingLevel { get; set; } = 10;


        /// <summary>
        /// MQTT Adress that is being used by Frigate (probably same as Home Assistant).
        /// </summary>
        public string MQTTAddress { get; set; } = "homeassistant.local";

        /// <summary>
        /// MQTT Port.
        /// </summary>
        public int MQTTPort { get; set; } = 1883;

        /// <summary>
        /// Timeout tolerance
        /// </summary>
        public int MQTTTimeout { get; set; } = 60;

        /// <summary>
        /// MQTT User
        /// </summary>
        public string MQTTUser { get; set; } = "user";

        /// <summary>
        /// MQTT Password
        /// </summary>
        public string MQTTPassword { get; set; } = "password";

        /// <summary>
        /// Telegram Chat bot token
        /// </summary>
        public string TelegramToken { get; set; } = "123:1234";

        /// <summary>
        /// Telegram Chat to send to.
        /// </summary>
        public int TelegramChatId { get; set; } = -123;
    }
}
