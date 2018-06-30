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
        public class RoleMessage: IRoleMessage
        {
            public string Name { get; set; }
            public string[] NameCases { get; set; }
            public string ImagePath { get; set; }
            public Variant[] Variants { get; set; }
        }


        [Serializable]
        [XmlRoot("Variant", Namespace = null)]
        public class Variant: IRoleMessage
        {
            public string Name { get; set; }
            public string[] NameCases { get; set; }
            public string ImagePath { get; set; }
        }
        
        public interface IRoleMessage
        {
            string Name { get; }
            string[] NameCases { get; }
            string ImagePath { get; }
        }
    }
}
