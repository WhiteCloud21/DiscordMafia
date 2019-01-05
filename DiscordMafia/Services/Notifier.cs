using DiscordMafia.Client;
using DiscordMafia.Lib;
using DiscordMafia.Messages;
using DiscordMafia.Roles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordMafia.Services
{
    public class Notifier
    {
        private DiscordClientWrapper _clientWrapper;
        private Game _game;
        private PeriodicTimer _timer = new PeriodicTimer();
        private int[] _timeOfDayBreakpoints = { 11000, 31000, 61000, 121000 };
        private HashSet<int> _passedTimeOfDayBreakpoints = new HashSet<int>();

        public bool IsTimeOfDayNotificationsEnabled { get; private set; }

        private DateTime timeOfDayEnd;

        public Notifier(DiscordClientWrapper clientWrapper)
        {
            _clientWrapper = clientWrapper;
            _timer.Elapsed += TimeOfDayTimerElapsed;
            IsTimeOfDayNotificationsEnabled = _clientWrapper.AnnouncerClient != _clientWrapper.MainClient;
        }

        public void SetGame(Game game)
        {
            _game = game;
        }

        public void SetTimeOfDay(int interval)
        {
            if (!IsTimeOfDayNotificationsEnabled)
            {
                return;
            }
            timeOfDayEnd = DateTime.Now.AddMilliseconds(interval);
            ResetTimeOfDay();
            _timer.Interval = 1000;
            _timer.Start();
        }

        public void ResetTimeOfDay()
        {
            if (!IsTimeOfDayNotificationsEnabled)
            {
                return;
            }
            _timer.Stop();
            _passedTimeOfDayBreakpoints.Clear();
        }

        private void TimeOfDayTimerElapsed(object obj)
        {
            if (!IsTimeOfDayNotificationsEnabled)
            {
                return;
            }
            var remaininguSec = (int)((timeOfDayEnd - DateTime.Now).TotalMilliseconds);
            foreach (var breakpoint in _timeOfDayBreakpoints)
            {
                if (_passedTimeOfDayBreakpoints.Contains(breakpoint))
                {
                    break;
                }
                if (remaininguSec < breakpoint)
                {
                    _passedTimeOfDayBreakpoints.Add(breakpoint);
                    string msg = _game.MessageBuilder.GetTextSimple("timeRemaining", new Dictionary<string, object> { ["seconds"] = remaininguSec / 1000 });
                    _clientWrapper.AnnouncerGameChannel.SplitAndSend(msg);
                    break;
                }
            }
        }

        public void Welcome()
        {
            var mafiaMessage = "";
            var yakuzaMessage = "";
            foreach (var player in _game.PlayersList)
            {
                if (player.IsAlive)
                {
                    var roleWelcomeParam = String.Format("GameStart_Role_{0}", player.Role.GetType().Name);
                    var photoName = player.Role.GetImage(_game.MainSettings.Language);
                    _game.MessageBuilder.PrepareTextReplacePlayer(roleWelcomeParam, player, "GameStart_Role_Default").AddImage(photoName).SendPrivate(player);
                    switch (player.Role.Team)
                    {
                        case Team.Mafia:
                            mafiaMessage += String.Format("{0} - {1} (`{2}`)", _game.MessageBuilder.FormatName(player), _game.MessageBuilder.FormatRole(player.Role.GetName(_game.MainSettings.Language)), player.GetName()) + Environment.NewLine;
                            break;
                        case Team.Yakuza:
                            yakuzaMessage += String.Format("{0} - {1} (`{2}`)", _game.MessageBuilder.FormatName(player), _game.MessageBuilder.FormatRole(player.Role.GetName(_game.MainSettings.Language)), player.GetName()) + Environment.NewLine;
                            break;
                    }
                    if (player.Role is Sergeant)
                    {
                        var commissioner = (from p in _game.PlayersList where p.Role is Commissioner select p).FirstOrDefault();
                        if (commissioner != null)
                        {
                            _game.MessageBuilder.PrepareTextReplacePlayer("CheckStatus", commissioner).SendPrivate(player);
                            _game.MessageBuilder.PrepareTextReplacePlayer("CheckStatus", player).SendPrivate(commissioner);
                        }
                    }
                }
            }
            _game.Pause();

            // Состав мафий
            _game.MessageBuilder.PrepareText("MafiaWelcome", new Dictionary<string, object>
            {
                ["players"] = mafiaMessage
            }).SendToTeam(Team.Mafia);
            _game.MessageBuilder.PrepareText("YakuzaWelcome", new Dictionary<string, object>
            {
                ["players"] = yakuzaMessage
            }).SendToTeam(Team.Yakuza);
        }
    }
}
