using System;
using System.Collections.Generic;

namespace DiscordMafia.Items
{
    public class AntiMask : BaseItem
    {
        public override string Name
        {
            get
            {
                return "Прибор ночного видения";
            }
        }
        public override string Description
        {
            get
            {
                return "Покупая прибор ночного видения, игрок в ближайшую ночь получает способность видеть и действовать сквозь маски. Таким образом, ПНВ является контрпредметом маске.";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "прибор ночного видения",
                    "прибора ночного видения",
                    "прибору ночного видения",
                    "прибор ночного видения",
                    "прибором ночного видения",
                    "приборе ночного видения",
                };
            }
        }

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
