using DiscordMafia.Base.Game;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace DiscordMafia.Base.Modifications
{
    [Serializable]
    public abstract class Modification: IDisposable
    {
        public abstract void Dispose();
    }
}
