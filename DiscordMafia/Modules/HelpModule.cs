using System;
using System.Threading.Tasks;
using Discord.Commands;
using DiscordMafia.Config;
using DiscordMafia.Roles;
using System.Linq;
using DiscordMafia.Preconditions;
using System.Collections.Generic;

namespace DiscordMafia.Modules
{
    [Summary("Команды помощи")]
    public class HelpModule : BaseModule
    {
        private Game _game;
        private MainSettings _settings;
        private CommandService _commandService;

        public HelpModule(Game game, MainSettings settings, CommandService commandService) {
            _game = game;
            _settings = settings;
            _commandService = commandService;
        }

        [Command("help"), Summary("Выводит справку."), Alias("хелп")]
        public async Task Help()
        {
            var currentPlayer = _game.CurrentPlayers.ContainsKey(Context.User.Id) ? _game.CurrentPlayers[Context.User.Id] : null;
            var message = "<b>==========Игровые команды==========</b>" + Environment.NewLine;
            message += "/help - вывод этой справки (в приват боту);" + Environment.NewLine;
            message += "/list - список команд;" + Environment.NewLine;
            message += "/help КОМАНДА - помощь по команде;" + Environment.NewLine;

            if (currentPlayer != null && currentPlayer.IsAlive && currentPlayer.Role != null)
            {
                message += " " + Environment.NewLine;
                message += "<b>=========== Помощь по статусу===========</b>" + Environment.NewLine;
                message += "Ваш статус - " + _game.MessageBuilder.FormatRole(currentPlayer.Role.GetName(_game.Settings.Language)) + Environment.NewLine;
                switch (currentPlayer.Role.Team)
                {
                    case Team.Civil:
                        message += "Вы играете за команду мирных жителей" + Environment.NewLine;
                        break;
                    case Team.Neutral:
                        message += "Вы играете сами за себя" + Environment.NewLine;
                        break;
                    case Team.Mafia:
                        message += "Вы играете за команду мафов" + Environment.NewLine;
                        break;
                    case Team.Yakuza:
                        message += "Вы играете за команду якудз" + Environment.NewLine;
                        break;
                }

                message += _game.MessageBuilder.GetText(string.Format("RoleHelp_{0}", currentPlayer.Role.GetType().Name)) + Environment.NewLine;
            }

            message += " " + Environment.NewLine;
            message += "<b>========Помощь по режиму игры========</b>" + Environment.NewLine;
            message += $"Текущий режим игры: {_game.Settings.GameType}" + Environment.NewLine;
            message += $"Якудза: {_game.Settings.IsYakuzaEnabled}" + Environment.NewLine;
            message += $"Мафов из каждой группировки: {_game.Settings.MafPercent}%" + Environment.NewLine;
            message += "<u><b>Доступные роли</b></u>" + Environment.NewLine;
            message += _game.Settings.Roles.RolesHelp(_game.Settings.Language);

            message += " " + Environment.NewLine;
            message += "<b>======Помощь по начислению очков======</b>" + Environment.NewLine;
            foreach (var pointConfig in _game.Settings.Points.Values)
            {
                message += $"{pointConfig.Description}: {pointConfig.Points}" + Environment.NewLine;
            }

            _game.MessageBuilder.Text(message, false).SendPrivate(Context.User);
            // ReplyAsync is a method on ModuleBase
            // await ReplyAsync(message);
            await Task.CompletedTask;
        }

        [Command("help"), Summary("Выводит справку по команде."), Alias("хелп")]
        protected async Task Help([Remainder] string commandName)
        {
            string message = "";
            foreach (var moduleInfo in _commandService.Modules)
            {
                message += ParseModuleForHelp(commandName, moduleInfo, true);
            }
            if (!string.IsNullOrEmpty(message))
            {
                _game.MessageBuilder.Text(message, false).SendPrivate(Context.User);
                await Task.CompletedTask;
                // await ReplyAsync(message);
            }
            else
            {
                _game.MessageBuilder.Text("Команда не найдена", false).SendPrivate(Context.User);
                await Task.CompletedTask;
                // await ReplyAsync(message);
            }
        }

