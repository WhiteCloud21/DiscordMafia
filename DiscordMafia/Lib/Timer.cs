using System;
using System.Threading;

namespace DiscordMafia.Lib
{
    public class Timer: IDisposable
    {
        protected System.Threading.Timer InternalTimer;

        public event Action<object> Elapsed;

        public int Interval { get; set; }

        public Timer()
        {
            InternalTimer = new System.Threading.Timer(OnElapsed, null, 0, 0);
        }

        public void Start()
        {
            InternalTimer.Change(Interval, Timeout.Infinite);
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