using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Discord.WebSocket;
using DiscordMafia.Client;
using DiscordMafia.Messages;

namespace DiscordMafia.Config
{
    public class MessageBuilder
    {
        protected Messages storage;
        protected DiscordSocketClient client;
        private IList<InGamePlayerInfo> playersList;
        protected Dictionary<ulong, IDMChannel> privateChannels = new Dictionary<ulong, IDMChannel>();
        protected Dictionary<ulong, ISocketMessageChannel> Channels = new Dictionary<ulong, ISocketMessageChannel>();

        protected string builtMessage = "";

        public MessageBuilder(GameSettings settings, DiscordSocketClient client, IList<InGamePlayerInfo> playersList)
        {
            this.storage = settings.Messages;
            this.client = client;
            this.playersList = playersList;
        }

        public string Encode(string str)
        {
            return System.Net.WebUtility.HtmlEncode(str);
        }

        public string Markup(string str)
        {
            return str.Replace("<b>", "**").Replace("</b>", "**").Replace("<i>", "_").Replace("</i>", "_").Replace("<u>", "__").Replace("</u>", "__");
        }

        public MessageBuilder PrepareTextReplacePlayer(string key, InGamePlayerInfo player, string fallbackKey = null)
        {
            builtMessage += GetTextReplacePlayer(key, player);
            if (String.IsNullOrEmpty(builtMessage) && !String.IsNullOrEmpty(fallbackKey))
            {
                builtMessage = GetTextReplacePlayer(fallbackKey, player);
            }
            return this;
        }

        public MessageBuilder PrepareText(string key)
        {
            builtMessage += GetText(key);
            return this;
        }

        public MessageBuilder Text(string message, bool encode = true)
        {
            builtMessage = encode ? Encode(message) : message;
            return this;
        }

        public string GetTextReplacePlayer(string key, InGamePlayerInfo player)
        {
            return FormatTextReplacePlayer(GetText(key), player);
        }

        public string FormatTextReplacePlayer(string messageTemplate, InGamePlayerInfo player)
        {
            var replaceDictionary = new Dictionary<string, object>
            {
                { "name", FormatName(player) },
                { "role", FormatRole(player.startRole.Name) },
                { "role0", FormatRole(player.startRole.NameCases[0]) },
                { "role1", FormatRole(player.startRole.NameCases[1]) },
                { "role2", FormatRole(player.startRole.NameCases[2]) },
                { "role3", FormatRole(player.startRole.NameCases[3]) },
                { "role4", FormatRole(player.startRole.NameCases[4]) },
                { "role5", FormatRole(player.startRole.NameCases[5]) },
            };

            return Format(messageTemplate, replaceDictionary);
        }

        public string GetText(string key)
        {
            return storage.get(key);
        }

        public MessageBuilder AddImage(string photoPathOnServer)
        {
            builtMessage +=  " " + Program.Settings.ImageBaseUrl + photoPathOnServer;
            return this;
        }

        public IMessage[] SendPrivate(InGamePlayerInfo player, bool clear = true, bool tts = false)
        {
            return SendPrivate(player.user, clear, tts);
        }

        public IMessage[] SendPrivate(UserWrapper user, bool clear = true, bool tts = false)
        {
            IMessage[] messages = null;
            if (string.IsNullOrEmpty(builtMessage))
            {
                return null;
            }
            messages = GetPrivateChannel(user).SplitAndSend(Markup(builtMessage), tts);
            if (clear)
            {
                Clear();
            }
            return messages;
        }

        protected IDMChannel GetPrivateChannel(UserWrapper user)
        {
            if (!privateChannels.ContainsKey(user.Id))
            {
                privateChannels.Add(user.Id, user.GetDmChannel());
            }
            return privateChannels[user.Id];
        }

        protected ISocketMessageChannel GetChannel(ulong channelId)
        {
            if (!Channels.ContainsKey(channelId))
            {
                Channels.Add(channelId, client.GetChannel(channelId) as ISocketMessageChannel);
            }
            return Channels[channelId];
        }
        
        public IMessage[] SendPublic(IMessageChannel channel, bool clear = true, bool tts = false)
        {
            IMessage[] messages = null;
            if (string.IsNullOrEmpty(builtMessage))
            {
                return null;
            }
            messages = channel.SplitAndSend(Markup(builtMessage), tts);
            if (clear)
            {
                Clear();
            }
            return messages;
        }

        public IMessage[] SendPublic(ulong chatId, bool clear = true, bool tts = false)
        {
            return SendPublic(GetChannel(chatId), clear, tts);
        }

        public void SendToTeam(DiscordMafia.Roles.Team team, bool clear = true)
        {
            foreach (var player in playersList)
            {
                if (player.role.Team == team)
                {
                    SendPrivate(player, clear: false);
                }
            }
            if (clear)
            {
                Clear();
            }
        }

        public virtual void Clear()
        {
            builtMessage = "";
        }

        public string Format(string format, IDictionary<string, object> values)
        {
            var matches = Regex.Matches(format, @"\{(.+?)\}");
            List<string> words = (from Match match in matches select match.Groups[1].Value).ToList();

            return words.Aggregate(
                format,
                (current, key) =>
                {
                    int colonIndex = key.IndexOf(':');
                    return current.Replace(
                    "{" + key + "}",
                    colonIndex > 0
                        ? string.Format("{0:" + key.Substring(colonIndex + 1) + "}", values[key.Substring(0, colonIndex)])
                        : values[key].ToString());
                });
        }

        public string FormatName(InGamePlayerInfo player)
        {
            return FormatName(player.user);
        }

        public string FormatName(UserWrapper user)
        {
            return FormatName(user.FirstName, user.LastName, user.UsernameMention);
        }

        public string FormatName(string firstName, string lastName, string userMention)
        {
            var nameText = Encode(firstName + " " + lastName);
            if (!string.IsNullOrEmpty(userMention))
            {
                return $"{userMention}";
            }
            return $"<b>{nameText}</b>";
        }

        public virtual string FormatRole(string role)
        {
            return "<b>" + Encode(role) + "</b>";
        }
    }
}
