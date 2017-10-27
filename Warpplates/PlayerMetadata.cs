namespace Warpplates
{
    /// <summary>
    ///     Holds player information.
    /// </summary>
    public sealed class PlayerMetadata
    {
        /// <summary>
        ///     Gets or sets the value indicating whether the player will ignore warpplates.
        /// </summary>
        public bool IsWarpplateAllow { get; set; } = true;

        /// <summary>
        ///     Gets or sets the cooldown.
        /// </summary>
        public int WarpplateCooldown { get; set; }

        /// <summary>
        ///     Gets or sets the remaining warp time.
        /// </summary>
        public int WarpplateDelay { get; set; }
    }
}