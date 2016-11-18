namespace DiscordMafia.Roles
{
    public class Killer : UniqueRole
    {
        public override string Name
        {
            get
            {
                return "Киллер";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "киллер",
                    "киллера",
                    "киллеру",
                    "киллера",
                    "киллером",
                    "киллере",
                };
            }
        }

        public override Team Team
        {
            get
            {
                return Team.Mafia;
            }
        }

        public InGamePlayerInfo PlayerToKill { get; set; }

        public override void ClearActivity(bool cancel, InGamePlayerInfo onlyAgainstTarget = null)
        {
            if (onlyAgainstTarget == null || PlayerToKill == onlyAgainstTarget)
            {
                PlayerToKill = null;
                base.ClearActivity(cancel);
            }
        }

        public override void NightInfo(Game game, InGamePlayerInfo currentPlayer)
        {
            base.NightInfo(game, currentPlayer);
            game.GetAlivePlayersMesssage(true, true, currentPlayer, "/kill");
        }

        public override bool IsReady(GameState currentState)
        {
            switch (currentState)
            {
                case GameState.Night:
                    if (PlayerToKill == null)
                    {
                        return false;
                    }
                    break;
            }
            return base.IsReady(currentState);
        }
    }
}