        [Command("list"), Summary("Выводит список команд."), Alias("список")]
        protected async Task List()
        {
            string message = "";
            foreach (var moduleInfo in _commandService.Modules)
            {
                message += ParseModuleForHelp(null, moduleInfo, false);
            }
            if (!string.IsNullOrEmpty(message))
            {
                _game.MessageBuilder.Text(message, false).SendPrivate(Context.User);
                await Task.CompletedTask;
                // await ReplyAsync(message);
            }
        }

        protected string ParseModuleForHelp(string commandName, ModuleInfo moduleInfo, bool isFullHelp)
        {
            var info = "";
            if (commandName == null || moduleInfo.Aliases.Contains(commandName))
            {
                foreach (var command in moduleInfo.Commands)
                {
                    info += isFullHelp ? GetCommandHelp(command) : GetCommandBaseHelp(command);
                }
            }
            else
            {
                foreach (var command in moduleInfo.Commands)
                {
                    if (commandName == null || command.Aliases.Contains(commandName))
                    {
                        info += isFullHelp ? GetCommandHelp(command) : GetCommandBaseHelp(command);
                    }
                }
            }
            foreach (var subModule in moduleInfo.Submodules)
            {
                info += ParseModuleForHelp(commandName, subModule, isFullHelp);
            }
            if (!string.IsNullOrEmpty(info))
            {
                if (!string.IsNullOrEmpty(moduleInfo.Summary))
                {
                    info = $"{Environment.NewLine}__**{moduleInfo.Summary}**__{Environment.NewLine}" + info;
                }
            }
            return info;
        }

        protected string GetCommandHelp(CommandInfo commandInfo)
        {
            var paramsInfo = "";
            var info = $"**/{commandInfo.Aliases[0]}** ";
            // TODO Нужно придумать почище
            var preconditionsInfo = new List<string>();
            foreach (var precondition in commandInfo.Preconditions)
            {
                if (precondition is RequireAdminAttribute)
                {
                    preconditionsInfo.Add("для администратора");
                }
                else if (precondition is RequirePlayerAttribute)
                {
                    preconditionsInfo.Add("во время игры");
                }
            }
            foreach (var paramInfo in commandInfo.Parameters)
            {
                if (paramInfo.Name == "ignored")
                {
                    continue;
                }
                if (paramInfo.IsOptional)
                {
                    info += '[';
                }
                info += paramInfo.Name;

                if (paramInfo.DefaultValue != null)
                {
                    info += $" = {paramInfo.DefaultValue}";
                }
                if (paramInfo.IsOptional)
                {
                    info += ']';
                }
                info += ' ';
                paramsInfo += $"  **{paramInfo.Name}** - {paramInfo.Summary}";
                if (paramInfo.IsRemainder)
                {
                    paramsInfo += $"(до конца строки) ";
                }
                paramsInfo += Environment.NewLine;
            }
            info += $"{Environment.NewLine}";
            info += "Варианты: " + string.Join(", ", from a in commandInfo.Aliases select $"*{a}*") + Environment.NewLine;
            if (preconditionsInfo.Count > 0)
            {
                info += "Условия: " + string.Join(", ", preconditionsInfo) + Environment.NewLine;
            }
            info += $"{commandInfo.Summary}{Environment.NewLine}{paramsInfo}{Environment.NewLine}";
            return info;
        }

        protected string GetCommandBaseHelp(CommandInfo commandInfo)
        {
            var info = $"**/{commandInfo.Aliases[0]}** ";
            foreach (var paramInfo in commandInfo.Parameters)
            {
                if (paramInfo.Name == "ignored")
                {
                    continue;
                }
                if (paramInfo.IsOptional)
                {
                    info += '[';
                }
                info += paramInfo.Name;

                if (paramInfo.DefaultValue != null)
                {
                    info += $" = {paramInfo.DefaultValue}";
                }
                if (paramInfo.IsOptional)
                {
                    info += ']';
                }
                info += ' ';
            }
            info += $" - {commandInfo.Summary}{Environment.NewLine}{Environment.NewLine}";
            return info;
        }
    }
}