using System;
using System.Collections.Generic;

namespace DiscordMafia.Items
{
    public class Antidote : BaseItem
    {
        public override string Name
        {
            get
            {
                return "Антидот";
            }
        }
        public override string Description
        {
            get
            {
                return "Покупая антидот, игрок в ближайшую ночь снимает с себя все эффекты, ведущие к отложенной смерти (например, инфекция путаны или отравление отравителя).";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "антидот",
                    "антидота",
                    "антидоту",
                    "антидот",
                    "антидотом",
                    "антидоте",
                };
            }
        }

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
