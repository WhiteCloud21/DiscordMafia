using Discord;
using Discord.WebSocket;

namespace DiscordMafia.Client
{
    public class UserWrapper
    {
        private readonly IUser _user;
        
        public ulong Id { get; set; }
        public string Username { get; set; }
        public string UsernameMention { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        
        public UserWrapper(IUser discordUser)
        {
            _user = discordUser;
            Id = discordUser.Id;
            var user = discordUser as IGuildUser;
            Username = !string.IsNullOrEmpty(user?.Nickname) ? user.Nickname : discordUser.Username;
            UsernameMention = discordUser.Mention;
            FirstName = Username;
            LastName = "";
        }

        public static implicit operator UserWrapper(SocketUser discordUser)
        {
            return new UserWrapper(discordUser);
        }

        public bool IsAdmin()
        {
            return Program.Settings.AdminId.Contains(Id);
        }

        public IDMChannel GetDmChannel()
        {
            return _user.GetOrCreateDMChannelAsync().Result;
        }
    }
}
