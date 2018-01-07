using System;
using System.Xml.Serialization;
using DiscordMafia.Lib;

namespace DiscordMafia.Config.Lang
{
    [Serializable]
    [XmlRoot("Messages")]
    public class AchievementMessages : MessageDictionary<AchievementMessages, string, AchievementMessages.AchievementMessage>
    {
        [Serializable]
        [XmlRoot("Info", Namespace = null)]
        public class AchievementMessage
        {
            public string Name;
            public string Description;
        }
    }
}
