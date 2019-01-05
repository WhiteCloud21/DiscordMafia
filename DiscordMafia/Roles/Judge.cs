using DiscordMafia.Activity;
using DiscordMafia.Base;

namespace DiscordMafia.Roles
{
    public class Judge : UniqueRole, ITargetedRole
    {
        public override Team Team
        {
            get
            {
                return Team.Civil;
            }
        }

        private JustifyActivity justifyActivity;
        public InGamePlayerInfo PlayerToInteract
        {
            get { return justifyActivity?.Accused; }
            set
            {
                if (value != null && value == LastPlayerToJustufy)
                {
                    throw new System.ArgumentException("Нельзя оправдать одного игрока второй раз подряд.");
                }
                if (value == Player)
                {
                    throw new System.ArgumentException("Нельзя оправдать себя.");
                }
                justifyActivity?.Cancel();
                justifyActivity = null;
                if (value != null)
                {
                    justifyActivity = new JustifyActivity(Player, value);
                }
            }
        }

        public InGamePlayerInfo LastPlayerToJustufy { get; set; }

        public override void ClearActivity(bool cancel, InGamePlayerInfo onlyAgainstTarget = null)
        {
            if (onlyAgainstTarget == null || PlayerToInteract == onlyAgainstTarget)
            {
                PlayerToInteract = null;
            }
            base.ClearActivity(cancel, onlyAgainstTarget);
        }

        public override void OnDayStart(Game game, InGamePlayerInfo currentPlayer)
        {
            base.OnDayStart(game, currentPlayer);
            game.SendAlivePlayersMesssage(currentPlayer);
        }

        public override bool IsReady(GameState currentState)
        {
            switch (currentState)
            {
                case GameState.Evening:
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
