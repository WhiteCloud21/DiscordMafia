using System.Collections.Generic;
using DiscordMafia.Roles;

namespace DiscordMafia
{
    public class KillManager
    {
        protected ISet<InGamePlayerInfo> KilledPlayers = new HashSet<InGamePlayerInfo>();
        protected Game Game;

        public KillManager(Game game)
        {
            this.Game = game;
        }

        public void Kill(InGamePlayerInfo player)
        {
            KilledPlayers.Add(player);
        }

        public void Apply()
        {
            foreach (var player in KilledPlayers)
            {
                player.IsAlive = false;
                Game.MessageBuilder.PrepareText("YouKilled").SendPrivate(player);

                if (player.Role is Commissioner)
                {
                    var sergeant = Game.PlayersList.Find(p => { return p.IsAlive && p.Role is Sergeant && !KilledPlayers.Contains(p); });
                    if (sergeant != null)
                    {
                        sergeant.Role = new Commissioner();
                        Game.MessageBuilder.PrepareTextReplacePlayer("ComKilled_ToSergeant", player).SendPrivate(sergeant);
                    }
                }
            }
            KilledPlayers.Clear();
        }
    }
}
