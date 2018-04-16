using DiscordMafia.Client;
using DiscordMafia.Lib;
using DiscordMafia.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordMafia.Services
{
    public class Notifier
    {
        private DiscordClientWrapper _clientWrapper;
        private Game _game;
        private PeriodicTimer _timer = new PeriodicTimer();
        private int[] _timeOfDayBreakpoints = { 11000, 31000, 61000, 121000 };
        private HashSet<int> _passedTimeOfDayBreakpoints = new HashSet<int>();

        public bool IsEnabled { get; private set; }

        private DateTime timeOfDayEnd;

        public Notifier(DiscordClientWrapper clientWrapper)
        {
            _clientWrapper = clientWrapper;
            _timer.Elapsed += TimeOfDayTimerElapsed;
            IsEnabled = _clientWrapper.AnnouncerClient != _clientWrapper.MainClient;
        }

        public void SetGame(Game game)
        {
            _game = game;
        }

        public void SetTimeOfDay(int interval)
        {
            if (!IsEnabled)
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
            if (!IsEnabled)
            {
                return;
            }
            _timer.Stop();
            _passedTimeOfDayBreakpoints.Clear();
        }

        private void TimeOfDayTimerElapsed(object obj)
        {
            if (!IsEnabled)
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
    }
}
