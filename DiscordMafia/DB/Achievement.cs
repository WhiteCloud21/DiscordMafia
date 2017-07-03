using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace DiscordMafia.DB
{
    public class Achievement
    {
        public long Id;
        public ulong UserId;
        public string AchievementId;
        public DateTime AchievedAt;
        public bool IsNewRecord = true;

        public static Achievement FindUserAchievement(ulong userId, string achievementId)
        {
            var connection = Program.Connection;
            var command = connection.CreateCommand();
            command.CommandText = GetSelect() + "WHERE user_id = :userId AND achievement_id = :achievementId";
            command.Parameters.AddWithValue(":userId", userId);
            command.Parameters.AddWithValue(":achievementId", achievementId);
            var reader = command.ExecuteReader();
            var achievement = new Achievement();
            achievement.PopulateRecord(reader);
            return !achievement.IsNewRecord ? achievement : null;
        }

        public static IList<Achievement> FindUserAchievements(ulong userId)
        {
            return FindAllByCondition("WHERE user_id = :userId", new SqliteParameter[] { new SqliteParameter(":userId", userId)});
        }

        public static IList<Achievement> FindAllByCondition(string condition, SqliteParameter[] parameters)
        {
            var connection = Program.Connection;
            var command = connection.CreateCommand();
            var achievements = new List<Achievement>();
            command.CommandText = GetSelect() + condition;
            command.Parameters.AddRange(parameters);
            var reader = command.ExecuteReader();
            do
            {
                var achievement = new Achievement();
                achievement.PopulateRecord(reader);
                if (achievement.IsNewRecord)
                {
                    break;
                }
                achievements.Add(achievement);

            } while (true);
            return achievements;
        }

        public static IList<Achievement> FindAllByCondition(string condition)
        {
            return FindAllByCondition(condition, new SqliteParameter[0]);
        }

        protected Achievement PopulateRecord(DbDataReader reader)
        {
            if (reader.Read())
            {
                Id = reader.GetInt64(0);
                UserId = ulong.Parse(reader.GetValue(1).ToString());
                AchievementId = reader.GetString(2);
                AchievedAt = DateTime.FromBinary(reader.GetInt64(3));
                IsNewRecord = false;
            }
            return this;
        }

        protected static string GetSelect()
        {
            return "SELECT id, user_id, achievement_id, achieved_at FROM user_achievement ";
        }

        public bool Save()
        {
            var connection = Program.Connection;
            var command = connection.CreateCommand();
            if (IsNewRecord)
            {
                command.CommandText = @"INSERT OR REPLACE INTO user_achievement (user_id, achievement_id, achieved_at)
                                    VALUES (:userId, :achievementId, :achievedAt)";
                command.Parameters.AddWithValue(":userId", UserId);
                command.Parameters.AddWithValue(":achievementId", AchievementId);
                command.Parameters.AddWithValue(":achievedAt", AchievedAt.ToBinary());

                return command.ExecuteNonQuery() > 0;
            }
            return false;
        }
    }
}
