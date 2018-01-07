using System;
using System.Collections.Generic;
using DiscordMafia.Config;
using DiscordMafia.Roles;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using DiscordMafia.Config.Lang;

namespace DiscordMafia.Config
{
    [Serializable]
    public class GameSettings : IXmlSerializable
    {
        public string GameType { get; private set; }

        public int MinPlayers { get; protected set; }
        public Dictionary<Team, int> MaxRandomPlayers { get; protected set; }
        public short MafPercent { get; protected set; }
        public short InfectionChancePercent { get; protected set; }

        public bool MuteOnDeath { get; protected set; }
        public bool StartFromNight { get; protected set; }
        public bool ShowNightActions { get; protected set; }
        public bool IsYakuzaEnabled { get; protected set; }
        public int PlayerCollectingTime { get; protected set; }
        public int PauseTime { get; protected set; }
        public int MorningTime { get; protected set; }
        public int DayTime { get; protected set; }
        public int EveningTime { get; protected set; }
        public int NightTime { get; protected set; }
        public byte MaxUsersToNotify { get; protected set; }
        public int MinNotificationInterval { get; protected set; }

        public Language Language { get; private set; }
        public Messages Messages { get; private set; }
        public Points Points { get; private set; }
        public Roles Roles { get; private set; }

        public GameSettings(MainSettings mainSettings, string gametype)
        {
            GameType = gametype;

            // Значения по умолчанию
            MaxRandomPlayers = new Dictionary<Team, int>()
            {
                { Team.Civil, 2 },
                { Team.Mafia, 1 },
                { Team.Neutral, 1 },
                { Team.Yakuza, 1 },
            };
            MuteOnDeath = false;
            MinPlayers = 3;
            MafPercent = 34;
            StartFromNight = true;
            PlayerCollectingTime = 60000;
            PauseTime = 1000;
            MorningTime = 2000;
            DayTime = 90000;
            EveningTime = 30000;
            NightTime = 90000;
            InfectionChancePercent = 33;
            MaxUsersToNotify = 50;
            MinNotificationInterval = 7200;
            ShowNightActions = true;
            IsYakuzaEnabled = false;

            ReadConfig();

            Language = new Language();
            Language.Load($"Config/Lang/{mainSettings.Language}");

            /*var im = new PlaceMessages();
            var items = DiscordMafia.Roles.Places.Place.AvailablePlaces;
            foreach (var item in items)
            {
                im.Add(item.Id, new PlaceMessages.PlaceMessage() { Name = item.Name });
            }
            using (System.IO.Stream stream = new System.IO.FileStream(GetFilePath($"Lang/{mainSettings.Language}/placeMessages.xml"), System.IO.FileMode.OpenOrCreate))
            {
                var serializer = new XmlSerializer(im.GetType());
                //Create our own namespaces for the output
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();

                //Add an empty namespace and empty value
                ns.Add("", "");
                serializer.Serialize(stream, im, ns);
            }*/

            Console.WriteLine("Messages successfully loaded");
            Points = Points.GetInstance(GetFilePath("points.xml"));
            Console.WriteLine("Points configuration successfully loaded");
            Roles = Roles.GetInstance(GetFilePath("roles.xml"));
            Console.WriteLine("Role configuration successfully loaded");
        }

        protected void ReadConfig()
        {
            using (var stream = new System.IO.FileStream(GetFilePath("gameSettings.xml"), System.IO.FileMode.Open))
            {
                using (var reader = XmlReader.Create(stream))
                {
                    reader.ReadToFollowing("Settings");
                    ReadXml(reader);
                }
            }
        }

        protected string GetFilePath(string fileName)
        {
            if (GameType != null && System.IO.File.Exists($"Config/Gametypes/{GameType}/{fileName}"))
            {
                return $"Config/Gametypes/{GameType}/{fileName}";
            }
            return $"Config/{fileName}";
        }

        public bool IsValidGametype(string gameType)
        {
            return System.IO.Directory.Exists($"Config/Gametypes/{gameType}");
        }

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
                while (reader.MoveToContent() != XmlNodeType.EndElement)
                {
                    var name = reader.Name;
                    // TODO Сделать нормальный парсер (на атрибутах свойств?)
                    switch (name)
                    {
                        case "MuteOnDeath":
                            MuteOnDeath = bool.Parse(reader.ReadElementContentAsString());
                            break;
                        case "StartFromNight":
                            StartFromNight = bool.Parse(reader.ReadElementContentAsString());
                            break;
                        case "ShowNightActions":
                            ShowNightActions = bool.Parse(reader.ReadElementContentAsString());
                            break;
                        case "IsYakuzaEnabled":
                            IsYakuzaEnabled = bool.Parse(reader.ReadElementContentAsString());
                            break;
                        case "MafPercent":
                            MafPercent = short.Parse(reader.ReadElementContentAsString());
                            break;
                        case "MinPlayers":
                            MinPlayers = int.Parse(reader.ReadElementContentAsString());
                            break;
                        case "PlayerCollectingTime":
                            PlayerCollectingTime = int.Parse(reader.ReadElementContentAsString());
                            break;
                        case "PauseTime":
                            PauseTime = int.Parse(reader.ReadElementContentAsString());
                            break;
                        case "MorningTime":
                            MorningTime = int.Parse(reader.ReadElementContentAsString());
                            break;
                        case "DayTime":
                            DayTime = int.Parse(reader.ReadElementContentAsString());
                            break;
                        case "EveningTime":
                            EveningTime = int.Parse(reader.ReadElementContentAsString());
                            break;
                        case "NightTime":
                            NightTime = int.Parse(reader.ReadElementContentAsString());
                            break;
                        case "MinNotificationInterval":
                            MinNotificationInterval = int.Parse(reader.ReadElementContentAsString());
                            break;
                        case "MaxUsersToNotify":
                            MaxUsersToNotify = byte.Parse(reader.ReadElementContentAsString());
                            break;
                        case "InfectionChancePercent":
                            InfectionChancePercent = short.Parse(reader.ReadElementContentAsString());
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
