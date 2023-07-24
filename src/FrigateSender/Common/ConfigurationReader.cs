using FrigateSender.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FrigateSender.Common
{
    public static class ConfigurationReader
    {
        private static readonly string _configurationFileName = "frigateSenderConfiguration.yaml";
        private static FrigateSenderConfiguration? _configCache = null;
        public static FrigateSenderConfiguration Configuration { get { return GetConfiguration(); } }

        private static FrigateSenderConfiguration GetConfiguration()
        {
            if (_configCache != null)
                return _configCache;

            if (File.Exists(_configurationFileName) == false)
            {
                CreateConfiguration();
            }

            _configCache = ReadFile<FrigateSenderConfiguration>(_configurationFileName);
            
            return _configCache;
        }

        private static T ReadFile<T>(string filePath)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreFields()
                .IgnoreUnmatchedProperties()
                .Build();

            var rawData = File.ReadAllText(filePath);
            var deserialized = deserializer.Deserialize<T>(rawData);

            return deserialized;
        }

        private static void CreateConfiguration()
        {
            var defaultConfig = new FrigateSenderConfiguration();
            WriteFile(defaultConfig, _configurationFileName);
        }

        private static void WriteFile<T>(T dataObject, string name)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithNewLine(Environment.NewLine)
                .WithQuotingNecessaryStrings(true)
                .Build();

            var yaml = serializer.Serialize(dataObject);
            File.WriteAllText(_configurationFileName, yaml, System.Text.Encoding.UTF8);
        }
    }
}
