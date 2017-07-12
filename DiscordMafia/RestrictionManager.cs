using System.Collections.Generic;
using DiscordMafia.Roles;
using Discord.WebSocket;
using System;

namespace DiscordMafia
{
    public class RestrictionManager
    {
        protected ISet<InGamePlayerInfo> KilledPlayers = new HashSet<InGamePlayerInfo>();
        protected SocketTextChannel Channel;
        protected bool MuteOnDeath;

        public RestrictionManager(Game game)
        {
            Channel = game.GameChannel;
            MuteOnDeath = game.Settings.MuteOnDeath;
        }

        public void BanPlayer(InGamePlayerInfo player)
        {
            if (!MuteOnDeath)
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
            Channel.AddPermissionOverwriteAsync(player.User.DiscordUser, permissions.Value).Wait();
        }

        public void UnbanPlayer(InGamePlayerInfo player)
        {
            if (!MuteOnDeath)
            {
                return;
            }
            var permissions = Channel.GetPermissionOverwrite(player.User.DiscordUser);
            if (permissions.HasValue && permissions.Value.SendMessages == Discord.PermValue.Deny)
            {
                if (permissions.Value.ToDenyList().Count == 1)
                {
                    Channel.RemovePermissionOverwriteAsync(player.User.DiscordUser);
                }
                else
                {
                    var newPermissions = permissions.Value.Modify(sendMessages: Discord.PermValue.Inherit);
                    Channel.AddPermissionOverwriteAsync(player.User.DiscordUser, newPermissions);
                }
            }
        }

        public void UnbanAll()
        {
            if (!MuteOnDeath)
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
                            Channel.RemovePermissionOverwriteAsync(user);
                        }
                        else
                        {
                            var newPermissions = permissionOverwrite.Permissions.Modify(sendMessages: Discord.PermValue.Inherit);
                            Channel.AddPermissionOverwriteAsync(user, newPermissions);
                        }
                    }
                }
            }
        }
    }
}
