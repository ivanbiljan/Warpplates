using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Warpplates.Database;
using Warpplates.Extensions;

namespace Warpplates
{
    /// <summary>
    ///     Represents the Warpplates plugin.
    /// </summary>
    [ApiVersion(2, 1)]
    public sealed class WarpplatesPlugin : TerrariaPlugin
    {
        private static readonly string ConfigPath = Path.Combine(TShock.SavePath, "warpplates.json");
        private readonly WarpplateManager _warpplateManager = new WarpplateManager();

        private DateTime _lastGameUpdate = DateTime.Now;

        private WarpplatesConfiguration _warpplatesConfig;

        /// <inheritdoc />
        public WarpplatesPlugin(Main game) : base(game)
        {
        }

        /// <inheritdoc />
        public override string Author => "Ivan";

        /// <inheritdoc />
        public override string Description => "Adds automatic warpplates.";

        /// <inheritdoc />
        public override string Name => "Warpplates";

        /// <inheritdoc />
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnGamePostInitialize);
                ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
            }

            base.Dispose(disposing);
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            ServerApi.Hooks.GamePostInitialize.Register(this, OnGamePostInitialize, -1);
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);

            Commands.ChatCommands.Add(new Command("warpplates.define", WarpplateCommand, "warpplate", "wp"));
        }

        private void OnGamePostInitialize(EventArgs args)
        {
            _warpplatesConfig = WarpplatesConfiguration.ReadOrCreate(ConfigPath);
            _warpplateManager.Load();
        }

        private void OnGameUpdate(EventArgs args)
        {
            if ((DateTime.Now - _lastGameUpdate).TotalSeconds < 1)
            {
                return;
            }

            foreach (var player in TShock.Players.Where(p => p != null))
            {
                var playerMetadata = player.GetOrCreatePlayerMetadata();
                var warpplate = _warpplateManager.Get(player.TileX, player.TileY);
                if (warpplate == null)
                {
                    playerMetadata.WarpplateDelay = 0;
                    continue;
                }

                var destinationWarpplate = _warpplateManager.Get(warpplate.DestinationWarpplate);
                if (destinationWarpplate == null)
                {
                    continue;
                }
                if (!warpplate.IsPublic && !warpplate.AllowedUsers.Contains(player.User?.ID ?? -1) ||
                    !playerMetadata.IsWarpplateAllow)
                {
                    continue;
                }
                if (playerMetadata.WarpplateCooldown > 0)
                {
                    playerMetadata.WarpplateCooldown--;
                    continue;
                }
                if (playerMetadata.WarpplateDelay < destinationWarpplate.Delay)
                {
                    var remainingTime = destinationWarpplate.Delay - playerMetadata.WarpplateDelay++;
                    player.SendInfoMessage(
                        $"You will be warped to '{destinationWarpplate.Name}' in {remainingTime} second{(remainingTime > 1 ? "s" : string.Empty)}.");
                }
                else
                {
                    player.Teleport(destinationWarpplate.X * 16, destinationWarpplate.Y * 16);
                    player.SendSuccessMessage($"You've been warped to '{destinationWarpplate.Name}'.");
                    playerMetadata.WarpplateCooldown = _warpplatesConfig.WarpplateCooldown;
                    playerMetadata.WarpplateDelay = 0;
                }
            }
            _lastGameUpdate = DateTime.Now;
        }

        private void OnWarpplateAllow(CommandArgs args)
        {
            if (!args.Player.HasPermission("warpplates.togglepublic"))
            {
                args.Player.SendErrorMessage("You do not have permission to allow other players.");
                return;
            }

            if (args.Parameters.Count != 3)
            {
                args.Player.SendErrorMessage(
                    $"Invalid syntax! Proper syntax: {Commands.Specifier}warpplate allow <warpplate name> <player name>");
                return;
            }

            var warpplateName = args.Parameters[1];
            var warpplate = _warpplateManager.Get(warpplateName);
            if (warpplate == null)
            {
                args.Player.SendErrorMessage($"Invalid warpplate '{warpplateName}'.");
                return;
            }

            var username = args.Parameters[2];
            var user = TShock.Users.GetUserByName(username);
            if (user == null)
            {
                args.Player.SendErrorMessage("Invalid user.");
            }
            else
            {
                if (!warpplate.AllowedUsers.Contains(user.ID))
                {
                    warpplate.AllowedUsers.Add(user.ID);
                    args.Player.SendSuccessMessage($"'{user.Name}' is now allowed to use warpplate '{warpplateName}'.");
                }
                else
                {
                    warpplate.AllowedUsers.Remove(user.ID);
                    args.Player.SendSuccessMessage(
                        $"'{user.Name}' is no longer allowed to use warpplate '{warpplateName}'.");
                }
            }
        }

        private void OnWarpplateDelay(CommandArgs args)
        {
            if (!args.Player.HasPermission("warpplates.setdelay"))
            {
                args.Player.SendErrorMessage("You do not have permission to set warpplate delays.");
                return;
            }

            if (args.Parameters.Count != 3)
            {
                args.Player.SendErrorMessage(
                    $"Invalid syntax! Proper syntax: {Commands.Specifier}warpplate delay <warpplate name> <delay>");
                return;
            }

            var warpplateName = args.Parameters[1];
            var warpplate = _warpplateManager.Get(warpplateName);
            if (warpplate == null)
            {
                args.Player.SendErrorMessage($"Invalid warpplate '{warpplateName}'.");
            }
            else if (!int.TryParse(args.Parameters[2], out var delay))
            {
                args.Player.SendErrorMessage("Invalid delay.");
            }
            else
            {
                warpplate.Delay = delay;
                _warpplateManager.Update(warpplate);
                args.Player.SendSuccessMessage(
                    $"Set delay for warpplate '{warpplateName}' to {delay} second{(delay > 1 ? "s" : string.Empty)}");
            }
        }

        private void OnWarpplateDelete(CommandArgs args)
        {
            if (!args.Player.HasPermission("warpplates.delete"))
            {
                args.Player.SendErrorMessage("You do not have permission to delete warpplates.");
                return;
            }

            if (args.Parameters.Count != 2)
            {
                args.Player.SendErrorMessage(
                    $"Invalid syntax! Proper syntax: {Commands.Specifier}warpplate delete <warpplate name>");
                return;
            }

            var warpplateName = args.Parameters[1];
            var warpplate = _warpplateManager.Get(warpplateName);
            if (warpplate == null)
            {
                args.Player.SendErrorMessage($"Invalid warpplate '{warpplateName}'.");
            }
            else
            {
                _warpplateManager.Remove(warpplate);
                args.Player.SendSuccessMessage($"Removed warpplate '{warpplateName}'.");
            }
        }

        private void OnWarpplateDestination(CommandArgs args)
        {
            if (!args.Player.HasPermission("warpplates.setdestination"))
            {
                args.Player.SendErrorMessage("You do not have permission to set warpplate destinations.");
                return;
            }

            if (args.Parameters.Count != 3)
            {
                args.Player.SendErrorMessage(
                    $"Invalid syntax! Proper syntax: {Commands.Specifier}warpplate destination <warpplate name> <desination warpplate name>");
                return;
            }

            var warpplateName = args.Parameters[1];
            var warpplate = _warpplateManager.Get(warpplateName);
            if (warpplate == null)
            {
                args.Player.SendErrorMessage($"Invalid warpplate '{warpplateName}'.");
                return;
            }

            var destinationWarpplateName = args.Parameters[2];
            var destinationWarpplate = _warpplateManager.Get(destinationWarpplateName);
            if (destinationWarpplate == null)
            {
                args.Player.SendErrorMessage($"Invalid destination warpplate '{destinationWarpplateName}'.");
                return;
            }

            warpplate.DestinationWarpplate = destinationWarpplateName;
            _warpplateManager.Update(warpplate);
            args.Player.SendSuccessMessage(
                $"Set destination for warpplate '{warpplateName}' to '{destinationWarpplateName}'.");
        }

        private void OnWarpplateInfo(CommandArgs args)
        {
            if (args.Parameters.Count != 2)
            {
                args.Player.SendErrorMessage(
                    $"Invalid syntax! Proper syntax: {Commands.Specifier}warpplate info <warpplate name>");
                return;
            }

            var warpplateName = args.Parameters[1];
            var warpplate = _warpplateManager.Get(warpplateName);
            if (warpplate == null)
            {
                args.Player.SendErrorMessage($"Invalid warpplate '{warpplateName}'.");
            }
            else
            {
                args.Player.SendInfoMessage($"Name: {warpplate.Name}");
                args.Player.SendInfoMessage($"Tag: {warpplate.Tag}");
                args.Player.SendInfoMessage($"IsPublic: {warpplate.IsPublic}");
                args.Player.SendInfoMessage($"Destination: {warpplate.DestinationWarpplate}");
                args.Player.SendInfoMessage($"Delay: {warpplate.Delay}");
            }
        }

        private void OnWarpplateList(CommandArgs args)
        {
            if (args.Parameters.Count > 2)
            {
                args.Player.SendErrorMessage(
                    $"Invalid syntax! Proper syntax: {Commands.Specifier}warpplate list <page>");
                return;
            }
            if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out var pageNumber))
            {
                return;
            }

            var warpplates = from warpplate in _warpplateManager.GetWarpplates()
                orderby warpplate.Name
                select warpplate.Name;
            var dataToPaginate = warpplates as IList<string> ?? warpplates.ToList();
            PaginationTools.SendPage(args.Player, pageNumber, dataToPaginate, dataToPaginate.Count, new PaginationTools.Settings
            {
                HeaderFormat = "Warpplates ({0}/{1})",
                FooterFormat = $"Type {Commands.Specifier}warpplate list {{0}} for more."
            });
        }

        private void OnWarpplateListAllowed(CommandArgs args)
        {
            if (args.Parameters.Count < 2 || args.Parameters.Count > 3)
            {
                args.Player.SendErrorMessage(
                    $"Invalid syntax! Proper syntax: {Commands.Specifier}warpplate listallowed <warpplate name> [page]");
                return;
            }

            var warpplateName = args.Parameters[1];
            var warpplate = _warpplateManager.Get(warpplateName);
            if (warpplate == null)
            {
                args.Player.SendErrorMessage($"Invalid warpplate '{warpplateName}'.");
            }
            else if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out var pageNumber))
            {
            }
            else
            {
                var users = from userId in warpplate.AllowedUsers
                    let user = TShock.Users.GetUserByID(userId)
                    where user != null
                    orderby user.Name
                    select user.Name;
                var dataToPaginate = users as IList<string> ?? users.ToList();
                PaginationTools.SendPage(args.Player, pageNumber, dataToPaginate, dataToPaginate.Count, new PaginationTools.Settings
                {
                    HeaderFormat = "Allowed Users ({0}/{1})",
                    FooterFormat = $"Type {Commands.Specifier}warpplate listallowed {{0}} for more."
                });
            }
        }

        private void OnWarpplateResize(CommandArgs args)
        {
            if (!args.Player.HasPermission("warpplates.resize"))
            {
                args.Player.SendErrorMessage("You do not have permission to resize warpplates.");
                return;
            }

            if (args.Parameters.Count != 4)
            {
                args.Player.SendErrorMessage(
                    $"Invalid syntax! Proper syntax: {Commands.Specifier}warpplate resize <warpplate name> <width> <height>");
                return;
            }

            var warpplateName = args.Parameters[1];
            var warpplate = _warpplateManager.Get(warpplateName);
            if (warpplate == null)
            {
                args.Player.SendErrorMessage($"Invalid warpplate '{warpplateName}'.");
            }
            else if (!int.TryParse(args.Parameters[2], out var width) ||
                     !int.TryParse(args.Parameters[3], out var height))
            {
                args.Player.SendErrorMessage("Invalid dimensions.");
            }
            else if (width > _warpplatesConfig.MaxWarpplateWidth || height > _warpplatesConfig.MaxWarpplateHeight)
            {
                args.Player.SendErrorMessage($"The new dimensions are too big. The maximum size is {width}x{height}.");
            }
            else
            {
                warpplate.Area = new Rectangle(warpplate.Area.X, warpplate.Area.Y, width, height);
                _warpplateManager.Update(warpplate);
                args.Player.SendSuccessMessage($"Set warpplate '{warpplateName}' size to {width}x{height}.");
            }
        }

        private void OnWarpplateSet(CommandArgs args)
        {
            if (!args.Player.RealPlayer)
            {
                args.Player.SendErrorMessage("You must use this command in-game.");
                return;
            }

            if (!args.Player.HasPermission("warpplates.set"))
            {
                args.Player.SendErrorMessage("You do not have permission to set warpplates.");
                return;
            }

            if (args.Parameters.Count != 2)
            {
                args.Player.SendErrorMessage(
                    $"Invalid syntax! Proper syntax: {Commands.Specifier}warpplate set <warpplate name>");
                return;
            }

            var warpplateName = args.Parameters[1];
            if (_warpplateManager.Get(warpplateName) != null)
            {
                args.Player.SendErrorMessage($"Warpplate '{warpplateName}' already exists.");
            }
            else if (_warpplateManager.GetWarpplates()
                .Any(w => w.IsWarpplateArea(args.Player.TileX, args.Player.TileY)))
            {
                args.Player.SendErrorMessage(
                    "You are currently located within another warpplate's area. The warpplate cannot be set.");
            }
            else
            {
                var warpplate = new Warpplate(Main.worldID, warpplateName, args.Player.TileX, args.Player.TileY)
                {
                    Area = new Rectangle(args.Player.TileX, args.Player.TileY, 3, 3),
                    Delay = 3
                };
                _warpplateManager.Add(warpplate);
                args.Player.SendSuccessMessage($"Set warpplate '{warpplateName}' at your position.");
            }
        }

        private void OnWarpplateTag(CommandArgs args)
        {
            if (!args.Player.HasPermission("warpplates.settag"))
            {
                args.Player.SendErrorMessage("You do not have permission to set warpplate tags.");
                return;
            }

            if (args.Parameters.Count != 3)
            {
                args.Player.SendErrorMessage(
                    $"Invalid syntax! Proper syntax: {Commands.Specifier}warpplate tag <warpplate name> <tag>");
                return;
            }

            var warpplateName = args.Parameters[1];
            var warpplate = _warpplateManager.Get(warpplateName);
            if (warpplate == null)
            {
                args.Player.SendErrorMessage($"Invalid warpplate '{warpplateName}'.");
            }
            else
            {
                var tag = args.Parameters[2];
                warpplate.Tag = tag;
                _warpplateManager.Update(warpplate);
                args.Player.SendSuccessMessage($"Set the tag for warpplate '{warpplateName}' to '{tag}'.");
            }
        }

        private static void OnWarpplateToggle(CommandArgs args)
        {
            var playerMetadata = args.Player.GetOrCreatePlayerMetadata();
            playerMetadata.IsWarpplateAllow = !playerMetadata.IsWarpplateAllow;
            args.Player.SendSuccessMessage(
                $"You are {(playerMetadata.IsWarpplateAllow ? "now" : "no longer")} affected by warpplates.");
        }

        private void OnWarpplateTogglePublic(CommandArgs args)
        {
            if (!args.Player.HasPermission("warpplates.togglepublic"))
            {
                args.Player.SendErrorMessage("You do not have permission to toggle warpplate statuses.");
                return;
            }

            if (args.Parameters.Count != 2)
            {
                args.Player.SendErrorMessage(
                    $"Invalid syntax! Proper syntax: {Commands.Specifier}warpplate togglepublic <warpplate name>");
                return;
            }

            var warpplateName = args.Parameters[1];
            var warpplate = _warpplateManager.Get(warpplateName);
            if (warpplate == null)
            {
                args.Player.SendErrorMessage($"Invalid warpplate '{warpplateName}'.");
            }
            else
            {
                warpplate.IsPublic = !warpplate.IsPublic;
                _warpplateManager.Update(warpplate);
                args.Player.SendSuccessMessage(
                    $"This warpplate is {(warpplate.IsPublic ? "now" : "no longer")} public.");
            }
        }

        private void WarpplateCommand(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage(
                    $"Invalid syntax! Type {Commands.Specifier}warpplate help for a list of valid commands.");
            }

            var command = args.Parameters[0].ToLowerInvariant();
            switch (command)
            {
                case "allow":
                    OnWarpplateAllow(args);
                    break;
                case "delay":
                    OnWarpplateDelay(args);
                    break;
                case "delete":
                    OnWarpplateDelete(args);
                    break;
                case "destination":
                    OnWarpplateDestination(args);
                    break;
                case "info":
                    OnWarpplateInfo(args);
                    break;
                case "list":
                    OnWarpplateList(args);
                    break;
                case "listallowed":
                    OnWarpplateListAllowed(args);
                    break;
                case "resize":
                    OnWarpplateResize(args);
                    break;
                case "set":
                    OnWarpplateSet(args);
                    break;
                case "tag":
                    OnWarpplateTag(args);
                    break;
                case "toggle":
                    OnWarpplateToggle(args);
                    break;
                case "togglepublic":
                    OnWarpplateTogglePublic(args);
                    break;
            }
        }
    }
}