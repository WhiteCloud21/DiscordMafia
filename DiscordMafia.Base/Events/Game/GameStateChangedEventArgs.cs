using System;

namespace DiscordMafia.Base.Events.Game
{
    public class GameStateChangedEventArgs: EventArgs
    {
        public GameState NewGameState { get; }

        public GameStateChangedEventArgs(GameState newGameState) : base()
        {
            NewGameState = newGameState;
        }
    }
}
