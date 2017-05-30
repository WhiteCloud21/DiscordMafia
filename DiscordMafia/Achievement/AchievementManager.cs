using DiscordMafia.Client;
using System;
using System.Collections.Generic;

namespace DiscordMafia.Achievement
{
    public class AchievementManager
    {
        protected readonly Dictionary<string, Achievement> allowedAchievements = new Dictionary<string, Achievement>();

        protected readonly List<Tuple<UserWrapper, Achievement>> achievements = new List<Tuple<UserWrapper, Achievement>>();

        protected readonly Game game;

        public AchievementManager(Game game)
        {
            this.game = game;

            var achievementsToRegister = new List<Achievement>
            {
                new Achievement { Id = "test", Name = "Тест" }
            };
            foreach (var achievement in achievementsToRegister)
            {
                allowedAchievements.Add(achievement.Id, achievement);
            }
        }

        public bool push(UserWrapper user, string achievementId)
        {
            if (allowedAchievements.ContainsKey(achievementId))
            {
                achievements.Add(new Tuple<UserWrapper, Achievement>(user, allowedAchievements[achievementId]));
                return true;
            }
            return false;
        }

        public bool addInstantly(UserWrapper user, string achievementId)
        {
            if (allowedAchievements.ContainsKey(achievementId))
            {
                var achievement = allowedAchievements[achievementId];
                DB.Achievement dbAchievement = DB.Achievement.findUserAchievement(user.Id, achievementId);
                if (achievement == null)
                {
                    dbAchievement = new DB.Achievement();
                    dbAchievement.userId = user.Id;
                    dbAchievement.achievementId = achievementId;
                    dbAchievement.achievedAt = DateTime.Now;
                    var result = dbAchievement.Save();
                    if (result)
                    {
                        var messageBuilder = game.messageBuilder;
                        var message = messageBuilder.GetText("AchievementUnlocked");
                        var achievementText = "<b>" + achievement.Name + "</b>";
                        var replaces = new Dictionary<string, object> { { "name", messageBuilder.FormatName(user) }, { "achievement", achievementText } };
                        messageBuilder
                            .Text(messageBuilder.Format(message, replaces), false)
                            .SendPublic(game.gameChannel);
                    }
                    return result;
                }
            }
            return false;
        }

        public void apply()
        {
            foreach (var achievementPair in achievements)
            {
                addInstantly(achievementPair.Item1, achievementPair.Item2.Id);
            }
            achievements.Clear();
        }
    }
}
