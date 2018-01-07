using System;
using System.Collections.Generic;

namespace DiscordMafia.Items
{
    public class Antidote : BaseItem
    {
        public override int Cost
        {
            get
            {
                return 5;
            }
        }

        public override void Use(InGamePlayerInfo currentPlayer, List<InGamePlayerInfo> playersList)
        {
            currentPlayer.DelayedDeath = null;
            IsActive = false;
        }
    }
}
