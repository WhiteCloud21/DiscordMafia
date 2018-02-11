using DiscordMafia.Lib;
using DiscordMafia.Services;
using System;
using System.IO;
using System.Xml.Serialization;

namespace DiscordMafia.Config.Lang
{
    public class Language: ILanguage
    {
        public Meta Meta { get; set; }
        public Messages Messages { get; private set; }
        public SimpleMessages SimpleMessages { get; private set; }
        public RoleMessages RoleMessages { get; private set; }
        public ItemMessages ItemMessages { get; private set; }
        public PlaceMessages PlaceMessages { get; private set; }
        public AchievementMessages AchievementMessages { get; private set; }
        public ModuleMessages ModuleMessages { get; private set; }
        public PointMessages PointMessages { get; private set; }

        public void Load(string filePath)
        {
            LoadMeta(Path.Combine(filePath, "meta.xml"));
            string basedOnFilePath = null;
            if (Meta.BasedOn != null)
            {
                basedOnFilePath = Path.Combine(Path.GetDirectoryName(filePath), Meta.BasedOn);
                if (basedOnFilePath == filePath)
                {
                    throw new ArgumentException("Language cannot be based on itself.");
                }
            }
            if (basedOnFilePath != null)
            {
                Load(basedOnFilePath);
            }
            Messages = Messages.MergeOrLoad(Messages, Path.Combine(filePath, "messages.xml"), MergeStrategy.Recursive);
            SimpleMessages = SimpleMessages.MergeOrLoad(SimpleMessages, Path.Combine(filePath, "simpleMessages.xml"), MergeStrategy.Recursive);
            RoleMessages = RoleMessages.MergeOrLoad(RoleMessages, Path.Combine(filePath, "roles.xml"), MergeStrategy.Recursive);
            ItemMessages = ItemMessages.MergeOrLoad(ItemMessages, Path.Combine(filePath, "items.xml"), MergeStrategy.Recursive);
            PlaceMessages = PlaceMessages.MergeOrLoad(PlaceMessages, Path.Combine(filePath, "places.xml"), MergeStrategy.Recursive);
            AchievementMessages = AchievementMessages.MergeOrLoad(AchievementMessages, Path.Combine(filePath, "achievements.xml"), MergeStrategy.Recursive);
            ModuleMessages = ModuleMessages.MergeOrLoad(ModuleMessages, Path.Combine(filePath, "modules.xml"), MergeStrategy.Recursive);
            PointMessages = PointMessages.MergeOrLoad(PointMessages, Path.Combine(filePath, "points.xml"), MergeStrategy.Recursive);
        }

        private void LoadMeta(string fileName)
        {
            using (Stream stream = new FileStream(fileName, FileMode.Open))
            {
                var serializer = new XmlSerializer(typeof(Meta));
                Meta = (Meta)serializer.Deserialize(stream);
            }
        }
    }
}
