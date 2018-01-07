using DiscordMafia.Config.Lang;

namespace DiscordMafia.Services
{
    public interface ILanguage
    {
        Config.Messages Messages { get; }
        RoleMessages RoleMessages { get; }
        ItemMessages ItemMessages { get; }
        PlaceMessages PlaceMessages { get; }
    }
}
