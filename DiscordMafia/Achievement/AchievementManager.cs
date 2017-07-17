using DiscordMafia.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using DiscordMafia.DB;
using Microsoft.EntityFrameworkCore;
using static DiscordMafia.Config.MessageBuilder;

namespace DiscordMafia.Achievement
{
    public class AchievementManager
    {
        protected readonly Dictionary<string, Achievement> AllowedAchievements = new Dictionary<string, Achievement>();

        protected readonly List<Tuple<UserWrapper, Achievement>> Achievements = new List<Tuple<UserWrapper, Achievement>>();

        protected readonly Game Game;

        public AchievementManager(Game game)
        {
            Game = game;

            var achievementsToRegister = new List<Achievement>
            {
                new Achievement { Id = Achievement.Id10K, Icon = "\U0001F949", Name = "Первая зарплата", Description = "Набрать 10000 очков" },
                new Achievement { Id = Achievement.Id25K, Icon = "\U0001F948", Name = "Толстосум", Description = "Набрать 25000 очков" },
                new Achievement { Id = Achievement.Id50K, Icon = "\U0001F947", Name = "Мешок золота", Description = "Набрать 50000 очков" },
                new Achievement { Id = Achievement.Id100K, Icon = "\U0001F3C6", Name = "Финансовый воротила", Description = "Набрать 100000 очков" },
                new Achievement { Id = Achievement.IdRatingBronze, Icon = "\U0001F949", Name = "Первые шаги", Description = "Набрать 2000 рейтинга" },
                new Achievement { Id = Achievement.IdRatingSilver, Icon = "\U0001F948", Name = "Знаток правил", Description = "Набрать 3500 рейтинга" },
                new Achievement { Id = Achievement.IdRatingGold, Icon = "\U0001F947", Name = "Профи", Description = "Набрать 5000 рейтинга" },
                new Achievement { Id = Achievement.IdRatingChampion, Icon = "\U0001F3C6", Name = "Безусловный чемпион", Description = "Набрать 10000 рейтинга" },
                new Achievement { Id = Achievement.IdCivilKillCom, Icon = "\U0000267F", Name = "Одно убийство — одно разочарование", Description = "Убить комиссара, играя за шерифа или горца" },
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
                            var achievementText = $"<b>{achievement.Icon} {achievement.Name}</b>";
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
            builder.AppendLine("<b><u>Достижения:</u></b>");
            foreach (var dbAchievement in achievements)
            {
                if (AllowedAchievements.TryGetValue(dbAchievement.AchievementId, out Achievement achievement))
                {
                    builder.AppendLine($"<b>{achievement.Icon} {achievement.Name}</b> ({achievement.Description}) - заработано {dbAchievement.AchievedAt.ToString()}");
                }
            }
            return builder.ToString();
        }
    }
}
