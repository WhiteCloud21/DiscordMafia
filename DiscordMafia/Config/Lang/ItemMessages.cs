using System;
using System.Xml.Serialization;
using DiscordMafia.Lib;

namespace DiscordMafia.Config.Lang
{
    [Serializable]
    [XmlRoot("Messages")]
    public class ItemMessages : MessageDictionary<ItemMessages, string, ItemMessages.ItemMessage>
    {
        [Serializable]
        [XmlRoot("Info", Namespace = null)]
        public class ItemMessage
        {
            public string Name;
            public string[] NameCases;
            public string Description;
        }
    }
}
