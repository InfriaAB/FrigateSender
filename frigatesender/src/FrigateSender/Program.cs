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
            int loop = 0;
            while (_ct.IsCancellationRequested == false)
            {
                loop++;
                try
                {
                    var config = ConfigurationReader.Configuration;
                    SetupLogging(config);
                    HandleExit();
                    Log.Logger.Information("Program.Main: Start. Attempt: {0}.", loop);

                    var eventQue = new EventQue(Log.Logger, config);
                    using (var mqttClient = new MQTTClient(config, eventQue, Log.Logger, _ct.Token))
                    {
                        await mqttClient.Start(_ct.Token);

                        var messageHandler = new EventHandler(eventQue, config, Log.Logger);
                        await messageHandler.Start(_ct.Token);

                        while (_ct.IsCancellationRequested == false)
                        {
                            await Task.Delay(200, _ct.Token);
                            await messageHandler.Work(_ct.Token);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Logger.Error(e, "Crashed in Program.Main. Attempt: {0}.", loop);
                    Log.Logger.Information("Should restart shortly.");
                    await Task.Delay(1000);
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