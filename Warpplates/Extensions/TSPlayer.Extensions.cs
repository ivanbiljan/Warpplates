using TShockAPI;

namespace Warpplates.Extensions
{
    /// <summary>
    ///     Holds extension methods for the <see cref="TSPlayer" /> type.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class TSPlayerExtensions
    {
        private const string Key = "Warpplates_Metadata";

        /// <summary>
        ///     Gets or creates metadata for the given player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>The metadata.</returns>
        public static PlayerMetadata GetOrCreatePlayerMetadata(this TSPlayer player)
        {
            var metadata = player.GetData<PlayerMetadata>(Key);
            if (metadata == null)
            {
                player.SetData(Key, metadata = new PlayerMetadata());
            }

            return metadata;
        }
    }
}