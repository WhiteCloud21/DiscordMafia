﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using DiscordMafia.Config.Lang;
using DiscordMafia.Lib;
using DiscordMafia.Services;

namespace DiscordMafia.Config
{
    [Serializable]
    [XmlRoot("Points")]
    public class Points: SerializableDictionary<string, PointsInfo>
    {
        [NonSerialized]
#pragma warning disable CA2235 // Mark all non-serializable fields.
        private static Dictionary<string, Points> instances = new Dictionary<string, Points>();
#pragma warning restore CA2235 // Mark all non-serializable fields.

        protected override string ValueTagName
        {
            get
            {
                return "";
            }
        }

        protected override string KeyTagName
        {
            get
            {
                return "";
            }
        }

        public long GetPoints(string key)
        {
            long result = 0;
            if (TryGetValue(key, out PointsInfo info))
            {
                result = info.Points;
            }
            return result;
        }

        private Points() { }

        public static Points GetInstance(string filename)
        {
            if (!instances.ContainsKey(filename))
            {
                var instance = new Points();

                // загружаем данные из файла
                using (Stream stream = new FileStream(filename, FileMode.Open))
                {
                    var serializer = new XmlSerializer(typeof(Points));
                    instance = (Points)serializer.Deserialize(stream);

                    foreach (var item in instance)
                    {
                        item.Value.Id = item.Key;
                    }
                }

                instances.Add(filename, instance);
            }
            return instances[filename];
        }
    }

    [Serializable]
    public class PointsInfo: IXmlSerializable
    {
#pragma warning disable CA2235 // Mark all non-serializable fields.
        public int Points;
        public string Id;
        private static readonly XmlSerializer IntSerializer =
                                        new XmlSerializer(typeof(int));

        private static readonly XmlSerializer StringSerializer =
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
                Points = (int)IntSerializer.Deserialize(reader);
            }
            finally
            {
                reader.ReadEndElement();
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            IntSerializer.Serialize(writer, Points);
        }
    }
    
    public static class PointsInfoExtensions
    {
        public static string GetDescription(this PointsInfo item, ILanguage language)
        {
            return GetItemLangInfo(item, language)?.Description ?? $"#POINTS_DESC_{item.Id}";
        }

        private static PointMessages.PointMessage GetItemLangInfo(this PointsInfo item, ILanguage language)
        {
            if (language.PointMessages.TryGetValue(item.Id, out var result))
            {
                return result;
            }
            return null;
        }
    }
}
