using Microsoft.Data.Sqlite;
using Discord;
using System.Threading;
using DiscordMafia.Config;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord.WebSocket;
using SimpleMigrations.DatabaseProvider;

namespace DiscordMafia
{
    internal class Program
    {
        public static BotSynchronizationContext SyncContext = new BotSynchronizationContext();
        private static Game _game;
        public static SqliteConnection Connection;

        public static MainSettings Settings { get; private set; }

        private static void Main(string[] args)
        {
            Settings = new MainSettings("Config/mainSettings.xml", "Config/Local/mainSettings.xml");
                
            SynchronizationContext.SetSynchronizationContext(SyncContext);
            SyncContext.Post(obj => Run(), null);
            try
            {
                SyncContext.RunMessagePump();
            }
            catch (Exception ex)
            {
                var message = $"[{DateTime.Now:s}] {ex}";
                Console.Error.WriteLine(message);
                System.IO.File.AppendAllText("error.log", message);
            }
        }

        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private static async void Run()
        {
            Connection = new SqliteConnection($"Data Source={Settings.DatabasePath};");
            Connection.Open();
            Migrate(Connection);

            var client = new DiscordSocketClient();
            client.Log += Log;
            _game = new Game(SyncContext, client, Settings);

            client.MessageReceived += (message) =>
            {
                return Task.Run(() =>
                {
                    SyncContext.Post((state) =>
                    {
                        if (message.Author.Id != client.CurrentUser.Id)
                        {
                            ProcessMessage(message);
                        }
                    }, null);
                });
            };

            await client.LoginAsync(TokenType.Bot, Settings.Token);
            await client.StartAsync();
            await client.SetGameAsync(null);
        }

        private static void Migrate(SqliteConnection connection)
        {
            var databaseProvider = new SqliteDatabaseProvider(connection);
            var migrationsAssembly = typeof(Program).GetTypeInfo().Assembly;
            var migrator = new SimpleMigrations.SimpleMigrator(migrationsAssembly, databaseProvider);
            migrator.Load();
            migrator.MigrateToLatest();
        }

        private static void ProcessMessage(SocketMessage message)
        {
            if (message.Channel is SocketDMChannel)
            {
                _game.OnPrivateMessage(message);
            }
            else
            {
                _game.OnPublicMessage(message);
            }
        }
    }
}

