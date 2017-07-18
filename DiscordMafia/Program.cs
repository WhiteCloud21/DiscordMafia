using Microsoft.Data.Sqlite;
using Discord;
using DiscordMafia.Config;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SimpleMigrations.DatabaseProvider;

namespace DiscordMafia
{
    internal class Program
    {
        public static BotSynchronizationContext SyncContext = new BotSynchronizationContext();
        private static Game _game;
        public static SqliteConnection Connection;

        public static MainSettings Settings { get; private set; }

        private CommandService commands;
        private DiscordSocketClient client;
        private IServiceProvider services;

        static void Main(string[] args)
        {
            new Program().Start().GetAwaiter().GetResult();
        }

        public async Task Start()
        {
            var settings = new MainSettings("Config/mainSettings.xml", "Config/Local/mainSettings.xml");

            var connection = new SqliteConnection($"Data Source={settings.DatabasePath};");
            connection.Open();
            Migrate(connection);

            Connection = connection;
            Settings = settings;

            client = new DiscordSocketClient();
            client.Log += Log;

            Func<Task> clientReadyHandler = null;
            client.Ready += clientReadyHandler = async () =>
            {
                _game = new Game(SyncContext, client, settings);

                commands = new CommandService();

                var serviceCollection = new ServiceCollection();
                serviceCollection.AddSingleton(client);
                serviceCollection.AddSingleton(connection);
                serviceCollection.AddSingleton(settings);
                serviceCollection.AddSingleton(commands);
                serviceCollection.AddSingleton(_game);

                services = serviceCollection.BuildServiceProvider();

                await InstallCommands();

                await client.SetGameAsync(null);
                client.Ready -= clientReadyHandler;
            };

            await client.LoginAsync(TokenType.Bot, Settings.Token);
            await client.StartAsync();

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

            await Task.CompletedTask;
        }

        public async Task InstallCommands()
        {
            commands.AddTypeReader<InGamePlayerInfo>(new TypeReaders.InGamePlayerInfoTypeReader());

            // Hook the MessageReceived Event into our Command Handler
            client.MessageReceived += HandleCommand;
            // Discover all of the commands in this assembly and load them.
            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public async Task HandleCommand(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message is a command, based on if it starts with '/' or a mention prefix
            if (!(message.HasCharPrefix('/', ref argPos) ||
                  message.HasMentionPrefix(client.CurrentUser, ref argPos)))
            {
                if (message.Author.Id != client.CurrentUser.Id)
                {
                    ProcessMessage(message);
                }
                return;
            }

            await Task.Run(() =>
            {
                SyncContext.Post(state =>
                {
                    // Create a Command Context
                    var context = new CommandContext(client, message);
                    // Execute the command. (result does not indicate a return value, 
                    // rather an object stating if the command executed successfully)
                    var result = commands.ExecuteAsync(context, argPos, services, MultiMatchHandling.Best);
                    // if (!result.IsSuccess)
                    // {
                    //     await context.Channel.SendMessageAsync(result.ErrorReason);
                    // }
                    result.Wait();
                    if (!result.Result.IsSuccess && !(result.Result is PreconditionResult))
                    {
                        Console.Error.WriteLine(result.Result);                        
                    }
                }, null);
            });
        }

        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
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
            if (message.Channel is IDMChannel)
            {
                _game.OnPrivateMessage(message);
            }
        }
    }
}