using System;
using System.Threading;

namespace DiscordMafia.Lib
{
    public class Timer: IDisposable
    {
        protected System.Threading.Timer InternalTimer;

        public event Action<object> Elapsed;

        public int Interval { get; set; }
        protected bool WasElapsed;
        protected object WasElapsedLock = new object();

        public Timer()
        {
            InternalTimer = new System.Threading.Timer(OnElapsed, null, 0, 0);
        }

        public void Start()
        {
            lock (WasElapsedLock)
            {
                WasElapsed = false;
                InternalTimer.Change(Interval, Timeout.Infinite);
            }
        }

        public void Stop()
        {
            InternalTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }
       
        public void SafeChange()
        {
            lock (WasElapsedLock)
            {
                if (!WasElapsed)
                {
                    InternalTimer.Change(Interval, Timeout.Infinite);
                }
            }
        }

        protected virtual void OnElapsed(object o)
        {
            lock (WasElapsedLock)
            {
                Stop();
                if (WasElapsed)
                {
                    return;
                }
                WasElapsed = true;
            }
            Elapsed?.Invoke(o);
        }

        public void Dispose()
        {
            InternalTimer.Dispose();
        }
    }
}