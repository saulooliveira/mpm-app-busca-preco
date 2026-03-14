using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace BuscaPreco.Infrastructure.Data
{
    public  class AppConfig
    {
        public  DbfConfig DbfConfig { get; set; }
    }

    public class DbfConfig
    {
        public string DbfFilePath { get; set; }
    }

    public class ConfigReader
    {
        private readonly string _configFilePath;

        public ConfigReader(string configFilePath)
        {
            _configFilePath = configFilePath;
        }

        public DbfConfig LoadConfig()
        {
            if (!File.Exists(_configFilePath))
            {
                throw new FileNotFoundException("Arquivo de configuração YAML não encontrado.", _configFilePath);
            }

            var deserializer = new DeserializerBuilder()
                .Build();

            using (var reader = new StreamReader(_configFilePath))
            {
                var config = deserializer.Deserialize<AppConfig>(reader);
                return config.DbfConfig;
            }
        }
    }
}
