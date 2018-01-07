using System;
using System.Xml.Serialization;
using DiscordMafia.Lib;

namespace DiscordMafia.Config.Lang
{
    [Serializable]
    [XmlRoot("Messages")]
    public class PlaceMessages : MessageDictionary<PlaceMessages, string, PlaceMessages.PlaceMessage>
    {
        [Serializable]
        [XmlRoot("Info", Namespace = null)]
        public class PlaceMessage
        {
            public string Name;
        }
    }
}
