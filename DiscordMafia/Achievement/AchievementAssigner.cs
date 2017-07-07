namespace DiscordMafia.Achievement
{
    public class AchievementAssigner
    {
        private readonly Game _game;
        private readonly AchievementManager _manager;

        public AchievementAssigner(Game game)
        {
            this._game = game;
            _manager = game.AchievementManager;
        }

        public void afterGame()
        {
            foreach (var player in _game.PlayersList)
            {
                // TODO Уже выглядит плохо, разбить на методы
                if (player.DbUser.TotalPoints >= 10000)
                {
                    _manager.Push(player.User, Achievement.Id10K);
                }
                if (player.DbUser.TotalPoints >= 25000)
                {
                    _manager.Push(player.User, Achievement.Id25K);
                }
                if (player.DbUser.TotalPoints >= 50000)
                {
                    _manager.Push(player.User, Achievement.Id50K);
                }
                if (player.DbUser.TotalPoints >= 100000)
                {
                    _manager.Push(player.User, Achievement.Id100K);
                }
                
                if (player.DbUser.Rate >= 2000)
                {
                    _manager.Push(player.User, Achievement.IdRatingBronze);
                }
                if (player.DbUser.Rate >= 3500)
                {
                    _manager.Push(player.User, Achievement.IdRatingSilver);
                }
                if (player.DbUser.Rate >= 5000)
                {
                    _manager.Push(player.User, Achievement.IdRatingGold);
                }
                if (player.DbUser.Rate >= 10000)
                {
                    _manager.Push(player.User, Achievement.IdRatingChampion);
                }
            }
        }
    }
}
