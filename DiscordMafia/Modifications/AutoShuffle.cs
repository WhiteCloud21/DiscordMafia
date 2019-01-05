using DiscordMafia.Base.Game;
using DiscordMafia.Base.Modifications;
using DiscordMafia.Services;
using System;
using System.Runtime.Serialization;

namespace DiscordMafia.Modifications
{
    public class AutoShuffle : Modification
    {
        public int GameCyclesBeforeShuffle { get; set; } = 3;

        private int CurrentGameCycles { get; set; } = 0;

        private readonly Game _game;
        private readonly Notifier _notifier;
        private readonly RoleAssigner _roleAssigner;

        public AutoShuffle(Game game, Notifier notifier)
        {
            _game = game;
            _game.GameStateChanged += Game_GameStateChanged;
            _notifier = notifier;
            _roleAssigner = new RoleAssigner();
        }

        public override void Dispose()
        {
            _game.GameStateChanged -= Game_GameStateChanged;
        }

        private void Game_GameStateChanged(object sender, Base.Events.Game.GameStateChangedEventArgs e)
        {
            switch (e.NewGameState)
            {
                case Base.GameState.Stopped:
                    CurrentGameCycles = 0;
                    break;
                case Base.GameState.PlayerCollecting:
                    CurrentGameCycles = 0;
                    break;
                case Base.GameState.Morning:
                    CurrentGameCycles++;
                    CurrentGameCycles %= GameCyclesBeforeShuffle;
                    if (CurrentGameCycles == 0)
                    {
                        _roleAssigner.ReassignRoles(_game.PlayersList);
                        _notifier.Welcome();
                    }
                    break;
            }
        }
    
        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            Console.WriteLine("[Modification] AutoShuffle was loaded. GameCyclesBeforeShuffle = {0}", GameCyclesBeforeShuffle);
        }
    }
}
