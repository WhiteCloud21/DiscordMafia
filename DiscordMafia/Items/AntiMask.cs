using System;
using System.Collections.Generic;

namespace DiscordMafia.Items
{
    public class AntiMask : BaseItem
    {
        public override int Cost
        {
            get
            {
                return 15;
            }
        }

        public override void Use(InGamePlayerInfo currentPlayer, List<InGamePlayerInfo> playersList)
        {
            // Действия самого предмета нет, его наличие проверяется в маскировочном комплекте
            IsActive = false;
        }
    }
}
