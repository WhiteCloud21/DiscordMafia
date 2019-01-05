using System.Collections.Generic;
using Discord.WebSocket;

namespace DiscordMafia
{
    public class RestrictionManager
    {
        protected ISet<InGamePlayerInfo> MutedPlayers = new HashSet<InGamePlayerInfo>();
        protected Game game;

        public RestrictionManager(Game game)
        {
            this.game = game;
        }

        public void BanPlayer(InGamePlayerInfo player)
        {
            if (!game.Settings.MuteOnDeath && !game.Settings.UseMuteBlacklist)
            {
                return;
            }
            if (game.Settings.UseMuteBlacklist && player.DbUser.IsMuteEnabled != true)
            {
                return;
            }
            var permissions = game.GameChannel.GetPermissionOverwrite(player.User.DiscordUser);
            if (permissions.HasValue)
            {
                permissions = permissions.Value.Modify(sendMessages: Discord.PermValue.Deny);
            }
            else
            {
                permissions = new Discord.OverwritePermissions(sendMessages: Discord.PermValue.Deny);
            }
            MutedPlayers.Add(player);
            game.GameChannel.AddPermissionOverwriteAsync(player.User.DiscordUser, permissions.Value).Wait();
        }

        public void UnbanPlayer(InGamePlayerInfo player)
        {
            if (!game.Settings.MuteOnDeath && !game.Settings.UseMuteBlacklist)
            {
                return;
            }
            if (game.Settings.UseMuteBlacklist && !MutedPlayers.Contains(player))
            {
                return;
            }
            var permissions = game.GameChannel.GetPermissionOverwrite(player.User.DiscordUser);
            if (permissions.HasValue && permissions.Value.SendMessages == Discord.PermValue.Deny)
            {
                if (permissions.Value.ToDenyList().Count == 1)
                {
                    game.GameChannel.RemovePermissionOverwriteAsync(player.User.DiscordUser).Wait();
                }
                else
                {
                    var newPermissions = permissions.Value.Modify(sendMessages: Discord.PermValue.Inherit);
                    game.GameChannel.AddPermissionOverwriteAsync(player.User.DiscordUser, newPermissions).Wait();
                }
            }
        }

        public void UnbanAll()
        {
            if (!game.Settings.MuteOnDeath && !game.Settings.UseMuteBlacklist)
            {
                return;
            }
            if (game.Settings.UseMuteBlacklist && MutedPlayers.Count == 0)
            {
                return;
            }
            foreach (var permissionOverwrite in game.GameChannel.PermissionOverwrites)
            {
                if (permissionOverwrite.TargetType == Discord.PermissionTarget.User && permissionOverwrite.Permissions.SendMessages == Discord.PermValue.Deny)
                {
                    var user = game.GameChannel.GetUser(permissionOverwrite.TargetId);
                    if (user != null)
                    {
                        if (permissionOverwrite.Permissions.ToDenyList().Count == 1)
                        {
                            game.GameChannel.RemovePermissionOverwriteAsync(user).Wait();
                        }
                        else
                        {
                            var newPermissions = permissionOverwrite.Permissions.Modify(sendMessages: Discord.PermValue.Inherit);
                            game.GameChannel.AddPermissionOverwriteAsync(user, newPermissions).Wait();
                        }
                    }
                }
            }
            MutedPlayers.Clear();
        }
    }
}
