using System.Data.SQLite;
using Discord;
using System.Threading;
using DiscordMafia.Config;

namespace DiscordMafia
{
    class Program
    {
        public static BotSynchronizationContext syncContext = new BotSynchronizationContext();
        private static MainSettings settings;
        private static Game game;
        public static SQLiteConnection connection;

        public static MainSettings Settings
        {
            get
            {
                return settings;
            }
        }

        static void Main(string[] args)
        {
            settings = new MainSettings("Config/mainSettings.xml", "Config/Local/mainSettings.xml");
                
            SynchronizationContext.SetSynchronizationContext(syncContext);
            syncContext.Post(obj => Run(), null);
            syncContext.RunMessagePump();
        }

        static async void Run()
        {
            connection = new SQLiteConnection($"Data Source={settings.DatabasePath};Version=3;");
            connection.Open();

            var _client = new DiscordClient();
            game = new Game(syncContext, _client);

            _client.MessageReceived += (s, e) =>
            {
                syncContext.Post((state) =>
                {
                    if (!e.Message.IsAuthor)
                    {
                        ProcessMessage(e);
                    }
                }, null);
            };

            await _client.Connect(settings.Token, TokenType.Bot);
            _client.SetGame(null);
        }

        private static void ProcessMessage(MessageEventArgs e)
        {
            if (e.Channel.IsPrivate)
            {
                game.OnPrivateMessage(e);
            }
            else
            {
                game.OnPublicMessage(e);
            }
        }
    }
}
