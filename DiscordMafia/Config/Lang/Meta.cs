using System;
using System.Xml.Serialization;

namespace DiscordMafia.Config.Lang
{
    [Serializable]
    public class Meta
    {
        public string FullName { get; set; }
        public string LocalName { get; set; }
        public string BasedOn { get; set; }
        public bool IsEvent { get; set; }
        public string GameType { get; set; }
        public Message[] WelcomeMessages { get; set; }

        [Serializable]
        [XmlRoot("Message", Namespace = null)]
        public class Message
        {
            public string Content { get; set; }
            public int Pause { get; set; }
        }
    }
}
