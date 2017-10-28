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
        ///     Gets a value that defines the maximum warpplate height.
        /// </summary>
        [JsonProperty("max_warpplate_height", Order = 3)]
        public int MaxWarpplateHeight { get; } = 5;

        /// <summary>
        ///     Gets a value that defines the maximum warpplate width.
        /// </summary>
        [JsonProperty("max_warpplate_width", Order = 2)]
        public int MaxWarpplateWidth { get; } = 5;

        /// <summary>
        ///     Gets a value indicating how many seconds need to pass until a player may use a warpplate again.
        /// </summary>
        [JsonProperty("warpplate_cooldown", Order = 1)]
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