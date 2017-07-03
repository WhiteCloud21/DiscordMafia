using DiscordMafia.Client;
using System;
using System.Collections.Generic;

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
                DB.Achievement dbAchievement = DB.Achievement.FindUserAchievement(user.Id, achievementId);
                if (dbAchievement == null)
                {
                    dbAchievement = new DB.Achievement();
                    dbAchievement.UserId = user.Id;
                    dbAchievement.AchievementId = achievementId;
                    dbAchievement.AchievedAt = DateTime.Now;
                    var result = dbAchievement.Save();
                    if (result)
                    {
                        var messageBuilder = Game.messageBuilder;
                        var message = messageBuilder.GetText("AchievementUnlocked");
                        var achievementText = $"<b>{achievement.Icon} {achievement.Name}</b>";
                        var replaces = new Dictionary<string, object> { { "name", messageBuilder.FormatName(user) }, { "achievement", achievementText } };
                        messageBuilder
                            .Text(messageBuilder.Format(message, replaces), false)
                            .SendPublic(Game.gameChannel);
                    }
                    return result;
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
            return DB.Achievement.FindUserAchievements(user.Id);
        }

        public string GetAchievementsAsString(UserWrapper user)
        {
            var builder = new System.Text.StringBuilder();
            var achievements = DB.Achievement.FindUserAchievements(user.Id);
            builder.AppendLine("<b><u>Достижения:</u></b>");
            foreach (var dbAchievement in achievements)
            {
                Achievement achievement;
                if (AllowedAchievements.TryGetValue(dbAchievement.AchievementId, out achievement))
                {
                    builder.AppendLine($"<b>{achievement.Icon} {achievement.Name}</b> ({achievement.Description}) - заработано {dbAchievement.AchievedAt.ToString()}");
                }
            }
            return builder.ToString();
        }
    }
}
