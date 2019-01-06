using System;
using DiscordMafia.Base;
using DiscordMafia.Roles.ChuckNorrisSpace;

namespace DiscordMafia.Roles
{
    public class ChuckNorris : UniqueRole, ITargetedRole
    {
        private Random _personalRandomGenerator = new Random();
        private InGamePlayerInfo _playerToInteract;
        private ChuckNorrisAction _action;

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
                _action = GetRandomAction();
                _playerToInteract = value;
            }
        }

        public ChuckNorrisAction Action { get => _action; }

        private ChuckNorrisAction GetRandomAction()
        {

            var values = Enum.GetValues(typeof(ChuckNorrisAction));
            return (ChuckNorrisAction)values.GetValue(_personalRandomGenerator.Next(values.Length));
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
