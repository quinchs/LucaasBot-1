using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot
{
    /// <summary>
    ///     Represents a configuration state for the bot.
    /// </summary>
    public class Config
    {
        /// <summary>
        ///     The token of the bot.
        /// </summary>
        public string Token { get; set; } = "TOKEN_HERE";

        /// <summary>
        ///     The mongo connection string.
        /// </summary>
        public string MongoCS { get; set; } = "MONGO_CS_HERE";

        /// <summary>
        ///     The Hapsy file host token.
        /// </summary>
        public string HapsyToken { get; set; } = "HAPSY_TOKEN_HERE";

        /// <summary>
        ///     The Google API Key to use for music.
        /// </summary>
        public string GoogleApiKey { get; set; } = "GOOGLE_API_KEY_HERE";
    }

    /// <summary>
    ///     Represents a static service to interact with a config file.
    /// </summary>
    public class ConfigService
    {
        /// <summary>
        ///     The location of the config file relative to the current directory.
        /// </summary>
        public const string ConfigPath = @"./Config.json";

        /// <summary>
        ///     The currently loaded config.
        /// </summary>
        public static Config Config { get; private set; }

        /// <summary>
        ///     Loads the configuration file, setting the <see cref="Config"/> field.
        /// </summary>
        public static void LoadConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                var c = new Config();
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(c));

                throw new FileNotFoundException("No config file found, please make a config file in the current directory!");
            }

            var json = File.ReadAllText(ConfigPath);

            Config = JsonConvert.DeserializeObject<Config>(json);
        }

        /// <summary>
        ///     Saves the provided config class to the config file.
        /// </summary>
        /// <param name="conf">The new config class to save</param>
        public static void SaveConfig(Config conf)
        {
            var json = JsonConvert.SerializeObject(conf, Formatting.Indented);

            File.WriteAllText(ConfigPath, json);

            Config = conf;
        }
    }
}
