using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace Warpplates.Database
{
    /// <summary>
    ///     Represents the warpplate manager.
    /// </summary>
    public sealed class WarpplateManager
    {
        private readonly IDbConnection _connection;
        private readonly List<Warpplate> _warpplates = new List<Warpplate>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="WarpplateManager" /> class.
        /// </summary>
        public WarpplateManager()
        {
            switch (TShock.Config.StorageType.ToLower())
            {
                case "mysql":
                    var dbHost = TShock.Config.MySqlHost.Split(':');
                    _connection = new MySqlConnection
                    {
                        ConnectionString =
                            $"Server={dbHost[0]}; " +
                            $"Port={(dbHost.Length == 1 ? "3306" : dbHost[1])}; " +
                            $"Database={TShock.Config.MySqlDbName}; " +
                            $"Uid={TShock.Config.MySqlUsername}; " +
                            $"Pwd={TShock.Config.MySqlPassword};"
                    };
                    break;

                default:
                    var sql = Path.Combine(TShock.SavePath, "tshock.sqlite");
                    _connection = new SqliteConnection($"uri=file://{sql},Version=3");
                    break;
            }

            var sqlCreator = new SqlTableCreator(_connection,
                _connection.GetSqlType() == SqlType.Sqlite
                    ? (IQueryBuilder) new SqliteQueryCreator()
                    : new MysqlQueryCreator());
            sqlCreator.EnsureTableStructure(new SqlTable("Warpplates", new SqlColumn("WorldId", MySqlDbType.Int32),
                new SqlColumn("X", MySqlDbType.Int32), new SqlColumn("Y", MySqlDbType.Int32),
                new SqlColumn("Width", MySqlDbType.Int32), new SqlColumn("Height", MySqlDbType.Int32),
                new SqlColumn("Name", MySqlDbType.Text), new SqlColumn("Tag", MySqlDbType.Text),
                new SqlColumn("Delay", MySqlDbType.Int32), new SqlColumn("Destination", MySqlDbType.Text),
                new SqlColumn("IsPublic", MySqlDbType.Int32)));
            sqlCreator.EnsureTableStructure(new SqlTable("WarpplateIsAllowed",
                new SqlColumn("WorldId", MySqlDbType.Int32), new SqlColumn("Warpplate", MySqlDbType.Text),
                new SqlColumn("UserId", MySqlDbType.Int32)));
        }

        /// <summary>
        ///     Writes the specified warpplate to the database.
        /// </summary>
        /// <param name="warpplate">The warpplate.</param>
        public void Add(Warpplate warpplate)
        {
            _warpplates.Add(warpplate);
            _connection.Query(
                "INSERT INTO Warpplates (WorldId, X, Y, Width, Height, Name, Tag, Delay, IsPublic) VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8)",
                warpplate.WorldId, warpplate.X, warpplate.Y, warpplate.Area.Width, warpplate.Area.Height,
                warpplate.Name, warpplate.Tag, warpplate.Delay, warpplate.IsPublic ? 1 : 0);
        }

        /// <summary>
        ///     Gets the warpplate by name match.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The warpplate.</returns>
        public Warpplate Get(string name)
        {
            return _warpplates.SingleOrDefault(w => w.Name.Equals(name));
        }

        /// <summary>
        ///     Gets the warpplate at the specified coordinates.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <remarks>A null check is in order as the method will return null if there are no warpplates found.</remarks>
        public Warpplate Get(int x, int y)
        {
            return _warpplates.SingleOrDefault(w => w.IsWarpplateArea(x, y));
        }

        /// <summary>
        ///     Gets an enumerable collection of defined warpplates.
        /// </summary>
        /// <returns>A collection of defined warpplates.</returns>
        public IEnumerable<Warpplate> GetWarpplates()
        {
            return _warpplates.AsReadOnly();
        }

        /// <summary>
        ///     Loads the warpplates into cache memory.
        /// </summary>
        public void Load()
        {
            _warpplates.Clear();
            using (var reader = _connection.QueryReader("SELECT * FROM Warpplates WHERE WorldId = @0", Main.worldID))
            {
                while (reader.Read())
                {
                    var worldId = reader.Get<int>("WorldId");
                    var x = reader.Get<int>("X");
                    var y = reader.Get<int>("Y");
                    var area = new Rectangle(x, y, reader.Get<int>("Width"),
                        reader.Get<int>("Height"));
                    var name = reader.Get<string>("Name");
                    var tag = reader.Get<string>("Tag");
                    var delay = reader.Get<int>("Delay");
                    var destination = reader.Get<string>("Destination");
                    var isPublic = reader.Get<int>("IsPublic") == 1;

                    var warpplate = new Warpplate(worldId, name, x, y)
                    {
                        Area = area,
                        Delay = delay,
                        DestinationWarpplate = destination,
                        IsPublic = isPublic,
                        Tag = tag
                    };
                    using (var reader2 = _connection.QueryReader(
                        "SELECT UserId FROM WarpplateIsAllowed WHERE WorldId = @0 AND Warpplate = @1", worldId, name))
                    {
                        while (reader2.Read())
                        {
                            var userId = reader2.Get<int>("UserId");
                            warpplate.AllowedUsers.Add(userId);
                        }
                    }

                    _warpplates.Add(warpplate);
                }
            }
        }

        /// <summary>
        ///     Removes the specified warpplate.
        /// </summary>
        /// <param name="warpplate">The warpplate.</param>
        public void Remove(Warpplate warpplate)
        {
            _warpplates.RemoveAll(w => w.X == warpplate.X && w.Y == warpplate.Y && w.Name.Equals(warpplate.Name));
            _connection.Query("DELETE FROM Warpplates WHERE WorldId = @0 AND Name = @1", warpplate.WorldId,
                warpplate.Name);
            _connection.Query("DELETE FROM WarpplateIsAllowed WHERE WorldId = @0 AND Warpplate = @1", warpplate.WorldId,
                warpplate.Name);
        }

        /// <summary>
        ///     Updates the specified warpplate.
        /// </summary>
        /// <param name="warpplate">The warpplate.</param>
        public void Update(Warpplate warpplate)
        {
            _connection.Query(
                "UPDATE Warpplates SET Tag = @0, Delay = @1, Destination = @2, IsPublic = @3 WHERE WorldId = @4 AND Name = @5",
                warpplate.Tag, warpplate.Delay, warpplate.DestinationWarpplate, warpplate.IsPublic, warpplate.WorldId,
                warpplate.Name);
            _connection.Query("DELETE FROM WarpplateIsAllowed WHERE WorldId = @0 AND Warpplate = @1", warpplate.WorldId,
                warpplate.Name);
            using (var connection = _connection.CloneEx())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (var command = (SqliteCommand) connection.CreateCommand())
                        {
                            command.CommandText =
                                "INSERT INTO WarpplateIsAllowed (WorldId, Warpplate, UserId) VALUES (@0, @1, @2)";
                            command.AddParameter("@0", warpplate.WorldId);
                            command.AddParameter("@1", warpplate.Name);
                            command.AddParameter("@2", null);

                            foreach (var userId in warpplate.AllowedUsers)
                            {
                                command.Parameters["@2"].Value = userId;
                                command.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        try
                        {
                            transaction.Rollback();
                        }
                        catch (Exception ex)
                        {
                            TShock.Log.ConsoleError($"An exception has occured during database rollback: {ex.Message}");
                        }
                    }
                }
            }
        }
    }
}