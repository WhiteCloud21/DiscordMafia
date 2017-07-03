using System;
using System.Collections.Generic;
using System.Threading;

namespace DiscordMafia
{
    public class BotSynchronizationContext: SynchronizationContext
    {
        private readonly Queue<Action> _messagesToProcess = new Queue<Action>();
        private readonly object _syncHandle = new object();
        private bool _isRunning = true;

        public override void Send(SendOrPostCallback codeToRun, object state)
        {
            throw new NotImplementedException();
        }

        public override void Post(SendOrPostCallback codeToRun, object state)
        {
            lock (_syncHandle)
            {
                _messagesToProcess.Enqueue(() => codeToRun(state));
                SignalContinue();
            }
        }

        public void RunMessagePump()
        {
            while (CanContinue())
            {
                Action nextToRun = GrabItem();
                nextToRun();
            }
        }

        private Action GrabItem()
        {
            lock (_syncHandle)
            {
                while (CanContinue() && _messagesToProcess.Count == 0)
                {
                    Monitor.Wait(_syncHandle);
                }
                return _messagesToProcess.Dequeue();
            }
        }

        private bool CanContinue()
        {
            lock (_syncHandle)
            {
                return _isRunning;
            }
        }

        public void Cancel()
        {
            lock (_syncHandle)
            {
                _isRunning = false;
                SignalContinue();
            }
        }

        private void SignalContinue()
        {
            Monitor.Pulse(_syncHandle);
        }
    }
}
