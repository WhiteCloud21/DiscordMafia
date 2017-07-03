using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using DiscordMafia.Lib;

namespace DiscordMafia.Config
{
    public class Messages
    {
        private MessageDictionary messages = new MessageDictionary();
        private Random random = new Random();

        private static Dictionary<string, Messages> instances = new Dictionary<string, Messages>();

        private Messages() { }

        public static Messages getInstance(string filename)
        {
            if (!instances.ContainsKey(filename))
            {
                var storage = new Messages();

                // загружаем данные из файла
                using (Stream stream = new FileStream(filename, FileMode.Open))
                {
                    var serializer = new XmlSerializer(typeof(MessageDictionary));
                    storage.messages = (MessageDictionary)serializer.Deserialize(stream);
                }

                instances.Add(filename, storage);
            }
            return instances[filename];
        }

        public string get(string key)
        {
            if (this.messages.ContainsKey(key))
            {
                var messages = this.messages[key];
                return messages[random.Next(messages.Length)];
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
