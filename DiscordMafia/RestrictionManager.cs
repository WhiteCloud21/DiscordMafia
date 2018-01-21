using System.Collections.Generic;
using DiscordMafia.Roles;
using Discord.WebSocket;
using System;

namespace DiscordMafia
{
    public class RestrictionManager
    {
        protected ISet<InGamePlayerInfo> MutedPlayers = new HashSet<InGamePlayerInfo>();
        protected SocketTextChannel Channel;
        protected bool MuteOnDeath;
        protected bool UseMuteBlacklist;

        public RestrictionManager(Game game)
        {
            Channel = game.GameChannel;
            MuteOnDeath = game.Settings.MuteOnDeath;
            UseMuteBlacklist = game.Settings.UseMuteBlacklist;
        }

        public void BanPlayer(InGamePlayerInfo player)
        {
            if (!MuteOnDeath && !UseMuteBlacklist)
            {
                return;
            }
            if (UseMuteBlacklist && player.DbUser.IsMuteEnabled != true)
            {
                return;
            }
            var permissions = Channel.GetPermissionOverwrite(player.User.DiscordUser);
            if (permissions.HasValue)
            {
                permissions = permissions.Value.Modify(sendMessages: Discord.PermValue.Deny);
            }
            else
            {
                permissions = new Discord.OverwritePermissions(sendMessages: Discord.PermValue.Deny);
            }
            MutedPlayers.Add(player);
            Channel.AddPermissionOverwriteAsync(player.User.DiscordUser, permissions.Value).Wait();
        }

        public void UnbanPlayer(InGamePlayerInfo player)
        {
            if (!MuteOnDeath && !UseMuteBlacklist)
            {
                return;
            }
            if (UseMuteBlacklist && !MutedPlayers.Contains(player))
            {
                return;
            }
            var permissions = Channel.GetPermissionOverwrite(player.User.DiscordUser);
            if (permissions.HasValue && permissions.Value.SendMessages == Discord.PermValue.Deny)
            {
                if (permissions.Value.ToDenyList().Count == 1)
                {
                    Channel.RemovePermissionOverwriteAsync(player.User.DiscordUser).Wait();
                }
                else
                {
                    var newPermissions = permissions.Value.Modify(sendMessages: Discord.PermValue.Inherit);
                    Channel.AddPermissionOverwriteAsync(player.User.DiscordUser, newPermissions).Wait();
                }
            }
        }

        public void UnbanAll()
        {
            if (!MuteOnDeath && !UseMuteBlacklist)
            {
                return;
            }
            if (UseMuteBlacklist && MutedPlayers.Count == 0)
            {
                return;
            }
            foreach (var permissionOverwrite in Channel.PermissionOverwrites)
            {
                if (permissionOverwrite.TargetType == Discord.PermissionTarget.User && permissionOverwrite.Permissions.SendMessages == Discord.PermValue.Deny)
                {
                    var user = Channel.GetUser(permissionOverwrite.TargetId);
                    if (user != null)
                    {
                        if (permissionOverwrite.Permissions.ToDenyList().Count == 1)
                        {
                            Channel.RemovePermissionOverwriteAsync(user).Wait();
                        }
                        else
                        {
                            var newPermissions = permissionOverwrite.Permissions.Modify(sendMessages: Discord.PermValue.Inherit);
                            Channel.AddPermissionOverwriteAsync(user, newPermissions).Wait();
                        }
                    }
                }
            }
            MutedPlayers.Clear();
        }
    }
}
