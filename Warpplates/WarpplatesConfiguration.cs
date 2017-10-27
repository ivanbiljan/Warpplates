using System.IO;
using Newtonsoft.Json;

namespace Warpplates
{
    /// <summary>
    ///     Represents the Warpplates plugin configuration.
    /// </summary>
    public sealed class WarpplatesConfiguration
    {
        /// <summary>
        ///     Gets the value indicating how many seconds need to pass until a player may use a warpplate again.
        /// </summary>
        [JsonProperty("warpplate_cooldown")]
        public int WarpplateCooldown { get; } = 5;

        /// <summary>
        ///     Reads the configuration from the given file, or creates a new one if it doesn't exist.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The configuration.</returns>
        public static WarpplatesConfiguration ReadOrCreate(string path)
        {
            if (File.Exists(path))
            {
                return JsonConvert.DeserializeObject<WarpplatesConfiguration>(File.ReadAllText(path));
            }

            var config = new WarpplatesConfiguration();
            File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
            return config;
        }
    }
}