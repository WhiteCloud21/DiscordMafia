using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using DiscordMafia.Lib;
using DiscordMafia.Roles;
using System.Text;

namespace DiscordMafia.Config
{
    [Serializable]
    [XmlRoot("Roles")]
    public class Roles: Dictionary<string, RoleConfig>, IXmlSerializable
    {
        [NonSerialized]
#pragma warning disable CA2235 // Mark all non-serializable fields.
        private static Dictionary<string, Roles> instances = new Dictionary<string, Roles>();

        private Dictionary<string, System.Reflection.ConstructorInfo> typeCache = null;

        private static readonly XmlSerializer valueSerializer =
                                        new XmlSerializer(typeof(Roles));
#pragma warning restore CA2235 // Mark all non-serializable fields.

        private Roles() { }

        public static Roles getInstance(string filename)
        {
            if (!instances.ContainsKey(filename))
            {
                var instance = new Roles();

                // загружаем данные из файла
                using (Stream stream = new FileStream(filename, FileMode.Open))
                {
                    var serializer = new XmlSerializer(typeof(Roles));
                    instance = (Roles)serializer.Deserialize(stream);
                }

                instances.Add(filename, instance);
            }
            return instances[filename];
        }

        protected Roles(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }

        public List<BaseRole> getTemporaryRoles(int playersCount)
        {
            return filter(playersCount, false);
        }

        public List<BaseRole> getRandomRoles(int playersCount, IDictionary<Team, int> maxPlayersByTeam, Random randomGenerator)
        {
            var roles = filter(playersCount, true);
            var result = new List<BaseRole>();
            var rolesByTeam = new Dictionary<Team, List<BaseRole>>();
            foreach (var team in maxPlayersByTeam.Keys)
            {
                rolesByTeam[team] = new List<BaseRole>();
            }
            foreach (var role in roles)
            {
                rolesByTeam[role.Team].Add(role);
            }
            foreach (var playersByTeam in maxPlayersByTeam)
            {
                for (int i = 0; i < playersByTeam.Value; i++)
                {
                    var count = rolesByTeam[playersByTeam.Key].Count;
                    if (count > 0)
                    {
                        var idx = randomGenerator.Next(count);
                        result.Add(rolesByTeam[playersByTeam.Key][idx]);
                        rolesByTeam[playersByTeam.Key].RemoveAt(idx);
                    }
                }
            }
            return result;
        }

        protected List<BaseRole> filter(int playersCount, bool isRandom, bool onlyEnabled = true)
        {
            fillCache();
            var result = new List<BaseRole>();
            foreach (var kvp in this)
            {
                if (typeCache.ContainsKey(kvp.Key))
                {
                    if ((kvp.Value.IsEnabled || !onlyEnabled) && kvp.Value.IsRandom == isRandom && playersCount >= kvp.Value.MinPlayers)
                    {
                        var constructor = typeCache[kvp.Key];
                        var role = constructor.Invoke(new object[0]) as BaseRole;
                        result.Add(role);
                    }
                }
            }
            return result;
        }

        protected void fillCache(bool clear = false)
        {
            if (clear && typeCache != null)
            {
                typeCache.Clear();
                typeCache = null;
            }
            if (typeCache == null)
            {
                typeCache = new Dictionary<string, System.Reflection.ConstructorInfo>();
                foreach (var roleName in this.Keys)
                {
                    var roleType = Type.GetType(typeof(BaseRole).Namespace + "." + roleName, false);
                    if (roleType != null)
                    {
                        try
                        {
                            var constructor = roleType.GetConstructor(Type.EmptyTypes);
                            typeCache.Add(roleName, constructor);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Ошибка при кешировании конфига ролей: " + ex.Message);
                        }
                    }
                }
            }
        }

        public string RolesHelp()
        {
            fillCache();
            var result = new StringBuilder();
            foreach (var kvp in this)
            {
                if (typeCache.ContainsKey(kvp.Key))
                {
                    var constructor = typeCache[kvp.Key];
                    var role = constructor.Invoke(new object[0]) as BaseRole;
                    result.Append($"<b>{role.Name}</b> - ");
                    result.Append(kvp.Value.IsEnabled ? "доступна, " : "недоступна, ");
                    result.Append(kvp.Value.IsRandom ? "одна из случайных, " : "в игре при достаточном количестве игроков, ");
                    result.AppendLine($"минимум игроков для появления - {kvp.Value.MinPlayers}");
                }
            }
            return result.ToString();
        }

        public void ReadXml(XmlReader reader)
        {
            bool wasEmpty = reader.IsEmptyElement;

            reader.Read();

            if (wasEmpty)
            {
                return;
            }

            try
            {
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    var key = reader.GetAttribute("Id");
                    var value = new RoleConfig();
                    value.ReadXml(reader);

                    Add(key, value);

                    reader.MoveToContent();
                }
            }
            finally
            {
                reader.ReadEndElement();
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            // TODO
        }

        public XmlSchema GetSchema()
        {
            return null;
        }
    }

    [Serializable]
    public class RoleConfig : IXmlSerializable
    {
#pragma warning disable CA2235 // Mark all non-serializable fields.
        public bool IsEnabled = false;
        public bool IsRandom = false;
        public int MinPlayers = 0;
        private static readonly XmlSerializer intSerializer =
                                        new XmlSerializer(typeof(int));

        private static readonly XmlSerializer stringSerializer =
                                        new XmlSerializer(typeof(string));
#pragma warning restore CA2235 // Mark all non-serializable fields.

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            bool wasEmpty = reader.IsEmptyElement;

            reader.Read();

            if (wasEmpty)
            {
                return;
            }

            try
            {
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    reader.MoveToContent();
                    var name = reader.Name;
                    switch (name)
                    {
                        case "IsEnabled":
                            IsEnabled = reader.ReadElementContentAsBoolean();
                            break;
                        case "IsRandom":
                            IsRandom = reader.ReadElementContentAsBoolean();
                            break;
                        case "MinPlayers":
                            MinPlayers = reader.ReadElementContentAsInt();
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
            finally
            {
                reader.ReadEndElement();
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            // TODO
        }
    }
}
