namespace DiscordMafia.Roles
{
    public class Yakuza : BaseRole
    {
        public override string Name
        {
            get
            {
                return "Якудза";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "якудза",
                    "якудзы",
                    "якудзе",
                    "якудзу",
                    "якудзой",
                    "якудзе",
                };
            }
        }

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
            game.GetAlivePlayersMesssage(true, true, currentPlayer, "/kill");
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
