using System.Collections.Generic;

namespace MafiaWeb.ViewModels.Achievement
{
    public class View
    {
        public DiscordMafia.Achievement.Achievement Achievement;
        public IEnumerable<DiscordMafia.DB.Achievement> DbAchievements;
    }
}
