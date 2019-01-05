using System;

namespace DiscordMafia.Base.Modifications
{
    [Serializable]
    public abstract class Modification: IDisposable
    {
        public abstract void Dispose();
    }
}
