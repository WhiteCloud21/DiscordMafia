using System;
using System.Xml.Serialization;
using DiscordMafia.Lib;

namespace DiscordMafia.Config.Lang
{
    [Serializable]
    [XmlRoot("Messages")]
    public class SimpleMessages : MessageDictionary<SimpleMessages, string, string>
    {
        public string GetMessage(string key)
        {
            if (ContainsKey(key))
            {
                return this[key];
            }
            return $"#SIMPLE_MESSAGE_{key}";
        }
    }
}
