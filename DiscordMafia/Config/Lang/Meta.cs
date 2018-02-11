using System;

namespace DiscordMafia.Config.Lang
{
    [Serializable]
    public class Meta
    {
        public string FullName { get; set; }
        public string LocalName { get; set; }
        public string BasedOn { get; set; }
    }
}
