using System;
using System.Threading.Tasks;
using Discord.Commands;
using DiscordMafia.Config;

namespace DiscordMafia.Preconditions
{
    public class RequireAdminAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            MainSettings mainSettings = services.GetService(typeof(MainSettings)) as MainSettings;
            if (mainSettings.AdminId.Contains(context.User.Id))
            {
                return PreconditionResult.FromSuccess();
            }
            else
            {
                return PreconditionResult.FromError("You must be the admin of the bot to run this command.");
            }
        }
    }
}