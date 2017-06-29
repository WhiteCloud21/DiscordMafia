namespace DiscordMafia.Achievement
{
    public class AchievementAssigner
    {
        private readonly Game game;
        private readonly AchievementManager manager;

        public AchievementAssigner(Game game)
        {
            this.game = game;
            manager = game.achievementManager;
        }

        public void afterGame()
        {
            foreach (var player in game.playersList)
            {
                // TODO Уже выглядит плохо, разбить на методы
                if (player.dbUser.totalPoints >= 10000)
                {
                    manager.push(player.user, Achievement.Id10k);
                }
                if (player.dbUser.totalPoints >= 25000)
                {
                    manager.push(player.user, Achievement.Id25k);
                }
                if (player.dbUser.totalPoints >= 50000)
                {
                    manager.push(player.user, Achievement.Id50k);
                }
                if (player.dbUser.totalPoints >= 100000)
                {
                    manager.push(player.user, Achievement.Id100k);
                }
                
                if (player.dbUser.rate >= 2000)
                {
                    manager.push(player.user, Achievement.IdRatingBronze);
                }
                if (player.dbUser.rate >= 3500)
                {
                    manager.push(player.user, Achievement.IdRatingSilver);
                }
                if (player.dbUser.rate >= 5000)
                {
                    manager.push(player.user, Achievement.IdRatingGold);
                }
                if (player.dbUser.rate >= 10000)
                {
                    manager.push(player.user, Achievement.IdRatingChampion);
                }
            }
        }
    }
}
