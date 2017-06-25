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
            }
        }
    }
}
