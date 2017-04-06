using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMafia.Messages
{
    static class ChannelExtensions
    {
        const int MAX_LENGTH = 1800;
        public static async Task<Message[]> SplitAndSend(this Channel channel, string text, bool tts = false)
        {
            var result = new List<Message>();
            if (text.Length <= MAX_LENGTH)
            {
                result.Add(await channel.Send(text, tts));
            }
            else
            {
                var textParts = text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var totalLength = 0;
                var message = "";
                foreach (var textPart in textParts)
                {
                    if (textPart.Length + totalLength > MAX_LENGTH)
                    {
                        result.Add(await channel.Send(message, tts));
                        totalLength = 0;
                        message = "";
                    }
                    message += textPart + Environment.NewLine;
                    totalLength += textPart.Length;
                }
                if (message != "")
                {
                    result.Add(await channel.Send(message, tts));
                }
            }
            return result.ToArray();
        }

        private async static Task<Message> Send(this Channel channel, string text, bool tts)
        {
            if (tts)
            {
                return await channel.SendMessage(text);
            }
            else
            {
                return await channel.SendTTSMessage(text);
            }
        }
    }
}