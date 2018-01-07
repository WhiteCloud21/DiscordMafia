using DiscordMafia.Services;
using System.IO;

namespace DiscordMafia.Config.Lang
{
    public class Language: ILanguage
    {
        public Messages Messages { get; private set; }
        public RoleMessages RoleMessages { get; private set; }
        public ItemMessages ItemMessages { get; private set; }
        public PlaceMessages PlaceMessages { get; private set; }

        public void Load(string filePath)
        {
            Messages = Messages.GetInstance(Path.Combine(filePath, "messages.xml"));
            RoleMessages = RoleMessages.GetInstance(Path.Combine(filePath, "roleMessages.xml"));
            ItemMessages = ItemMessages.GetInstance(Path.Combine(filePath, "itemMessages.xml"));
            PlaceMessages = PlaceMessages.GetInstance(Path.Combine(filePath, "placeMessages.xml"));
        }
    }
}
