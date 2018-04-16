using Discord;
using Discord.WebSocket;
using DiscordMafia.Config;
using System;
using System.Threading.Tasks;

namespace DiscordMafia.Client
{
    public class DiscordClientWrapper
    {
        public DiscordSocketClient MainClient { get; private set; }
        public DiscordSocketClient AnnouncerClient { get; private set; }
        public SocketTextChannel AnnouncerGameChannel { get; private set; }
        private MainSettings _settings;

        public DiscordClientWrapper(MainSettings settings, Func<Task> clientReadyHandler)
        {
            _settings = settings;
            InitAsync(clientReadyHandler).Wait();
        }

        private async Task InitAsync(Func<Task> clientReadyHandler)
        {
            MainClient = new DiscordSocketClient();

            if (!string.IsNullOrWhiteSpace(_settings.AnnouncerToken))
            {
                AnnouncerClient = new DiscordSocketClient();

                await AnnouncerClient.LoginAsync(TokenType.Bot, _settings.AnnouncerToken);
                await AnnouncerClient.StartAsync();
            }
            else
            {
                AnnouncerClient = MainClient;
            }
            
            MainClient.Ready += clientReadyHandler;
            AnnouncerClient.Ready += SetAnnouncerChannels;

            await MainClient.LoginAsync(TokenType.Bot, _settings.Token);
            await MainClient.StartAsync();
        }

        private async Task SetAnnouncerChannels()
        {
            AnnouncerClient.Ready -= SetAnnouncerChannels;
            AnnouncerGameChannel = AnnouncerClient.GetChannel(_settings.GameChannel) as SocketTextChannel;
            await Task.CompletedTask;
        }
    }
}
