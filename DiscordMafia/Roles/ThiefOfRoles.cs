using System;
using DiscordMafia.Base;

namespace DiscordMafia.Roles
{
    public class ThiefOfRoles : UniqueRole, ITargetedRole
    {
        private InGamePlayerInfo _playerToInteract;

        public override Team Team
        {
            get
            {
                return Team.Neutral;
            }
        }

        public InGamePlayerInfo PlayerToInteract
        {
            get => _playerToInteract;
            set
            {
                if (value == Player)
                {
                    throw new ArgumentException("Нельзя воровать у себя.");
                }
                _playerToInteract = value;
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
