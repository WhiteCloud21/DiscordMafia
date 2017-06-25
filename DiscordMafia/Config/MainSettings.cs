using System;
using System.Collections.Generic;
using System.IO;
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
        public HashSet<ulong> AdminID { get; protected set; }

        public MainSettings(params string[] filenames)
        {
            foreach (var filename in filenames)
            {
                if (File.Exists(filename))
                {
                    using (Stream stream = new FileStream(filename, FileMode.Open))
                    {
                        var reader = new XmlTextReader(stream);
                        reader.WhitespaceHandling = WhitespaceHandling.None;
                        reader.ReadToFollowing("Settings");
                        ReadXml(reader);
                        reader.Close();
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
            AdminID = new HashSet<ulong>();
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
                            AdminID.Add(ulong.Parse(reader.ReadElementContentAsString()));
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
    }
}
