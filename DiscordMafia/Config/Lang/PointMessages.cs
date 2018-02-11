using System;
using System.Xml.Serialization;
using DiscordMafia.Lib;

namespace DiscordMafia.Config.Lang
{
    [Serializable]
    [XmlRoot("Messages")]
    public class PointMessages : MessageDictionary<PointMessages, string, PointMessages.PointMessage>
    {
        [Serializable]
        [XmlRoot("Info", Namespace = null)]
        public class PointMessage
        {
            public string Description;
        }

        public string GetDescription(string key)
        {
            if (ContainsKey(key))
            {
                return this[key].Description;
            }
            return $"#POINTS_DESC_{key}";
        }
    }
}
