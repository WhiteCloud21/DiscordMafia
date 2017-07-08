using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System;

namespace DiscordMafia.Modules
{
    // TODO Refactor (ChannelExtensions has same code)
    public class BaseModule: ModuleBase
    {
        private const int MaxLength = 1800;

        protected override async Task<IUserMessage> ReplyAsync(string message, bool isTTS = false, Embed embed = null, RequestOptions options = null)
        {
            if (message.Length <= MaxLength)
            {
                return await base.ReplyAsync(message, isTTS, embed, options);
            }
            else
            {
                IUserMessage lastMessage = null;
                var textParts = message.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var totalLength = 0;
                var tempMessage = "";
                foreach (var textPart in textParts)
                {
                    if (textPart.Length + totalLength > MaxLength)
                    {
                        lastMessage = await base.ReplyAsync(tempMessage, isTTS, embed, options);
                        embed = null;
                        totalLength = 0;
                        tempMessage = "";
                    }
                    tempMessage += textPart + Environment.NewLine;
                    totalLength += textPart.Length;
                }
                if (tempMessage != "")
                {
                    lastMessage = await base.ReplyAsync(tempMessage, isTTS, embed, options);
                }
                return lastMessage;
            }
        }
    }
}
