using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace DiscordMafia.Lib
{
    [Serializable]
    [XmlRoot("Messages")]
    public class MessageDictionary<T, TKey, TValue> : SerializableDictionary<TKey, TValue> where T: MessageDictionary<T, TKey, TValue>
    {
        private static readonly Dictionary<string, T> _instances = new Dictionary<string, T>();

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

        public static T GetInstance(string filename)
        {
            if (!_instances.ContainsKey(filename))
            {
                using (Stream stream = new FileStream(filename, FileMode.Open))
                {
                    var serializer = new XmlSerializer(typeof(T));
                    T result = (T)serializer.Deserialize(stream);
                    _instances.Add(filename, result);
                }

            }
            return _instances[filename];
        }
    }
}
