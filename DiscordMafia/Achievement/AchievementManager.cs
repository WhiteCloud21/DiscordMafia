using DiscordMafia.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using DiscordMafia.DB;
using Microsoft.EntityFrameworkCore;
using static DiscordMafia.Config.MessageBuilder;
using System.Collections.ObjectModel;

namespace DiscordMafia.Achievement
{
    public class AchievementManager
    {
        protected static readonly Dictionary<string, Achievement> AllowedAchievements = new Dictionary<string, Achievement>();

        protected readonly List<Tuple<UserWrapper, Achievement>> Achievements = new List<Tuple<UserWrapper, Achievement>>();

        protected readonly Game Game;

        public AchievementManager(Game game)
        {
            Game = game;
        }

        static AchievementManager()
        {
            AllowedAchievements.Clear();
            var achievementsToRegister = new List<Achievement>
            {
                new Achievement { Id = Achievement.Id10K, Icon = "\U0001F949" },
                new Achievement { Id = Achievement.Id25K, Icon = "\U0001F948" },
                new Achievement { Id = Achievement.Id50K, Icon = "\U0001F947" },
                new Achievement { Id = Achievement.Id100K, Icon = "\U0001F3C6" },
                new Achievement { Id = Achievement.IdPointsPerGame, Icon = "\U0001F4B0" },
                new Achievement { Id = Achievement.IdPointsPerGameWasted, Icon = "\U0001F643" },
                new Achievement { Id = Achievement.IdRatingBronze, Icon = "\U0001F949" },
                new Achievement { Id = Achievement.IdRatingSilver, Icon = "\U0001F948" },
                new Achievement { Id = Achievement.IdRatingGold, Icon = "\U0001F947" },
                new Achievement { Id = Achievement.IdRatingChampion, Icon = "\U0001F3C6" },
                new Achievement { Id = Achievement.IdGamesBronze, Icon = "\U0001F949" },
                new Achievement { Id = Achievement.IdGamesSilver, Icon = "\U0001F948" },
                new Achievement { Id = Achievement.IdGamesGold, Icon = "\U0001F947" },
                new Achievement { Id = Achievement.IdGamesChampion, Icon = "\U0001F3C6" },
                new Achievement { Id = Achievement.IdPerfectWin, Icon = "\U0001F386" },
                new Achievement { Id = Achievement.IdNeutralWin, Icon = "\U0001F43A" },
                new Achievement { Id = Achievement.IdWinStreak, Icon = "\U0001F638" },
                new Achievement { Id = Achievement.IdLoseStreak, Icon = "\U0001F62D" },
                new Achievement { Id = Achievement.IdCivilKillCom, Icon = "\U0000267F" },
                new Achievement { Id = Achievement.IdDocHealCom, Icon = "\U0001F47C" },
                new Achievement { Id = Achievement.IdDocHealMaf, Icon = "\U0001F912" },
                new Achievement { Id = Achievement.IdDemomanMaster, Icon = "\U0001F4A5" },
                new Achievement { Id = Achievement.IdPlayWithDeveloper, Icon = "\U00002601" },
                new Achievement { Id = Achievement.IdWenchBlock, Icon = "\U0001F6D1" },
                new Achievement { Id = Achievement.IdLostInStars, Icon = "\U00002B50" },
            };
            foreach (var achievement in achievementsToRegister)
            {
                AllowedAchievements.Add(achievement.Id, achievement);
            }
        }

        public bool Push(UserWrapper user, string achievementId)
        {
            if (AllowedAchievements.ContainsKey(achievementId))
            {
                Achievements.Add(new Tuple<UserWrapper, Achievement>(user, AllowedAchievements[achievementId]));
                return true;
            }
            return false;
        }

        public bool AddInstantly(UserWrapper user, string achievementId)
        {
            if (AllowedAchievements.ContainsKey(achievementId))
            {
                var achievement = AllowedAchievements[achievementId];
                using (var gameContext = new GameContext())
                {
                    var dbAchievement = gameContext.Achievements
                        .Where(a => a.UserId == user.Id).SingleOrDefault(a => a.AchievementId == achievementId);
                    
                    if (dbAchievement == null)
                    {
                        dbAchievement = new DB.Achievement()
                        {
                            UserId = user.Id,
                            AchievementId = achievementId,
                            AchievedAt = DateTime.Now
                        };
                        gameContext.Achievements.Add(dbAchievement);
                        try
                        {
                            gameContext.SaveChanges();
                            var messageBuilder = Game.MessageBuilder;
                            var achievementText = $"<b>{achievement.Icon} {achievement.GetName(messageBuilder.Language)}</b>";
                            messageBuilder
                                .PrepareTextReplacePlayer("AchievementUnlocked", new InGamePlayerInfo(user, Game), additionalReplaceDictionary: new ReplaceDictionary { ["achievement"] = achievementText })
                                .SendPublic(Game.GameChannel);
                            return true;
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }
                }
            }
            return false;
        }

        public void Apply()
        {
            foreach (var achievementPair in Achievements)
            {
                AddInstantly(achievementPair.Item1, achievementPair.Item2.Id);
            }
            Achievements.Clear();
        }

        public IList<DB.Achievement> GetAchievements(UserWrapper user)
        {
            using (var gameContext = new GameContext())
            {
                return gameContext.Achievements.AsNoTracking().Where(u => u.UserId == user.Id).ToList();
            }
        }

        public string GetAchievementsAsString(UserWrapper user)
        {
            var builder = new System.Text.StringBuilder();
            var achievements = GetAchievements(user);
            foreach (var dbAchievement in achievements)
            {
                var achievement = GetAchievementInfo(dbAchievement.AchievementId);
                if (achievement != null)
                {
                    builder.AppendLine(Game.MessageBuilder.GetTextSimple("UserAchievementsStatRowTemplate", new Dictionary<string, object>
                    {
                        ["icon"] = achievement.Icon,
                        ["name"] = achievement.GetName(Game.MessageBuilder.Language),
                        ["description"] = achievement.GetDescription(Game.MessageBuilder.Language),
                        ["achievedAt"] = dbAchievement.AchievedAt,
                    }));
                }
            }
            return Game.MessageBuilder.GetTextSimple("UserAchievementsStatTemplate", new Dictionary<string, object> {
                ["rows"] = builder.ToString()
            });
        }

        public static Achievement GetAchievementInfo(string achievementId)
        {
            if (AllowedAchievements.TryGetValue(achievementId, out Achievement achievement))
            {
                return achievement;
            }
            return null;
        }

        public static IReadOnlyCollection<Achievement> GetAllowedAchievements()
        {
            return new ReadOnlyCollection<Achievement>(AllowedAchievements.Values.ToList());
        }
    }
}
