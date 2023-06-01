using FrigateSender.Common;
using FrigateSender.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrigateSender.Tests
{
    internal class VideoHandlerTests
    {
        [SetUp]
        public void Setup()
        {
            String logTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u}] {Message}{NewLine}{Exception}"; // [{SourceContext}]
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: logTemplate)
                .CreateLogger();
        }

        [Test]
        [Parallelizable(ParallelScope.Self)]
        public async Task SplitVideoTest()
        {
            var vh = new VideoHandler(Log.Logger);
            var files = await vh.SplitVideoToSize("media/TestVideoHandlerSplitFile.mp4", 5, CancellationToken.None);
        }
    }
}
