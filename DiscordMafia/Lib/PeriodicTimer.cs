using System;
using System.Threading;

namespace DiscordMafia.Lib
{
    public class PeriodicTimer: IDisposable
    {
        protected System.Threading.Timer InternalTimer;

        public event Action<object> Elapsed;

        public int Interval { get; set; }

        public PeriodicTimer()
        {
            InternalTimer = new System.Threading.Timer(OnElapsed, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Start()
        {
            InternalTimer.Change(Interval, Interval);
        }

        public void Stop()
        {
            InternalTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        protected virtual void OnElapsed(object o)
        {
            Elapsed?.Invoke(o);
        }

        public void Dispose()
        {
            InternalTimer.Dispose();
        }
    }
}