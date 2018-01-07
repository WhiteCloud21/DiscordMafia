using System;
using System.Xml.Serialization;
using DiscordMafia.Lib;

namespace DiscordMafia.Config.Lang
{
    [Serializable]
    [XmlRoot("Messages")]
    public class RoleMessages : MessageDictionary<RoleMessages, string, RoleMessages.RoleMessage>
    {
        [Serializable]
        [XmlRoot("Info", Namespace = null)]
        public class RoleMessage
        {
            public string Name;
            public string[] NameCases;
        }
    }
}
