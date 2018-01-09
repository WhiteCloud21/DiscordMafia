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
            _game = game;
            _manager = game.AchievementManager;
        }

        public void AfterGame(DB.Game gameResult)
        {
            var state = new GameState(this, gameResult);
            foreach (var player in _game.PlayersList)
            {
                CheckPoints(player);
                CheckRate(player);
                CheckGames(player);
                CheckStreaks(player);
                CheckDemoman(player);
                state.CheckAchievements(player);
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
            if (player.CurrentGamePoints <= -30)
            {
                _manager.Push(player.User, Achievement.IdPointsPerGameWasted);
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

        private class GameState
        {
            public readonly bool IsGameWithAuthor = false;
            public readonly bool IsClearVictory = false;

            private readonly AchievementManager _manager;
            private readonly DB.Game _gameResult;

            public GameState(AchievementAssigner assigner, DB.Game gameResult)
            {
                _manager = assigner._manager;
                _gameResult = gameResult;
                var game = assigner._game;
                var winner = _gameResult.Winner;
                IsClearVictory = _gameResult.PlayersCount > 5 && winner != Team.None;
                foreach (var player in game.PlayersList)
                {
                    if (player.User.Id == 137234657623277568)
                    {
                        IsGameWithAuthor = true;
                    }
                    if (!player.IsAlive && player.Role.Team == winner)
                    {
                        IsClearVictory = false;
                    }
                }
            }

            public void CheckAchievements(InGamePlayerInfo player)
            {
                if (IsGameWithAuthor)
                {
                    _manager.Push(player.User, Achievement.IdPlayWithDeveloper);
                }
                if (IsClearVictory && player.Role.Team == _gameResult.Winner)
                {
                    _manager.Push(player.User, Achievement.IdPlayWithDeveloper);
                }
                if (player.Role.Team == _gameResult.Winner && _gameResult.Winner == Team.Neutral)
                {
                    _manager.Push(player.User, Achievement.IdNeutralWin);
                }
            }
        }
    }
}
