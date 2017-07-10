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
            Channel.AddPermissionOverwriteAsync(player.User.DiscordUser, new Discord.OverwritePermissions(sendMessages: Discord.PermValue.Deny));
        }

        public void UnbanPlayer(InGamePlayerInfo player)
        {
            if (!MuteOnDeath)
            {
                return;
            }
            Channel.RemovePermissionOverwriteAsync(player.User.DiscordUser);
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
                    if (permissionOverwrite.Permissions.ToDenyList().Count == 1)
                    {
                        var user = Channel.GetUser(permissionOverwrite.TargetId);
                        if (user != null)
                        {
                            Channel.RemovePermissionOverwriteAsync(user);
                        }
                    }
                }
            }
        }
    }
}
