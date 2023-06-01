using Serilog;

namespace FrigateSender.Tests
{
    internal class EventDataTests
    {
        public static readonly string testParse1 = "{\"before\": {\"id\": \"1685564150.40458-9ls9a6\", \"camera\": \"backyard\", \"frame_time\": 1685564150.40458, \"snapshot_time\": 0.0, \"label\": \"person\", \"sub_label\": null, \"top_score\": 0.0, \"false_positive\": true, \"start_time\": 1685564150.40458, \"end_time\": null, \"score\": 0.6171875, \"box\": [1203, 163, 1238, 273], \"area\": 3850, \"ratio\": 0.3181818181818182, \"region\": [960, 55, 1280, 375], \"stationary\": false, \"motionless_count\": 0, \"position_changes\": 0, \"current_zones\": [], \"entered_zones\": [], \"has_clip\": false, \"has_snapshot\": false}, \"after\": {\"id\": \"1685564150.40458-9ls9a6\", \"camera\": \"backyard\", \"frame_time\": 1685564154.816177, \"snapshot_time\": 1685564154.816177, \"label\": \"person\", \"sub_label\": null, \"top_score\": 0.748046875, \"false_positive\": false, \"start_time\": 1685564150.40458, \"end_time\": null, \"score\": 0.82421875, \"box\": [1006, 145, 1068, 321], \"area\": 10912, \"ratio\": 0.3522727272727273, \"region\": [876, 77, 1196, 397], \"stationary\": false, \"motionless_count\": 5, \"position_changes\": 1, \"current_zones\": [], \"entered_zones\": [], \"has_clip\": true, \"has_snapshot\": true}, \"type\": \"new\"}";
        public static readonly string testParse2 = "{\"before\": {\"id\": \"1685564150.40458-9ls9a6\", \"camera\": \"backyard\", \"frame_time\": 1685564154.816177, \"snapshot_time\": 1685564154.816177, \"label\": \"person\", \"sub_label\": null, \"top_score\": 0.748046875, \"false_positive\": false, \"start_time\": 1685564150.40458, \"end_time\": null, \"score\": 0.82421875, \"box\": [1006, 145, 1068, 321], \"area\": 10912, \"ratio\": 0.3522727272727273, \"region\": [876, 77, 1196, 397], \"stationary\": false, \"motionless_count\": 5, \"position_changes\": 1, \"current_zones\": [], \"entered_zones\": [], \"has_clip\": true, \"has_snapshot\": true}, \"after\": {\"id\": \"1685564150.40458-9ls9a6\", \"camera\": \"backyard\", \"frame_time\": 1685564160.013072, \"snapshot_time\": 1685564155.3998, \"label\": \"person\", \"sub_label\": null, \"top_score\": 0.84375, \"false_positive\": false, \"start_time\": 1685564150.40458, \"end_time\": null, \"score\": 0.84375, \"box\": [1006, 147, 1074, 334], \"area\": 12716, \"ratio\": 0.36363636363636365, \"region\": [776, 32, 1096, 352], \"stationary\": false, \"motionless_count\": 31, \"position_changes\": 1, \"current_zones\": [], \"entered_zones\": [], \"has_clip\": true, \"has_snapshot\": true}, \"type\": \"update\"}";
        public static readonly string testParse3 = "{\"before\": {\"id\": \"1685564150.40458-9ls9a6\", \"camera\": \"backyard\", \"frame_time\": 1685564160.013072, \"snapshot_time\": 1685564155.3998, \"label\": \"person\", \"sub_label\": null, \"top_score\": 0.84375, \"false_positive\": false, \"start_time\": 1685564150.40458, \"end_time\": null, \"score\": 0.84375, \"box\": [1006, 147, 1074, 334], \"area\": 12716, \"ratio\": 0.36363636363636365, \"region\": [776, 32, 1096, 352], \"stationary\": false, \"motionless_count\": 31, \"position_changes\": 1, \"current_zones\": [], \"entered_zones\": [], \"has_clip\": true, \"has_snapshot\": true}, \"after\": {\"id\": \"1685564150.40458-9ls9a6\", \"camera\": \"backyard\", \"frame_time\": 1685564164.813455, \"snapshot_time\": 1685564163.645318, \"label\": \"person\", \"sub_label\": null, \"top_score\": 0.84375, \"false_positive\": false, \"start_time\": 1685564150.40458, \"end_time\": null, \"score\": 0.53515625, \"box\": [1201, 155, 1245, 269], \"area\": 5016, \"ratio\": 0.38596491228070173, \"region\": [960, 48, 1280, 368], \"stationary\": false, \"motionless_count\": 0, \"position_changes\": 1, \"current_zones\": [], \"entered_zones\": [], \"has_clip\": true, \"has_snapshot\": true}, \"type\": \"update\"}";
        public static readonly string testParse4 = "{\"before\": {\"id\": \"1685564150.40458-9ls9a6\", \"camera\": \"backyard\", \"frame_time\": 1685564164.813455, \"snapshot_time\": 1685564163.645318, \"label\": \"person\", \"sub_label\": null, \"top_score\": 0.84375, \"false_positive\": false, \"start_time\": 1685564150.40458, \"end_time\": null, \"score\": 0.53515625, \"box\": [1201, 155, 1245, 269], \"area\": 5016, \"ratio\": 0.38596491228070173, \"region\": [960, 48, 1280, 368], \"stationary\": false, \"motionless_count\": 0, \"position_changes\": 1, \"current_zones\": [], \"entered_zones\": [], \"has_clip\": true, \"has_snapshot\": true}, \"after\": {\"id\": \"1685564150.40458-9ls9a6\", \"camera\": \"backyard\", \"frame_time\": 1685564164.813455, \"snapshot_time\": 1685564163.645318, \"label\": \"person\", \"sub_label\": null, \"top_score\": 0.84375, \"false_positive\": false, \"start_time\": 1685564150.40458, \"end_time\": 1685564169.991358, \"score\": 0.53515625, \"box\": [1201, 155, 1245, 269], \"area\": 5016, \"ratio\": 0.38596491228070173, \"region\": [960, 48, 1280, 368], \"stationary\": false, \"motionless_count\": 0, \"position_changes\": 1, \"current_zones\": [], \"entered_zones\": [], \"has_clip\": true, \"has_snapshot\": true}, \"type\": \"end\"}";

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
        public void ParseTestSnapshot1()
        {
            var e1 = new Models.EventData(testParse1, Log.Logger);
            Assert.IsNotNull(e1.EventId);
            Assert.IsTrue(e1.HasSnapshot);
            Assert.IsTrue(e1.CameraName.Length > 0);
            Assert.IsTrue(e1.EventType == Models.EventType.New);
        }

        [Test]
        public void ParseTestUpdate2()
        {
            var e1 = new Models.EventData(testParse2, Log.Logger);
            Assert.IsNotNull(e1.EventId);
            Assert.IsTrue(e1.HasSnapshot);
            Assert.IsTrue(e1.CameraName.Length > 0);
            Assert.IsTrue(e1.EventType == Models.EventType.Update);
        }

        [Test]
        public void ParseTestUpdate3()
        {
            var e1 = new Models.EventData(testParse3, Log.Logger);
            Assert.IsNotNull(e1.EventId);
            Assert.IsTrue(e1.HasSnapshot);
            Assert.IsTrue(e1.CameraName.Length > 0);
            Assert.IsTrue(e1.EventType == Models.EventType.Update);
        }

        [Test]
        public void ParseTestVideo4()
        {
            var e1 = new Models.EventData(testParse4, Log.Logger);
            Assert.IsNotNull(e1.EventId);
            Assert.IsTrue(e1.HasClip);
            Assert.IsTrue(e1.CameraName.Length > 0);
            Assert.IsTrue(e1.EventType == Models.EventType.End);
        }
    }
}