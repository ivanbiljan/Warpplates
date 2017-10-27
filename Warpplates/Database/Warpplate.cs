using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Warpplates.Database
{
    /// <summary>
    ///     Represents a warpplate.
    /// </summary>
    public sealed class Warpplate
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Warpplate" /> class.
        /// </summary>
        /// <param name="worldId">The world ID.</param>
        /// <param name="name">The warpplate name.</param>
        /// <param name="x">The warpplate's X coordinate.</param>
        /// <param name="y">The warpplate's Y coordinate.</param>
        public Warpplate(int worldId, string name, int x, int y)
        {
            WorldId = worldId;
            Name = Tag = name;
            X = x;
            Y = y;
        }

        /// <summary>
        ///     Gets a list of users allowed to use this warpplate.
        /// </summary>
        public IList<int> AllowedUsers { get; } = new List<int>();

        /// <summary>
        ///     Gets or sets the area.
        /// </summary>
        public Rectangle Area { get; set; }

        /// <summary>
        ///     Gets or sets the delay.
        /// </summary>
        public int Delay { get; set; }

        /// <summary>
        ///     Gets or sets the destination warpplate name.
        /// </summary>
        public string DestinationWarpplate { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this warpplate's use is restricted to specific members.
        /// </summary>
        public bool IsPublic { get; set; } = true;

        /// <summary>
        ///     Gets the name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Gets or sets the display tag.
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        ///     Gets the X coordinate.
        /// </summary>
        public int X { get; }

        /// <summary>
        ///     Gets the Y coordinate.
        /// </summary>
        public int Y { get; }

        /// <summary>
        ///     Gets the world ID.
        /// </summary>
        public int WorldId { get; }

        /// <summary>
        ///     Determines whether the given coordinates are a part of the warpplate's area.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <returns><c>true</c> if the coordinates are a part of the area; otherwise, <c>false</c>.</returns>
        public bool IsWarpplateArea(int x, int y)
        {
            return x >= Area.Left && x <= Area.Right && y >= Area.Top && y <= Area.Bottom;
        }
    }
}