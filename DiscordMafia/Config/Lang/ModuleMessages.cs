using System;
using System.Xml.Serialization;
using DiscordMafia.Lib;

namespace DiscordMafia.Config.Lang
{
    [Serializable]
    [XmlRoot("Modules")]
    public class ModuleMessages : MessageDictionary<ModuleMessages, string, ModuleMessages.ModuleInfo>
    {
        [Serializable]
        [XmlRoot("Info", Namespace = null)]
        public class ModuleInfo: ISummarized
        {
            public string[] Aliases { get; set; }
            public string Summary { get; set; }
            public CommandMessages Commands { get; set; }
        }
    }

    [Serializable]
    [XmlRoot("Commands")]
    public class CommandMessages : MessageDictionary<CommandMessages, string, CommandMessages.CommandInfo>
    {
        [Serializable]
        [XmlRoot("Info", Namespace = null)]
        public class CommandInfo: ISummarized
        {
            public string[] Aliases { get; set; }
            public string Summary { get; set; }
            public ParameterMessages Parameters { get; set; }
        }
    }

    [Serializable]
    [XmlRoot("Parameters")]
    public class ParameterMessages : MessageDictionary<ParameterMessages, string, ParameterMessages.ParameterInfo>
    {
        [Serializable]
        [XmlRoot("Info", Namespace = null)]
        public class ParameterInfo: ISummarized
        {
            public string Summary { get; set; }
            public string[] Aliases { get; set; }
        }
    }

    public interface ISummarized
    {
        string Summary { get; set; }
        string[] Aliases { get; set; }
    }
}
