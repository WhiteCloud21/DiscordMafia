using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace DiscordMafia.Config
{

    [Serializable]
    public class MainSettings : IXmlSerializable
    {
        public string Token { get; protected set; }
        public string ImageBaseUrl { get; protected set; }
        public string DatabasePath { get; protected set; }
        public ulong  GameChannel { get; protected set; }
        public HashSet<ulong> AdminId { get; protected set; }
        public string LanguageStr { get; protected set; }
        public Lang.Language Language { get; protected set; }

        public string ConfigPath { get; private set; }

        public MainSettings(params string[] filenames)
        {
            foreach (var filename in filenames)
            {
                if (File.Exists(filename))
                {
                    if (string.IsNullOrEmpty(ConfigPath))
                    {
                        ConfigPath = Path.GetDirectoryName(filename);
                    }
                    using (Stream stream = new FileStream(filename, FileMode.Open))
                    {
                        using (var reader = XmlReader.Create(stream))
                        {
                            reader.ReadToFollowing("Settings");
                            ReadXml(reader);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Config file {0} not found. Skipping...", filename);
                }
            }
        }

        public void ReadXml(XmlReader reader)
        {
            AdminId = new HashSet<ulong>();
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
                        case "Token":
                            Token = reader.ReadElementContentAsString();
                            break;
                        case "ImageBaseUrl":
                            ImageBaseUrl = reader.ReadElementContentAsString();
                            break;
                        case "DatabasePath":
                            DatabasePath = reader.ReadElementContentAsString();
                            break;
                        case "GameChannel":
                            GameChannel = ulong.Parse(reader.ReadElementContentAsString());
                            break;
                        case "AdminID":
                            AdminId.Add(ulong.Parse(reader.ReadElementContentAsString()));
                            break;
                        case "Language":
                            LanguageStr = reader.ReadElementContentAsString();
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

        public XmlSchema GetSchema()
        {
            return null;
        }

        public bool IsValidLanguage(string language)
        {
            if (!Regex.IsMatch(language, @"[a-zA-Z0-9\-]+"))
            {
                return false;
            }
            return Directory.Exists(Path.Combine(ConfigPath, $"Lang/{LanguageStr}"));
        }

        public void LoadLanguage()
        {
            if (!IsValidLanguage(LanguageStr))
            {
                throw new Exception("Language is not valid in mainSettings!");
            }
            Language = new Lang.Language();
            Language.Load(Path.Combine(ConfigPath, $"Lang/{LanguageStr}"));
        }
    }
}
