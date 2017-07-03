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
        protected Messages Storage;
        protected DiscordSocketClient Client;
        private IList<InGamePlayerInfo> _playersList;
        protected Dictionary<ulong, IDMChannel> PrivateChannels = new Dictionary<ulong, IDMChannel>();
        protected Dictionary<ulong, ISocketMessageChannel> Channels = new Dictionary<ulong, ISocketMessageChannel>();

        protected string BuiltMessage = "";

        public MessageBuilder(GameSettings settings, DiscordSocketClient client, IList<InGamePlayerInfo> playersList)
        {
            this.Storage = settings.Messages;
            this.Client = client;
            this._playersList = playersList;
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
            BuiltMessage += GetTextReplacePlayer(key, player);
            if (String.IsNullOrEmpty(BuiltMessage) && !String.IsNullOrEmpty(fallbackKey))
            {
                BuiltMessage = GetTextReplacePlayer(fallbackKey, player);
            }
            return this;
        }

        public MessageBuilder PrepareText(string key)
        {
            BuiltMessage += GetText(key);
            return this;
        }

        public MessageBuilder Text(string message, bool encode = true)
        {
            BuiltMessage = encode ? Encode(message) : message;
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
                { "role", FormatRole(player.StartRole.Name) },
                { "role0", FormatRole(player.StartRole.NameCases[0]) },
                { "role1", FormatRole(player.StartRole.NameCases[1]) },
                { "role2", FormatRole(player.StartRole.NameCases[2]) },
                { "role3", FormatRole(player.StartRole.NameCases[3]) },
                { "role4", FormatRole(player.StartRole.NameCases[4]) },
                { "role5", FormatRole(player.StartRole.NameCases[5]) },
            };

            return Format(messageTemplate, replaceDictionary);
        }

        public string GetText(string key)
        {
            return Storage.get(key);
        }

        public MessageBuilder AddImage(string photoPathOnServer)
        {
            BuiltMessage +=  " " + Program.Settings.ImageBaseUrl + photoPathOnServer;
            return this;
        }

        public IMessage[] SendPrivate(InGamePlayerInfo player, bool clear = true, bool tts = false)
        {
            return SendPrivate(player.User, clear, tts);
        }

        public IMessage[] SendPrivate(UserWrapper user, bool clear = true, bool tts = false)
        {
            IMessage[] messages = null;
            if (string.IsNullOrEmpty(BuiltMessage))
            {
                return null;
            }
            messages = GetPrivateChannel(user).SplitAndSend(Markup(BuiltMessage), tts);
            if (clear)
            {
                Clear();
            }
            return messages;
        }

        protected IDMChannel GetPrivateChannel(UserWrapper user)
        {
            if (!PrivateChannels.ContainsKey(user.Id))
            {
                PrivateChannels.Add(user.Id, user.GetDmChannel());
            }
            return PrivateChannels[user.Id];
        }

        protected ISocketMessageChannel GetChannel(ulong channelId)
        {
            if (!Channels.ContainsKey(channelId))
            {
                Channels.Add(channelId, Client.GetChannel(channelId) as ISocketMessageChannel);
            }
            return Channels[channelId];
        }
        
        public IMessage[] SendPublic(IMessageChannel channel, bool clear = true, bool tts = false)
        {
            IMessage[] messages = null;
            if (string.IsNullOrEmpty(BuiltMessage))
            {
                return null;
            }
            messages = channel.SplitAndSend(Markup(BuiltMessage), tts);
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
            foreach (var player in _playersList)
            {
                if (player.Role.Team == team)
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
            BuiltMessage = "";
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
            return FormatName(player.User);
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
