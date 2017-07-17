using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;


namespace DiscordMafia.DB
{
    [Table("user")]
    public class User
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("id")]
        public ulong Id { get; set; }

        [Column("username")]
        public string Username { get; set; } = "";

        [Column("first_name")]
        public string FirstName { get; set; } = "";

        [Column("last_name")]
        public string LastName { get; set; } = "";

        [Column("total_points")]
        public long TotalPoints { get; set; } = 0;

        [Column("games")]
        public int GamesPlayed { get; set; } = 0;

        [Column("wins")]
        public int Wins { get; set; } = 0;

        [Column("survivals")]
        public int Survivals { get; set; } = 0;

        [Column("draws")]
        public int Draws { get; set; } = 0;

        [Column("rate")]
        public double Rate { get; set; } = 0.0;

        [Column("is_registered")]
        public bool IsRegistered { get; set; } = false;

        [Column("settings")]
        public string SerializedSettings
        {
            get => Settings?.Serialized;
            set => Settings = new UserSettings {Serialized = value};
        }

        [NotMapped]
        public UserSettings Settings { get; set; } = new UserSettings();

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

        public static User FindById(ulong id)
        {
            using (var context = new GameContext())
            {
                var dbUser = context.Users.AsNoTracking().SingleOrDefault(u => u.Id == id);
                if (dbUser == null)
                {
                    dbUser = new User {Id = id};
                }

                return dbUser;
            }
        }

        public bool TryToSave()
        {
            try
            {
                using (var context = new GameContext())
                {
                    context.Users.Add(this);
                    context.SaveChanges();
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public enum Gender
        {
            Male,
            Female
        }

        public class UserSettings : IDictionary<string, object>
        {
            private Dictionary<string, object> _internalDictionary = new Dictionary<string, object>();

            protected static IDictionary<string, object> AllowedSettings => new Dictionary<string, object>()
            {
                {"gender", Gender.Male}
            };

            public Gender Gender => (Gender) this["gender"];

            public ICollection<string> Keys => AllowedSettings.Keys;

            public ICollection<object> Values => _internalDictionary.Values;

            public int Count => AllowedSettings.Count;

            public bool IsReadOnly => false;

            public object this[string key]
            {
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
                    yield return _internalDictionary.ContainsKey(paramInfo.Key)
                        ? new KeyValuePair<string, object>(paramInfo.Key, _internalDictionary[paramInfo.Key])
                        : paramInfo;
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

            public string Serialized
            {
                get => Newtonsoft.Json.JsonConvert.SerializeObject(_internalDictionary);
                set
                {
                    if (string.IsNullOrEmpty(value)) return;
                    var metaData = Newtonsoft.Json.JsonConvert.DeserializeObject<UserSettings>(value);
                    _internalDictionary = metaData._internalDictionary ?? new Dictionary<string, object>();
                }
            }
        }
    }
}