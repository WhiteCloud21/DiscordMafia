using System;
using System.Collections.Generic;

namespace DiscordMafia.Items
{
    public class Mask : BaseItem
    {
        public override int Cost
        {
            get
            {
                return 20;
            }
        }

        public override void Use(InGamePlayerInfo currentPlayer, List<InGamePlayerInfo> playersList)
        {
            foreach (var player in playersList)
            {
                if (player != currentPlayer)
                {
                    if (player.GetItem(new AntiMask(), true) != null)
                    {
                        // AntiMask позволяет действовать сквозь маскировку
                        continue;
                    }
                    player.CancelActivity(currentPlayer);
                }
            }
            IsActive = false;
        }
    }
}
