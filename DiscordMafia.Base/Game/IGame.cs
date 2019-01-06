using DiscordMafia.Base.Events.Game;
using System;

namespace DiscordMafia.Base.Game
{
    public interface IGame
    {
        event EventHandler<GameStateChangedEventArgs> GameStateChanged;
    }
}
