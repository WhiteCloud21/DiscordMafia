using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace DiscordMafia.Lib
{
    [Serializable]
    [XmlRoot("Messages")]
    public class MessageDictionary<T, TKey, TValue> : SerializableDictionary<TKey, TValue>, IMergeable where T: MessageDictionary<T, TKey, TValue>
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
            if (TryGetInstance(filename, out T instance))
            {
                return instance;
            }
            throw new FileNotFoundException("Language file was not found", filename);
        }

        public static bool TryGetInstance(string filename, out T instance)
        {
            if (!_instances.ContainsKey(filename))
            {
                if (!File.Exists(filename))
                {
                    instance = null;
                    return false;
                }
                using (Stream stream = new FileStream(filename, FileMode.Open))
                {
                    var serializer = new XmlSerializer(typeof(T));
                    T result = (T)serializer.Deserialize(stream);
                    _instances.Add(filename, result);
                }

            }
            instance = _instances[filename];
            return true;
        }

        public void Merge(string filename, MergeStrategy strategy)
        {
            if (TryGetInstance(filename, out T newInstance))
            {
                Merge(newInstance, strategy);
            }
        }

        public void Merge(T newInstance, MergeStrategy strategy)
        {
            foreach (var item in newInstance)
            {
                if (ContainsKey(item.Key))
                {
                    switch (strategy)
                    {
                        case MergeStrategy.Normal:
                            this[item.Key] = item.Value;
                            break;
                        case MergeStrategy.Recursive:
                            if (item.Value is IMergeable)
                            {
                                (this[item.Key] as IMergeable).Merge(item.Value as IMergeable, strategy);
                            }
                            else
                            {
                                this[item.Key] = item.Value;
                            }
                            break;
                        default:
                            throw new ArgumentException("Unknown merge strategy", "strategy");
                    }
                }
                else
                {
                    this[item.Key] = item.Value;
                }
            }
        }

        void IMergeable.Merge(IMergeable newInstance, MergeStrategy strategy)
        {
            Merge(newInstance as T, strategy);
        }

        public static T MergeOrLoad(T messageDictionary, string fileName, MergeStrategy mergeStrategy)
        {
            if (messageDictionary != null)
            {
                messageDictionary.Merge(fileName, mergeStrategy);
            }
            else
            {
                messageDictionary = GetInstance(fileName);
            }
            return messageDictionary;
        }
    }

    public interface IMergeable
    {
        void Merge(IMergeable newInstance, MergeStrategy strategy);
    }

    public enum MergeStrategy
    {
        Normal = 1,
        Recursive = 2
    }
}
