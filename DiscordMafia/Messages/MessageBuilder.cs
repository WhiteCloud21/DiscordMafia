using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DiscordMafia.Client;
using DiscordMafia.Messages;

namespace DiscordMafia.Config
{
    public class MessageBuilder
    {
        protected Messages storage;
        protected DiscordClient client;
        private IList<InGamePlayerInfo> playersList;
        protected Dictionary<ulong, Channel> privateChannels = new Dictionary<ulong, Channel>();
        protected Dictionary<ulong, Channel> Channels = new Dictionary<ulong, Channel>();

        protected string builtMessage = "";

        public MessageBuilder(GameSettings settings, DiscordClient client, IList<InGamePlayerInfo> playersList)
        {
            this.storage = settings.Messages;
            this.client = client;
            this.playersList = playersList;
        }

        public string Encode(string str)
        {
            return System.Web.HttpUtility.HtmlEncode(str);
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

        public Message[] SendPrivate(InGamePlayerInfo player, bool clear = true, bool tts = false)
        {
            return SendPrivate(player.user.Id, clear, tts);
        }

        public Message[] SendPrivate(ulong userId, bool clear = true, bool tts = false)
        {
            Message[] messages = null;
            if (string.IsNullOrEmpty(builtMessage))
            {
                return null;
            }
            messages = GetPrivateChannel(userId).SplitAndSend(Markup(builtMessage), tts).Result;
            if (clear)
            {
                Clear();
            }
            return messages;
        }

        protected Channel GetPrivateChannel(ulong userId)
        {
            if (!privateChannels.ContainsKey(userId))
            {
                privateChannels.Add(userId, client.CreatePrivateChannel(userId).Result);
            }
            return privateChannels[userId];
        }

        protected Channel GetChannel(ulong channelId)
        {
            if (!Channels.ContainsKey(channelId))
            {
                Channels.Add(channelId, client.GetChannel(channelId));
            }
            return Channels[channelId];
        }
        
        public Message[] SendPublic(Channel channel, bool clear = true, bool tts = false)
        {
            Message[] messages = null;
            if (string.IsNullOrEmpty(builtMessage))
            {
                return null;
            }
            messages = channel.SplitAndSend(Markup(builtMessage), tts).Result;
            if (clear)
            {
                Clear();
            }
            return messages;
        }

        public Message[] SendPublic(ulong chatId, bool clear = true, bool tts = false)
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
