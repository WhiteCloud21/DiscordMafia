using System.Collections.Generic;
using DiscordMafia.Roles;

namespace DiscordMafia
{
    public class KillManager
    {
        protected ISet<InGamePlayerInfo> killedPlayers = new HashSet<InGamePlayerInfo>();
        protected Game game;

        public KillManager(Game game)
        {
            this.game = game;
        }

        public void Kill(InGamePlayerInfo player)
        {
            killedPlayers.Add(player);
        }

        public void Apply()
        {
            foreach (var player in killedPlayers)
            {
                player.isAlive = false;
                game.messageBuilder.PrepareText("YouKilled").SendPrivate(player);

                if (player.role is Commissioner)
                {
                    var sergeant = game.playersList.Find(p => { return p.isAlive && p.role is Sergeant && !killedPlayers.Contains(p); });
                    if (sergeant != null)
                    {
                        sergeant.role = new Commissioner();
                        game.messageBuilder.PrepareTextReplacePlayer("ComKilled_ToSergeant", player).SendPrivate(sergeant);
                    }
                }
            }
            killedPlayers.Clear();
        }
    }
}
