using DiscordMafia.Base;
using System.Collections.Generic;

namespace DiscordMafia.Roles
{
    public class Wench : UniqueRole, ITargetedRole
    {
        public override Team Team
        {
            get
            {
                return Team.Civil;
            }
        }

        private InGamePlayerInfo playerToCheck;
        private InGamePlayerInfo _lastPlayerToCheck;

        public InGamePlayerInfo PlayerToInteract
        {
            get { return playerToCheck; }
            set
            {
                if (value != null && value == LastPlayerToCheck)
                {
                    throw new System.ArgumentException("Нельзя ходить к одному игроку 2 раза подряд.");
                }
                if (value == Player)
                {
                    throw new System.ArgumentException("Нельзя ходить к себе.");
                }
                playerToCheck = value;
            }
        }
        public InGamePlayerInfo LastPlayerToCheck {
            get => _lastPlayerToCheck;
            set
            {
                _lastPlayerToCheck = value;
                BlockedPlayers.Add(_lastPlayerToCheck);
            }
        }

        public HashSet<InGamePlayerInfo> BlockedPlayers { get; } = new HashSet<InGamePlayerInfo>();

        public override void ClearActivity(bool cancel, InGamePlayerInfo onlyAgainstTarget = null)
        {
            if (onlyAgainstTarget == null || PlayerToInteract == onlyAgainstTarget)
            {
                PlayerToInteract = null;
            }
            base.ClearActivity(cancel, onlyAgainstTarget);
        }

        public override void OnNightStart(Game game, InGamePlayerInfo currentPlayer)
        {
            base.OnNightStart(game, currentPlayer);
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
