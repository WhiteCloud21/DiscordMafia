using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace DiscordMafia.DB
{
    public class User
    {
        public ulong Id;
        public string Username = "";
        public string FirstName = "";
        public string LastName = "";
        public long TotalPoints = 0;
        public int GamesPlayed = 0;
        public int Wins = 0;
        public int Survivals = 0;
        public int Draws = 0;
        public double Rate = 0.0;
        public bool IsRegistered = false;
        public bool IsNewRecord = true;

        public static User FindById(ulong id)
        {
            var user = new User() { Id = id };
            var connection = Program.Connection;
            var command = connection.CreateCommand();
            command.CommandText = GetSelect() + "WHERE id = :id";
            command.Parameters.AddWithValue(":id", id);
            var reader = command.ExecuteReader();
            user.PopulateRecord(reader);
            return user;
        }

        public static IEnumerable<User> FindAllByCondition(string condition, SqliteParameter[] parameters)
        {
            var connection = Program.Connection;
            var command = connection.CreateCommand();
            var users = new List<User>();
            command.CommandText = GetSelect() + condition;
            command.Parameters.AddRange(parameters);
            var reader = command.ExecuteReader();
            do
            {
                var user = new User();
                user.PopulateRecord(reader);
                if (user.Id == 0)
                {
                    break;
                }
                users.Add(user);

            } while (true);
            return users;
        }

        public static IEnumerable<User> FindAllByCondition(string condition)
        {
            return FindAllByCondition(condition, new SqliteParameter[0]);
        }

        protected User PopulateRecord(DbDataReader reader)
        {
            if (reader.Read())
            {
                Id = ulong.Parse(reader.GetValue(0).ToString());
                Username = reader.GetString(1);
                FirstName = reader.GetString(2);
                LastName = reader.GetString(3);
                TotalPoints = reader.GetInt64(4);
                GamesPlayed = reader.GetInt32(5);
                Wins = reader.GetInt32(6);
                Survivals = reader.GetInt32(7);
                Draws = reader.GetInt32(8);
                Rate = reader.GetDouble(9);
                IsRegistered = reader.GetBoolean(10);
                IsNewRecord = false;
            }
            return this;
        }

        protected static string GetSelect()
        {
            return "SELECT id, username, first_name, last_name, total_points, games, wins, survivals, draws, rate, is_registered FROM user ";
        }

        public void RecalculateStats()
        {
            if (GamesPlayed - Wins - Draws == 0)
            {
                Rate = (Wins + Survivals * 0.5) * GamesPlayed * 1.0;
            }
            else
            {
                Rate = 1.0 * (Wins + Survivals * 0.5) * GamesPlayed / (GamesPlayed - Wins - Draws);
            }
        }

        public bool Save()
        {
            var connection = Program.Connection;
            var command = connection.CreateCommand();
            if (IsNewRecord)
            {
                command.CommandText = @"INSERT OR REPLACE INTO user (id, username, first_name, last_name, total_points, games, wins, survivals, draws, rate, is_registered)
                                    VALUES (:id, :uname, :fname, :lname, :tpoints, :games, :wins, :survivals, :draws, :rate, 1)";
            }
            else
            {
                command.CommandText = @"UPDATE user SET id = :id, username = :uname, first_name = :fname, last_name = :lname, total_points = :tpoints,
                                    games = :games, wins = :wins, survivals = :survivals, draws = :draws, rate = :rate, is_registered = 1 WHERE id = :id";
            }
            command.Parameters.AddWithValue(":id", Id);
            command.Parameters.AddWithValue(":uname", Username);
            command.Parameters.AddWithValue(":fname", FirstName);
            command.Parameters.AddWithValue(":lname", LastName);
            command.Parameters.AddWithValue(":tpoints", TotalPoints);
            command.Parameters.AddWithValue(":games", GamesPlayed);
            command.Parameters.AddWithValue(":wins", Wins);
            command.Parameters.AddWithValue(":survivals", Survivals);
            command.Parameters.AddWithValue(":draws", Draws);
            command.Parameters.AddWithValue(":rate", Rate);

            return command.ExecuteNonQuery() > 0;
        }
    }
}
