using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Discord.WebSocket;
using DiscordMafia.Client;
using DiscordMafia.Messages;
using DiscordMafia.Roles;
using DiscordMafia.Config.Lang;

namespace DiscordMafia.Config
{
    public class MessageBuilder
    {
        protected Language Language;
        protected DiscordSocketClient Client;
        private IList<InGamePlayerInfo> _playersList;
        protected Dictionary<ulong, IDMChannel> PrivateChannels = new Dictionary<ulong, IDMChannel>();
        protected Dictionary<ulong, ISocketMessageChannel> Channels = new Dictionary<ulong, ISocketMessageChannel>();

        protected string BuiltMessage = "";

        protected static Regex GenderRegex = new Regex(@"\{\s*gender:\s*(.*?)\s*\|\s*(.*?)\s*\}");

        public MessageBuilder(GameSettings settings, DiscordSocketClient client, IList<InGamePlayerInfo> playersList)
        {
            this.Language = settings.Language;
            this.Client = client;
            this._playersList = playersList;
        }

        public static string Encode(string str)
        {
            return System.Net.WebUtility.HtmlEncode(str);
        }

        public static string Markup(string str)
        {
            return str.Replace("<b>", "**").Replace("</b>", "**").Replace("<i>", "_").Replace("</i>", "_").Replace("<u>", "__").Replace("</u>", "__");
        }

        public MessageBuilder PrepareTextReplacePlayer(string key, InGamePlayerInfo player, string fallbackKey = null, IDictionary<string, object> additionalReplaceDictionary = null)
        {
            BuiltMessage += GetTextReplacePlayer(key, player, additionalReplaceDictionary: additionalReplaceDictionary);
            if (String.IsNullOrEmpty(BuiltMessage) && !String.IsNullOrEmpty(fallbackKey))
            {
                BuiltMessage = GetTextReplacePlayer(fallbackKey, player, additionalReplaceDictionary: additionalReplaceDictionary);
            }
            return this;
        }

        public MessageBuilder PrepareText(string key, IDictionary<string, object> replaceDictionary = null)
        {
            BuiltMessage += GetText(key, replaceDictionary);
            return this;
        }

        public MessageBuilder Text(string message, bool encode = true)
        {
            BuiltMessage = encode ? Encode(message) : message;
            return this;
        }

        public string GetTextReplacePlayer(string key, InGamePlayerInfo player, IDictionary<string, object> additionalReplaceDictionary = null)
        {
            return FormatTextReplacePlayer(GetText(key), player, additionalReplaceDictionary);
        }

        public string FormatTextReplacePlayer(string messageTemplate, InGamePlayerInfo player, IDictionary<string, object> additionalReplaceDictionary = null)
        {
            var replaceDictionary = new Dictionary<string, object>
            {
                { "name", FormatName(player) },
                { "nameSimple", Encode(player.GetName()) },
                { "role", FormatRole(player.StartRole?.GetName(Language)) },
                { "role0", FormatRole(player.StartRole?.GetNameCases(Language)[0]) },
                { "role1", FormatRole(player.StartRole?.GetNameCases(Language)[1]) },
                { "role2", FormatRole(player.StartRole?.GetNameCases(Language)[2]) },
                { "role3", FormatRole(player.StartRole?.GetNameCases(Language)[3]) },
                { "role4", FormatRole(player.StartRole?.GetNameCases(Language)[4]) },
                { "role5", FormatRole(player.StartRole?.GetNameCases(Language)[5]) },
            };

            messageTemplate = GenderRegex.Replace(messageTemplate, player.DbUser.Settings.Gender == DB.User.Gender.Male ? "$1" : "$2");

            if (additionalReplaceDictionary != null)
            {
                messageTemplate = Format(messageTemplate, additionalReplaceDictionary);
            }

            return Format(messageTemplate, replaceDictionary);
        }

        public string GetText(string key, IDictionary<string, object> replaceDictionary = null)
        {
            var message = Language.Messages.get(key);
            if (replaceDictionary != null && !string.IsNullOrEmpty(message))
            {
                message = Format(message, replaceDictionary);
            }
            return message;
        }

        public MessageBuilder AddImage(string photoPathOnServer)
        {
            BuiltMessage += " " + Program.Settings.ImageBaseUrl + photoPathOnServer;
            return this;
        }

        public IMessage[] SendPrivate(InGamePlayerInfo player, bool clear = true, bool tts = false)
        {
            return SendPrivate(player.User, clear, tts);
        }

        public IMessage[] SendPrivate(IUser player, bool clear = true, bool tts = false)
        {
            return SendPrivate(new UserWrapper(player), clear, tts);
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
                    var valuesKey = colonIndex > 0 ? key.Substring(0, colonIndex) : key;
                    if (!values.ContainsKey(valuesKey))
                    {
                        return current;
                    }
                    return current.Replace(
                    "{" + key + "}",
                    colonIndex > 0
                        ? string.Format("{0:" + key.Substring(colonIndex + 1) + "}", values[valuesKey])
                        : values[valuesKey]?.ToString() ?? "");
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
            if (!string.IsNullOrEmpty(userMention))
            {
                return $"{userMention}";
            }
            var nameText = Encode(firstName + " " + lastName);
            return $"<b>{nameText}</b>";
        }

        public virtual string FormatRole(string role)
        {
            return "<b>" + Encode(role) + "</b>";
        }

        public class ReplaceDictionary: Dictionary<string, object>
        {

        }
    }
}
