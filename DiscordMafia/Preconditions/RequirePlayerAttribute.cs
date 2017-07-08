using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using DiscordMafia.Config;

namespace DiscordMafia.Preconditions
{
    public class RequirePlayerAttribute : PreconditionAttribute
    {
        private ISet<Type> validRoles;

        public RequirePlayerAttribute()
        {
            validRoles = null;
        }

        public RequirePlayerAttribute(params Type[] roles)
        {
            validRoles = new HashSet<Type>();
            foreach (var role in roles)
            {
                validRoles.Add(role);
            }
        }

        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            Game game = services.GetService(typeof(Game)) as Game;
            InGamePlayerInfo player;
            if (game.CurrentPlayers.TryGetValue(context.User.Id, out player))
            {
                if (player.IsAlive)
                {
                    if (validRoles == null || validRoles.Contains(player.Role.GetType()))
                    {
                        return PreconditionResult.FromSuccess();
                    } else {
                        return PreconditionResult.FromError($"Command {command.Name} is not available for your role.");
                    }
                }
                else
                {
                    return PreconditionResult.FromError("You are dead now.");
                }
            }
            else
            {
                return PreconditionResult.FromError("You are not participating in current game.");
            }
        }
    }
}