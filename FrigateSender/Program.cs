using FrigateSender.Common;
using FrigateSender.Models;
using Serilog;

namespace FrigateSender
{
    internal class Program
    {
        private static readonly CancellationTokenSource _ct = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            var config = ConfigurationReader.Configuration;
            SetupLogging(config);
            HandleExit();

            Log.Logger.Information("Program.Main: Start.");

            var eventQue = new EventQue(Log.Logger, config);
            using (var mqttClient = new MQTTClient(config, eventQue, Log.Logger, _ct.Token))
            {
                await mqttClient.Start(_ct.Token);
                
                var messageHandler = new EventHandler(eventQue, config, Log.Logger);
                await messageHandler.Start(_ct.Token);

                while (_ct.IsCancellationRequested == false)
                {
                    await Task.Delay(100, _ct.Token);
                    await messageHandler.Work(_ct.Token);
                }
            }
            Log.Logger.Information("Exiting gracefully.");
        }

        private static void SetupLogging(FrigateSenderConfiguration config)
        {
            var logPath = Path.Join(config.LoggingPath, "log.txt");

            String logTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u}] {Message}{NewLine}{Exception}"; // [{SourceContext}]
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(outputTemplate: logTemplate)
                .WriteTo.File(logPath, outputTemplate: logTemplate)
                .CreateLogger();
        }

        private static void HandleExit()
        {
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Log.Logger.Information("Cancel event triggered.");
                _ct.Cancel();

                eventArgs.Cancel = true;
            };
        }
    }
}