using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using System.Reflection;

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
        public UserSettings Settings;
        public double Rate = 0.0;
        public bool IsRegistered = false;
        public bool IsNewRecord = true;

        private static IDictionary<string, string> fieldDictionary = new Dictionary<string, string>()
        {
            {nameof(Id), "id"},
            {nameof(Username), "username"},
            {nameof(FirstName), "first_name"},
            {nameof(LastName), "last_name"},
            {nameof(TotalPoints), "total_points"},
            {nameof(GamesPlayed), "games"},
            {nameof(Wins), "wins"},
            {nameof(Survivals), "survivals"},
            {nameof(Draws), "draws"},
            {nameof(Rate), "rate"},
            {nameof(Settings), "settings"},
            {nameof(IsRegistered), "is_registered"},
        };

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
                var settingsObject = reader.IsDBNull(11) ? null : reader.GetString(11);
                var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<UserSettings>(settingsObject ?? "{}");
                Settings = settings ?? new UserSettings();
                IsNewRecord = false;
            }
            return this;
        }

        protected static string GetSelect()
        {
            return "SELECT id, username, first_name, last_name, total_points, games, wins, survivals, draws, rate, is_registered, settings FROM user ";
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
                command.CommandText = @"INSERT OR REPLACE INTO user (id, username, first_name, last_name, total_points, games, wins, survivals, draws, rate, settings, is_registered)
                                    VALUES (:id, :uname, :fname, :lname, :tpoints, :games, :wins, :survivals, :draws, :rate, :settings, 1)";
            }
            else
            {
                command.CommandText = @"UPDATE user SET id = :id, username = :uname, first_name = :fname, last_name = :lname, total_points = :tpoints,
                                    games = :games, wins = :wins, survivals = :survivals, draws = :draws, rate = :rate, settings = :settings, is_registered = 1 WHERE id = :id";
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
            command.Parameters.AddWithValue(":settings", Settings.ToString());

            return command.ExecuteNonQuery() > 0;
        }

        public bool UpdateFields(params string[] fields)
        {
            if (fields.Length == 0)
            {
                throw new ArgumentException("fields", "fields cannot be empty.");
            }
            if (IsNewRecord)
            {
                return Save();
            }
            var connection = Program.Connection;
            var command = connection.CreateCommand();
            command.CommandText = @"UPDATE user SET ";
            foreach (var field in fields)
            {
                var fieldName = fieldDictionary[field];
                command.CommandText += $"{fieldName} = :{fieldName} ";
                command.Parameters.AddWithValue($":{fieldName}", GetType().GetField(field).GetValue(this).ToString());
            }
            command.CommandText += "WHERE id = :id";
            command.Parameters.AddWithValue(":id", Id);

            return command.ExecuteNonQuery() > 0;
        }

        public enum Gender
        {
            Male,
            Female
        }

        public class UserSettings: IDictionary<string, object>
        {
            private Dictionary<string, object> _internalDictionary = new Dictionary<string, object>();

            protected static IDictionary<string, object> AllowedSettings => new Dictionary<string, object>() {
                { "gender", Gender.Male }
            };

            public Gender Gender => (Gender)this["gender"];

            public ICollection<string> Keys => AllowedSettings.Keys;

            public ICollection<object> Values => _internalDictionary.Values;

            public int Count => AllowedSettings.Count;

            public bool IsReadOnly => false;

            public object this[string key] {
                get
                {
                    if (_internalDictionary.ContainsKey(key))
                    {
                        return _internalDictionary[key];
                    }
                    return AllowedSettings[key];
                }
                set
                {
                    if (_internalDictionary.ContainsKey(key))
                    {
                        _internalDictionary.Remove(key);
                    }
                    Add(key, value);
                }
            }

            public void Add(string key, object value)
            {
                if (!AllowedSettings.ContainsKey(key))
                {
                    throw new ArgumentOutOfRangeException("key", $"User parameter {key} not found.");
                }
                
                switch (key)
                {
                    case "gender":
                        value = Enum.Parse(typeof(Gender), value.ToString());
                        break;
                }

                _internalDictionary.Add(key, value);
            }

            public bool ContainsKey(string key)
            {
                return AllowedSettings.ContainsKey(key);
            }

            public bool Remove(string key)
            {
                return _internalDictionary.Remove(key);
            }

            public bool TryGetValue(string key, out object value)
            {
                return _internalDictionary.TryGetValue(key, out value);
            }

            public void Add(KeyValuePair<string, object> item)
            {
                Add(item.Key, item.Value);
            }

            public void Clear()
            {
                _internalDictionary.Clear();
            }

            public bool Contains(KeyValuePair<string, object> item)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public bool Remove(KeyValuePair<string, object> item)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                foreach (var paramInfo in AllowedSettings)
                {
                    yield return _internalDictionary.ContainsKey(paramInfo.Key) ? new KeyValuePair<string, object>(paramInfo.Key, _internalDictionary[paramInfo.Key]) : paramInfo;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _internalDictionary.GetEnumerator();
            }

            public override string ToString()
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(this);
            }
        }
    }
}
