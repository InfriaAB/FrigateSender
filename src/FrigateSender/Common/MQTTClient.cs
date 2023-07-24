using MQTTnet.Client;
using MQTTnet;
using FrigateSender.Models;
using System;
using System.Text;
using FrigateSender.Common;
using Serilog;

namespace FrigateSender
{
    internal class MQTTClient: IDisposable
    {
        private readonly FrigateSenderConfiguration _configuration;
        private readonly EventQue _eventQue;
        private readonly ILogger _logger;
        private readonly MqttFactory _MQTTFactory;
        private readonly IMqttClient _MQTTClient;

        public MQTTClient(
            FrigateSenderConfiguration configuration,
            EventQue eventQue,
            ILogger logger,
            CancellationToken ct
        )
        {
            _configuration = configuration;
            _eventQue = eventQue;
            _logger = logger;
            _MQTTFactory = new MqttFactory();
            _MQTTClient = _MQTTFactory.CreateMqttClient();

            _MQTTClient.ApplicationMessageReceivedAsync += e =>
            {
                _logger.Information("Event received.");

                var payload = e.ApplicationMessage.PayloadSegment;
                var payloadByteArray = payload.ToArray();
                string payloadString = Encoding.UTF8.GetString(payloadByteArray, 0, payloadByteArray.Length);
                try
                {
                    logger.Information(payloadString);
                    var eventData = new EventData(payloadString, _logger);
                    eventQue.Add(eventData);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to add event.");
                }

                return Task.CompletedTask;
            };

            // This will also trigger on failure to connect causing endless
            // loop of trying to connect every one second untill success.
            _MQTTClient.DisconnectedAsync += async e =>
            {
                _logger.Information("MQTT Disconnected, Code: {0}({1}).", e.Reason.ToString(), (int)e.Reason);
                _logger.Information("Retrying connection in 2 seconds");
                await Task.Delay(2000);
                await Start(ct);
            };
        }

        public async Task Start(CancellationToken ct)
        {
            _logger.Information("Connecting to MQTT: {0}:{1}", _configuration.MQTTAddress, _configuration.MQTTPort);

            var mqttClientOptions = new MqttClientOptionsBuilder()
               .WithTcpServer(_configuration.MQTTAddress, _configuration.MQTTPort)
               .WithCredentials(_configuration.MQTTUser, _configuration.MQTTPassword)
               .Build();

            try
            {
                var result = await _MQTTClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
                _logger.Information("Connection result code: {0}({1}).", result.ResultCode.ToString(), (int)result.ResultCode);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to connect: {0}.", ex.Message);
                // this will cause DisconnectedAsync to trigger and retrying connection.
                return;
            }

            var mqttSubscribeOptions = _MQTTFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(
                    f =>
                    {
                        f.WithTopic(_configuration.MQTTTopic);
                    })
                .Build();

            await _MQTTClient.SubscribeAsync(mqttSubscribeOptions, ct);
            _logger.Information("Subscribed to: {0}", _configuration.MQTTTopic);
        }

        public async void Dispose()
        {
            await _MQTTClient.DisconnectAsync();
            _MQTTClient.Dispose();

            _logger.Information("MQTT Disposed.");
        }
    }
}
