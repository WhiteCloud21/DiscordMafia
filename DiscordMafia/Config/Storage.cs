using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using DiscordMafia.Lib;

namespace DiscordMafia.Config
{
    [Serializable]
    [XmlRoot("Messages")]
    public class Messages: MessageDictionary<Messages, string, string[]>
    {
        private readonly Random _random = new Random();

        private static readonly Dictionary<string, Messages> _instances = new Dictionary<string, Messages>();

        public string get(string key)
        {
            if (ContainsKey(key))
            {
                var messages = this[key];
                return messages[_random.Next(messages.Length)];
            }
            return "";
        }
    }
}
