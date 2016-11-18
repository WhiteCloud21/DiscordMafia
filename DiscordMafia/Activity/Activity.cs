using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMafia.Activity
{
    public class BaseActivity
    {
        public EventHandler Canceled;
        protected WeakReference<InGamePlayerInfo> player;
        public bool IsCanceled { get; protected set; }

        public InGamePlayerInfo Player
        {
            get
            {
                InGamePlayerInfo target;
                if (player.TryGetTarget(out target))
                {
                    return target;
                }
                return null;
            }
        }

        public BaseActivity(InGamePlayerInfo player)
        {
            IsCanceled = false;
            this.player = new WeakReference<InGamePlayerInfo>(player);
        }

        protected virtual void OnCancel(InGamePlayerInfo onlyAgainstTarget)
        {
            if (onlyAgainstTarget == null)
            {
                Canceled?.Invoke(this, EventArgs.Empty);
                IsCanceled = true;
            }
        }

        public void Cancel(InGamePlayerInfo onlyAgainstTarget = null)
        {
            OnCancel(onlyAgainstTarget);
        }
    }
}
