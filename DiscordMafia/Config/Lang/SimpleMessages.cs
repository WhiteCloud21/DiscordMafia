using System;
using System.Xml.Serialization;
using DiscordMafia.Lib;

namespace DiscordMafia.Config.Lang
{
    [Serializable]
    [XmlRoot("Messages")]
    public class SimpleMessages : MessageDictionary<SimpleMessages, string, string>
    {
    }
}
