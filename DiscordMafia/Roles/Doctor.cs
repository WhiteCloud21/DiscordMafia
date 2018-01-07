﻿using DiscordMafia.Activity;

namespace DiscordMafia.Roles
{
    public class Doctor : UniqueRole, ITargetedRole
    {
        public override Team Team
        {
            get
            {
                return Team.Civil;
            }
        }

        private HealActivity healActivity;
        private bool wasHealed = false;
        public InGamePlayerInfo PlayerToInteract
        {
            get { return healActivity?.Patient; }
            set {
                if (value != null && value == LastPlayerToHeal)
                {
                    throw new System.ArgumentException("Нельзя лечить одного игрока 2 раза подряд.");
                }
                if (wasHealed && value == Player)
                {
                    throw new System.ArgumentException("Себя можно лечить только один раз за игру.");
                }
                healActivity?.Cancel();
                healActivity = null;
                if (value != null)
                {
                    healActivity = new HealActivity(Player, value);
                }
            }
        }

        private InGamePlayerInfo lastPlayerToHeal;
        public InGamePlayerInfo LastPlayerToHeal {
            get { return lastPlayerToHeal; }
            set
            {
                lastPlayerToHeal = value;
                if (value == Player)
                {
                    wasHealed = true;
                }
            }
        }

        public override void ClearActivity(bool cancel, InGamePlayerInfo onlyAgainstTarget = null)
        {
            if (onlyAgainstTarget == null || PlayerToInteract == onlyAgainstTarget)
            {
                PlayerToInteract = null;
            }
            base.ClearActivity(cancel, onlyAgainstTarget);
        }

        public override void NightInfo(Game game, InGamePlayerInfo currentPlayer)
        {
            base.NightInfo(game, currentPlayer);
            game.SendAlivePlayersMesssage(currentPlayer);
        }

        public override bool IsReady(GameState currentState)
        {
            switch (currentState)
            {
                case GameState.Night:
                    if (PlayerToInteract == null)
                    {
                        return false;
                    }
                    break;
            }
            return base.IsReady(currentState);
        }
    }
}
