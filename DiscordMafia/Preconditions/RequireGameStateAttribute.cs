using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using DiscordMafia.Config;

namespace DiscordMafia.Preconditions
{
    public class RequireGameStateAttribute : PreconditionAttribute
    {
        private ISet<GameState> validStates;

        public RequireGameStateAttribute(params GameState[] states)
        {
            validStates = new HashSet<GameState>();
            foreach (var state in states) {
                validStates.Add(state);
            }
        }

        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            return await Task.Run(() =>
            {
                var game = services.GetService(typeof(Game)) as Game;
                if (validStates.Contains(game.CurrentState))
                {
                    return PreconditionResult.FromSuccess();
                }
                else
                {
                    return PreconditionResult.FromError($"Current game state is incorrect for command {command.Name}.");
                }
            });
        }
    }
}