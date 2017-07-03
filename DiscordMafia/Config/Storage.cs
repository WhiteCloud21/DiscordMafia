using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using DiscordMafia.Lib;

namespace DiscordMafia.Config
{
    public class Messages
    {
        private MessageDictionary _messages = new MessageDictionary();
        private readonly Random _random = new Random();

        private static readonly Dictionary<string, Messages> _instances = new Dictionary<string, Messages>();

        private Messages() { }

        public static Messages GetInstance(string filename)
        {
            if (!_instances.ContainsKey(filename))
            {
                var storage = new Messages();

                // загружаем данные из файла
                using (Stream stream = new FileStream(filename, FileMode.Open))
                {
                    var serializer = new XmlSerializer(typeof(MessageDictionary));
                    storage._messages = (MessageDictionary)serializer.Deserialize(stream);
                }

                _instances.Add(filename, storage);
            }
            return _instances[filename];
        }

        public string get(string key)
        {
            if (this._messages.ContainsKey(key))
            {
                var messages = this._messages[key];
                return messages[_random.Next(messages.Length)];
            }
            return "";
        }
    }

    [Serializable]
    [XmlRoot("Messages")]
    public class MessageDictionary : SerializableDictionary<string, string[]>
    {
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

        public MessageDictionary() : base()
        {
        }
    }
}
