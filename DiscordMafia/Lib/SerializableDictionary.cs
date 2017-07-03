using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace DiscordMafia.Lib
{
    [Serializable]
    [XmlRoot("Dictionary")]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {
#pragma warning disable CA2235 // Mark all non-serializable fields.
        private static readonly XmlSerializer KeySerializer =
                                        new XmlSerializer(typeof(TKey));

        private static readonly XmlSerializer ValueSerializer =
                                        new XmlSerializer(typeof(TValue));
#pragma warning restore CA2235 // Mark all non-serializable fields.

        public SerializableDictionary() : base()
        {
        }

        protected virtual string ItemTagName
        {
            get { return "Item"; }
        }

        protected virtual string KeyTagName
        {
            get { return "Key"; }
        }

        protected virtual string ValueTagName
        {
            get { return "Value"; }
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
                    if (ItemTagName.Length > 0)
                    {
                        reader.ReadStartElement(this.ItemTagName);
                    }
                    try
                    {
                        TKey tKey;
                        TValue tValue;

                        if (KeyTagName.Length > 0)
                        {
                            reader.ReadStartElement(this.KeyTagName);
                        }
                        try
                        {
                            tKey = (TKey)KeySerializer.Deserialize(reader);
                        }
                        finally
                        {
                            if (KeyTagName.Length > 0)
                            {
                                reader.ReadEndElement();
                            }
                        }

                        if (ValueTagName.Length > 0)
                        {
                            reader.ReadStartElement(this.ValueTagName);
                        }
                        try
                        {
                            tValue = (TValue)ValueSerializer.Deserialize(reader);
                        }
                        finally
                        {
                            if (ValueTagName.Length > 0)
                            {
                                reader.ReadEndElement();
                            }
                        }

                        this.Add(tKey, tValue);
                    }
                    finally
                    {
                        if (ItemTagName.Length > 0)
                        {
                            reader.ReadEndElement();
                        }
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
            foreach (KeyValuePair<TKey, TValue> keyValuePair in this)
            {
                if (ItemTagName.Length > 0)
                {
                    writer.WriteStartElement(this.ItemTagName);
                }
                try
                {
                    if (KeyTagName.Length > 0)
                    {
                        writer.WriteStartElement(this.KeyTagName);
                    }
                    try
                    {
                        KeySerializer.Serialize(writer, keyValuePair.Key);
                    }
                    finally
                    {
                        if (KeyTagName.Length > 0)
                        {
                            writer.WriteEndElement();
                        }
                    }

                    if (ValueTagName.Length > 0)
                    {
                        writer.WriteStartElement(this.ValueTagName);
                    }
                    try
                    {
                        ValueSerializer.Serialize(writer, keyValuePair.Value);
                    }
                    finally
                    {
                        if (ValueTagName.Length > 0)
                        {
                            writer.WriteEndElement();
                        }
                    }
                }
                finally
                {

                    if (ItemTagName.Length > 0)
                    {
                        writer.WriteEndElement();
                    }
                }
            }
        }
    }
}
