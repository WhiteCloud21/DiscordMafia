using DiscordMafia.Config.Lang;
using DiscordMafia.Services;

namespace DiscordMafia.Roles.Places
{
    public static class PlaceExtensions
    {
        public static string GetName(this Place place, ILanguage language)
        {
            return GetLangInfo(place, language)?.Name ?? $"#PLACE_NAME_{place.Id}";
        }
        
        private static PlaceMessages.PlaceMessage GetLangInfo(this Place place, ILanguage language)
        {
            if (language.PlaceMessages.TryGetValue(place.Id, out var result))
            {
                return result;
            }
            return null;
        }
    }
}
