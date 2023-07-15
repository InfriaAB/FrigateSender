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



    internal class EventQueTests
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
        public void GetSimpleEnqueBackTest()
        {
            var eventQue = new EventQue(Log.Logger, ConfigurationReader.Configuration);
            
            var snapShot1 = new EventData(EventDataTests.testParse1, Log.Logger);
            eventQue.Add(snapShot1);
            
            var returnedSnapShot = eventQue.GetNext();
            Assert.IsTrue(returnedSnapShot == snapShot1);
        }

        [Test]
        [Parallelizable(ParallelScope.Self)]
        public void GetBackSnapshotIfBothVideoAndSnapshotTest()
        {
            var eventQue = new EventQue(Log.Logger, ConfigurationReader.Configuration);

            var snapShot1 = new EventData(EventDataTests.testParse1, Log.Logger);
            var video1 = new EventData(EventDataTests.testParse4, Log.Logger);

            eventQue.Add(snapShot1);
            eventQue.Add(video1);

            var returnedSnapShot = eventQue.GetNext();
            Assert.IsTrue(returnedSnapShot == snapShot1);
        }

        [Test]
        [Parallelizable(ParallelScope.Self)]
        public void VideoIsAwaitedForAskingRightAwayIsNull()
        {
            var eventQue = new EventQue(Log.Logger, ConfigurationReader.Configuration);

            var snapShot1 = new EventData(EventDataTests.testParse1, Log.Logger);
            var video1 = new EventData(EventDataTests.testParse4, Log.Logger);

            eventQue.Add(snapShot1);
            eventQue.Add(video1);

            var trash = eventQue.GetNext();
            var returnedVideo = eventQue.GetNext();
            Assert.IsNull(returnedVideo);
        }

        [Test]
        [Parallelizable(ParallelScope.Self)]
        public async Task WaitingForVideoReturnsVideo()
        {
            var eventQue = new EventQue(Log.Logger, ConfigurationReader.Configuration);

            var snapShot1 = new EventData(EventDataTests.testParse1, Log.Logger);
            var video1 = new EventData(EventDataTests.testParse4, Log.Logger);

            eventQue.Add(snapShot1);
            eventQue.Add(video1);

            await Task.Delay(TimeSpan.FromSeconds(ConfigurationReader.Configuration.FrigateVideoSendDelay + 1));
            var trash = eventQue.GetNext();
            var returnedVideo = eventQue.GetNext();

            Assert.IsTrue(returnedVideo.EventId == video1.EventId);
        }

        [Test]
        [Parallelizable(ParallelScope.Self)]
        public async Task GetBackOldestSnapshotWhenMultipleTest()
        {
            var eventQue = new EventQue(Log.Logger, ConfigurationReader.Configuration);

            var snapShot1 = new EventData(EventDataTests.testParse1, Log.Logger);
            var video1 = new EventData(EventDataTests.testParse4, Log.Logger);

            eventQue.Add(video1);
            eventQue.Add(snapShot1);

            // event que rate limits, need to slow down adding.
            await Task.Delay(TimeSpan.FromSeconds(21));

            var snapShot2 = new EventData(EventDataTests.testParse1, Log.Logger);
            var video2 = new EventData(EventDataTests.testParse4, Log.Logger);
          
            eventQue.Add(snapShot2);
            eventQue.Add(video2);

            var returnedSnapShot = eventQue.GetNext();
            Assert.IsTrue(returnedSnapShot.EventId == snapShot1.EventId);
        }

        [Test]
        [Parallelizable(ParallelScope.Self)]
        public async Task GetBackEventsInRightOrderWhenMultipleTest()
        {
            var eventQue = new EventQue(Log.Logger, ConfigurationReader.Configuration);

            var snapShot1 = new EventData(EventDataTests.testParse1, Log.Logger);
            eventQue.Add(snapShot1);

            await Task.Delay(TimeSpan.FromSeconds(21));
            var snapShot2 = new EventData(EventDataTests.testParse1, Log.Logger);
            eventQue.Add(snapShot2);

            var video1 = new EventData(EventDataTests.testParse4, Log.Logger);
            eventQue.Add(video1);
            await Task.Delay(1000);

            var video2 = new EventData(EventDataTests.testParse4, Log.Logger);
            eventQue.Add(video2);

            await Task.Delay(TimeSpan.FromSeconds(ConfigurationReader.Configuration.FrigateVideoSendDelay + 1));

            var returnedSnapShot1 = eventQue.GetNext();
            var returnedSnapShot2 = eventQue.GetNext();
            
            var returnedVideo1 = eventQue.GetNext();
            var returnedVideo2 = eventQue.GetNext();

            
            Assert.IsTrue(returnedSnapShot1.EventId == snapShot1.EventId);
            Assert.IsTrue(returnedSnapShot2.EventId == snapShot2.EventId);

            Assert.IsTrue(returnedVideo1.EventId == video1.EventId);
            Assert.IsTrue(returnedVideo2.EventId == video2.EventId);
        }

        [Test]
        [Parallelizable(ParallelScope.Self)]
        public async Task AskingForVideoToFastReturnsNothingTest()
        {
            var eventQue = new EventQue(Log.Logger, ConfigurationReader.Configuration);

            var video1 = new EventData(EventDataTests.testParse4, Log.Logger);
            var video2 = new EventData(EventDataTests.testParse4, Log.Logger);

            eventQue.Add(video1);
            eventQue.Add(video2);

            var returnedVideo1 = eventQue.GetNext();
            var returnedVideo2 = eventQue.GetNext();

            Assert.IsNull(returnedVideo1);
            Assert.IsNull(returnedVideo2);
        }
    }
}
