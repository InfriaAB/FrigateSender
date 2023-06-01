using FrigateSender.Common;

namespace FrigateSender.Tests
{
    public class ConfigurationReaderTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var config = ConfigurationReader.Configuration;
            Assert.IsNotNull(config);
            Assert.IsTrue(config.RateLimitTimeout > 0);
        }
    }
}