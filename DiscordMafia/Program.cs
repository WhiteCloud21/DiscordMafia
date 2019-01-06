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
using DiscordMafia.Client;
using DiscordMafia.Services;

namespace DiscordMafia
{
    internal class Program
    {
        public static BotSynchronizationContext SyncContext = new BotSynchronizationContext();
        private static Game _game;
        public static SqliteConnection Connection;

        public static MainSettings Settings { get; private set; }

        private CommandService commands;
        private DiscordClientWrapper clientWrapper;
        private IServiceProvider services;

        static void Main(string[] args)
        {
            new Program().Start().GetAwaiter().GetResult();
        }

        public async Task Start()
        {
            var settings = new MainSettings("Config/mainSettings.xml", "Config/Local/mainSettings.xml");
            settings.LoadLanguage();

            var connection = new SqliteConnection($"Data Source={settings.DatabasePath};");
            connection.Open();
            Migrate(connection);

            Connection = connection;
            Settings = settings;

            async Task clientReadyHandler()
            {
                commands = new CommandService();

                var serviceCollection = new ServiceCollection();
                serviceCollection.AddSingleton(clientWrapper.MainClient);
                serviceCollection.AddSingleton(clientWrapper);
                serviceCollection.AddSingleton(connection);
                serviceCollection.AddSingleton(settings);
                serviceCollection.AddSingleton(commands);
                serviceCollection.AddSingleton<Services.Notifier>();
                serviceCollection.AddSingleton<System.Threading.SynchronizationContext>(SyncContext);
                serviceCollection.AddSingleton<Game>();
                serviceCollection.AddSingleton<Base.Game.IGame>(p => p.GetRequiredService<Game>());
                serviceCollection.AddSingleton<DIContractResolver>();

                services = serviceCollection.BuildServiceProvider();
                _game = services.GetRequiredService<Game>();
                _game.LoadSettings();

                await InstallCommands();

                await clientWrapper.MainClient.SetGameAsync(null);
                clientWrapper.MainClient.Ready -= clientReadyHandler;
            }

            clientWrapper = new DiscordClientWrapper(settings, clientReadyHandler);

            clientWrapper.MainClient.Log += Log;

            if (clientWrapper.MainClient != clientWrapper.AnnouncerClient)
            {
                clientWrapper.AnnouncerClient.Log += AnnouncerLog;
            }

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
            clientWrapper.MainClient.MessageReceived += HandleCommand;
            // Discover all of the commands in this assembly and load them.
            var wrapper = new CommandsServiceLocalizer();
            await wrapper.AddModulesAsync(Settings, commands, Assembly.GetEntryAssembly());
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
                  message.HasMentionPrefix(clientWrapper.MainClient.CurrentUser, ref argPos)))
            {
                if (message.Author.Id != clientWrapper.MainClient.CurrentUser.Id && message.Author.Id != clientWrapper.AnnouncerClient.CurrentUser.Id)
                {
                    SyncContext.Post(state =>
                    {
                        ProcessMessage(message);
                    }, null);
                }
                return;
            }

            await Task.Run(() =>
            {
                SyncContext.Post(state =>
                {
                    // Create a Command Context
                    var context = new CommandContext(clientWrapper.MainClient, message);
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

        public static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public static Task AnnouncerLog(LogMessage msg)
        {
            Console.WriteLine($"{msg} [Announcer]");
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