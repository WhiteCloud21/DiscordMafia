using System;
using System.Threading.Tasks;
using Discord.Commands;
using DiscordMafia.Config;

namespace DiscordMafia.Preconditions
{
    public class RequirePlayerAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            Game game = services.GetService(typeof(Game)) as Game;
            InGamePlayerInfo player;
            if (game.currentPlayers.TryGetValue(context.User.Id, out player))
            {
                if (player.IsAlive)
                {
                    return PreconditionResult.FromSuccess();
                } else {
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