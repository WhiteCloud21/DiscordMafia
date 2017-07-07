using System;
using System.Threading.Tasks;
using Discord.Commands;
using DiscordMafia.Config;
using DiscordMafia.Roles;

namespace DiscordMafia.Modules
{
    public class HelpModule : ModuleBase
    {
        private Game _game;
        private MainSettings _settings;

        public HelpModule(Game game, MainSettings settings) {
            _game = game;
            _settings = settings;
        }

        [Command("help"), Summary("Выводит справку."), Alias("хелп")]
        public async Task Help()
        {
            var currentPlayer = _game.CurrentPlayers.ContainsKey(Context.User.Id) ? _game.CurrentPlayers[Context.User.Id] : null;
            var message = "<b>==========Игровые команды==========</b>" + Environment.NewLine;
            message += "/help - вывод этой справки (в приват боту);" + Environment.NewLine;
            message += "/join, /я - регистрация в игре (во время набора игроков);" + Environment.NewLine;
            message += "/cancel, /отмена - выход из игры (во время набора игроков);" + Environment.NewLine;
            message += "/mystat, /мойстат - ваша статистика(в приват боту);" + Environment.NewLine;
            message += "/top, /топ - лучшие игроки;" + Environment.NewLine;
            message += "/buy, /купить - посмотреть доступные вещи для покупки(только во время игры, в приват боту);" + Environment.NewLine;
            //message += "/announceon, /предупреждай - сообщать о начале игры(в приват боту);" + Environment.NewLine;
            //message += "/announceoff, /отстань - больше не сообщать о начале игры(в приват боту);" + Environment.NewLine;

            if (currentPlayer != null && currentPlayer.IsAlive && currentPlayer.Role != null)
            {
                message += " " + Environment.NewLine;
                message += "<b>=========== Помощь по статусу===========</b>" + Environment.NewLine;
                message += "Ваш статус - " + _game.MessageBuilder.FormatRole(currentPlayer.Role.Name) + Environment.NewLine;
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
            message += _game.Settings.Roles.RolesHelp();

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
    }
}