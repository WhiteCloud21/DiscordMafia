using System.Collections.Generic;
using System.Data.Common;
using Mono.Data.Sqlite;

namespace DiscordMafia.DB
{
    public class User
    {
        public ulong id;
        public string username = "";
        public string firstName = "";
        public string lastName = "";
        public long totalPoints = 0;
        public int gamesPlayed = 0;
        public int wins = 0;
        public int survivals = 0;
        public int draws = 0;
        public double rate = 0.0;
        public bool isRegistered = false;
        public bool isNewRecord = true;

        public static User findById(ulong id)
        {
            var user = new User() { id = id };
            var connection = Program.connection;
            var command = connection.CreateCommand();
            command.CommandText = getSelect() + "WHERE id = :id";
            command.Parameters.AddWithValue(":id", id);
            var reader = command.ExecuteReader();
            user.populateRecord(reader);
            return user;
        }

        public static IEnumerable<User> findAllByCondition(string condition, SqliteParameter[] parameters)
        {
            var connection = Program.connection;
            var command = connection.CreateCommand();
            var users = new List<User>();
            command.CommandText = getSelect() + condition;
            command.Parameters.AddRange(parameters);
            var reader = command.ExecuteReader();
            do
            {
                var user = new User();
                user.populateRecord(reader);
                if (user.id == 0)
                {
                    break;
                }
                users.Add(user);

            } while (true);
            return users;
        }

        public static IEnumerable<User> findAllByCondition(string condition)
        {
            return findAllByCondition(condition, new SqliteParameter[0]);
        }

        protected User populateRecord(DbDataReader reader)
        {
            if (reader.Read())
            {
                id = ulong.Parse(reader.GetValue(0).ToString());
                username = reader.GetString(1);
                firstName = reader.GetString(2);
                lastName = reader.GetString(3);
                totalPoints = reader.GetInt64(4);
                gamesPlayed = reader.GetInt32(5);
                wins = reader.GetInt32(6);
                survivals = reader.GetInt32(7);
                draws = reader.GetInt32(8);
                rate = reader.GetDouble(9);
                isRegistered = reader.GetBoolean(10);
                isNewRecord = false;
            }
            return this;
        }

        protected static string getSelect()
        {
            return "SELECT id, username, first_name, last_name, total_points, games, wins, survivals, draws, rate, is_registered FROM user ";
        }

        public void RecalculateStats()
        {
            if (gamesPlayed - wins - draws == 0)
            {
                rate = (wins + survivals * 0.5) * gamesPlayed * 1.0;
            }
            else
            {
                rate = 1.0 * (wins + survivals * 0.5) * gamesPlayed / (gamesPlayed - wins - draws);
            }
        }

        public bool Save()
        {
            var connection = Program.connection;
            var command = connection.CreateCommand();
            if (isNewRecord)
            {
                command.CommandText = @"INSERT OR REPLACE INTO user (id, username, first_name, last_name, total_points, games, wins, survivals, draws, rate, is_registered)
                                    VALUES (:id, :uname, :fname, :lname, :tpoints, :games, :wins, :survivals, :draws, :rate, 1)";
            }
            else
            {
                command.CommandText = @"UPDATE user SET id = :id, username = :uname, first_name = :fname, last_name = :lname, total_points = :tpoints,
                                    games = :games, wins = :wins, survivals = :survivals, draws = :draws, rate = :rate, is_registered = 1 WHERE id = :id";
            }
            command.Parameters.AddWithValue(":id", id);
            command.Parameters.AddWithValue(":uname", username);
            command.Parameters.AddWithValue(":fname", firstName);
            command.Parameters.AddWithValue(":lname", lastName);
            command.Parameters.AddWithValue(":tpoints", totalPoints);
            command.Parameters.AddWithValue(":games", gamesPlayed);
            command.Parameters.AddWithValue(":wins", wins);
            command.Parameters.AddWithValue(":survivals", survivals);
            command.Parameters.AddWithValue(":draws", draws);
            command.Parameters.AddWithValue(":rate", rate);

            return command.ExecuteNonQuery() > 0;
        }
    }
}
