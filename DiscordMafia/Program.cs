using System.Data.SQLite;
using Discord;
using System.Threading;
using DiscordMafia.Config;
using System;
using SimpleMigrations.DatabaseProvider;

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
            try
            {
                syncContext.RunMessagePump();
            }
            catch (Exception ex)
            {
                var message = String.Format("[{0:s}] {1}", DateTime.Now, ex);
                Console.Error.WriteLine(message);
                System.IO.File.AppendAllText("error.log", message);
            }
        }

        static async void Run()
        {
            connection = new SQLiteConnection($"Data Source={settings.DatabasePath};Version=3;");
            connection.Open();
            Migrate(connection);

            var client = new DiscordClient();
            game = new Game(syncContext, client, Settings);

            client.MessageReceived += (s, e) =>
            {
                syncContext.Post((state) =>
                {
                    if (!e.Message.IsAuthor)
                    {
                        ProcessMessage(e);
                    }
                }, null);
            };

            await client.Connect(settings.Token, TokenType.Bot);
            client.SetGame(null);
        }

        private static void Migrate(SQLiteConnection connection)
        {
            var databaseProvider = new SqliteDatabaseProvider(connection);
            var migrationsAssembly = typeof(Program).Assembly;
            var migrator = new SimpleMigrations.SimpleMigrator(migrationsAssembly, databaseProvider, new SimpleMigrations.Console.ConsoleLogger());
            migrator.Load();
            migrator.MigrateToLatest();
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
