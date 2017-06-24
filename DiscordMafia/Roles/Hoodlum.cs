namespace DiscordMafia.Roles
{
    public class Hoodlum : UniqueRole
    {
        public override string Name
        {
            get
            {
                return "Громила";
            }
        }

        public override string[] NameCases
        {
            get
            {
                return new string[] {
                    "громила",
                    "громилы",
                    "громиле",
                    "громилу",
                    "громилой",
                    "громиле",
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

        private InGamePlayerInfo playerToBlock;
        public InGamePlayerInfo PlayerToBlock
        {
            get { return playerToBlock; }
            set {
                if (value != null && value == LastPlayerToBlock)
                {
                    throw new System.ArgumentException("Нельзя блокировать одного игрока 2 раза подряд.");
                }
                if (value == Player)
                {
                    throw new System.ArgumentException("Нельзя блокировать себя.");
                }
                playerToBlock = value;
            }
        }
        public InGamePlayerInfo LastPlayerToBlock { get; set; }
        
        public override void ClearActivity(bool cancel, InGamePlayerInfo onlyAgainstTarget = null)
        {
            if (onlyAgainstTarget == null || PlayerToBlock == onlyAgainstTarget)
            {
                PlayerToBlock = null;
                base.ClearActivity(cancel);
            }
        }

        public override void NightInfo(Game game, InGamePlayerInfo currentPlayer)
        {
            base.NightInfo(game, currentPlayer);
            game.GetAlivePlayersMesssage(true, true, currentPlayer, "/блок");
        }

        public override bool IsReady(GameState currentState)
        {
            switch (currentState)
            {
                case GameState.Night:
                    if (PlayerToBlock == null)
                    {
                        return false;
                    }
                    break;
            }
            return base.IsReady(currentState);
        }

        public override bool HasActivityAgainst(InGamePlayerInfo target)
        {
            return target == PlayerToBlock;
        }
    }
}
