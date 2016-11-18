using System;

namespace DiscordMafia.Client
{
    public class UserWrapper
    {
        public ulong Id { get; set; }
        public string Username { get; set; }
        public string UsernameMention { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public UserWrapper(Discord.User DiscordUser)
        {
            Id = DiscordUser.Id;
            Username = DiscordUser.Nickname ?? DiscordUser.Name;
            UsernameMention = DiscordUser.NicknameMention;
            FirstName = DiscordUser.Name;
            LastName = "";
        }

        public static implicit operator UserWrapper(Discord.User DiscordUser)
        {
            return new UserWrapper(DiscordUser);
        }

        public bool IsAdmin()
        {
            // TODO Сделать нормально
            return FirstName == "WhiteCloud";
        }
    }
}
