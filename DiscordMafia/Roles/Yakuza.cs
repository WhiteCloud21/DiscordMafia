﻿namespace DiscordMafia.Roles
{
    public class Yakuza : BaseRole
    {
        public override Team Team
        {
            get
            {
                return Team.Yakuza;
            }
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
                    if (Player.VoteFor == null)
                    {
                        return false;
                    }
                    break;
            }
            return base.IsReady(currentState);
        }
    }
}
