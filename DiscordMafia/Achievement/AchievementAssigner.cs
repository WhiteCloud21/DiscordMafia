using DiscordMafia.Roles;
using System.Linq;

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

        public void AfterGame()
        {
            foreach (var player in _game.PlayersList)
            {
                CheckPoints(player);
                CheckRate(player);
                CheckGames(player);
                CheckStreaks(player);
                CheckDemoman(player);
            }
        }

        private void CheckRate(InGamePlayerInfo player)
        {
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

        private void CheckPoints(InGamePlayerInfo player)
        {
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
            if (player.CurrentGamePoints >= 30)
            {
                _manager.Push(player.User, Achievement.IdPointsPerGame);
            }
        }

        private void CheckGames(InGamePlayerInfo player)
        {
            if (player.DbUser.GamesPlayed >= 1)
            {
                _manager.Push(player.User, Achievement.IdGamesBronze);
            }
            if (player.DbUser.GamesPlayed >= 1501)
            {
                _manager.Push(player.User, Achievement.IdGamesSilver);
            }
            if (player.DbUser.GamesPlayed >= 3000)
            {
                _manager.Push(player.User, Achievement.IdGamesGold);
            }
            if (player.DbUser.GamesPlayed >= 5000)
            {
                _manager.Push(player.User, Achievement.IdGamesChampion);
            }
        }

        private void CheckStreaks(InGamePlayerInfo player)
        {
            if (player.DbUser.WinStreak >= 10)
            {
                _manager.Push(player.User, Achievement.IdWinStreak);
            }
            if (player.DbUser.LoseStreak >= 10)
            {
                _manager.Push(player.User, Achievement.IdLoseStreak);
            }
        }

        private void CheckDemoman(InGamePlayerInfo player)
        {
            if (player.StartRole is Demoman demo)
            {
                var playersCountToAchievement = _game.PlayersList.Where(p => p.StartRole.Team != Team.Mafia).Count() / 2;
                if (demo.TotalVictims >= playersCountToAchievement)
                {
                    _manager.Push(player.User, Achievement.IdDemomanMaster);
                }
            }
        }
    }
}
