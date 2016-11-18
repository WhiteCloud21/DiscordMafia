namespace DiscordMafia.Roles
{
    public class Homeless : UniqueRole
    {
        public override string Name
        {
            get
            {
                return "Бомж";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "бомж",
                    "бомжа",
                    "бомжу",
                    "бомжа",
                    "бомжом",
                    "бомже",
                };
            }
        }

        public override Team Team
        {
            get
            {
                return Team.Civil;
            }
        }

        public InGamePlayerInfo PlayerToCheck { get; set; }
        
        public override void ClearActivity(bool cancel, InGamePlayerInfo onlyAgainstTarget = null)
        {
            if (onlyAgainstTarget == null || PlayerToCheck == onlyAgainstTarget)
            {
                PlayerToCheck = null;
                base.ClearActivity(cancel);
            }
        }

        public override void NightInfo(Game game, InGamePlayerInfo currentPlayer)
        {
            base.NightInfo(game, currentPlayer);
            game.GetAlivePlayersMesssage(true, true, currentPlayer, "/пров");
        }

        public override bool IsReady(GameState currentState)
        {
            switch (currentState)
            {
                case GameState.Night:
                    if (PlayerToCheck == null)
                    {
                        return false;
                    }
                    break;
            }
            return base.IsReady(currentState);
        }
    }
}
