using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMafia.TypeReaders
{
    public class InGamePlayerInfoTypeReader : TypeReader
    {
        public override async Task<TypeReaderResult> Read(ICommandContext context, string input, IServiceProvider services)
        {
            var game = services.GetService(typeof(Game)) as Game;
            var results = new Dictionary<ulong, TypeReaderValue>();

            //By Game number (2.0)
            if (int.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out int gameNumber))
            {
                AddResult(results, game.GetPlayerInfo(gameNumber), 2.0f, game);
            }

            //By Mention (1.0)
            if (MentionUtils.TryParseUser(input, out ulong id))
            {
                if (context.Guild != null)
                    AddResult(results, await context.Guild.GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false), 1.00f, game);
                else
                    AddResult(results, await context.Channel.GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false), 1.00f, game);
            }

            //By Id (0.9)
            if (ulong.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out id))
            {
                if (context.Guild != null)
                    AddResult(results, await context.Guild.GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false), 0.90f, game);
                else
                    AddResult(results, await context.Channel.GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false), 0.90f, game);
            }

            //By Username + Discriminator (0.7-0.85)
            int index = input.LastIndexOf('#');
            if (index >= 0)
            {
                string username = input.Substring(0, index);
                if (ushort.TryParse(input.Substring(index + 1), out ushort discriminator))
                {
                    var gameUser = game.PlayersList.FirstOrDefault(x => x.User.DiscordUser?.DiscriminatorValue == discriminator &&
                        string.Equals(username, x?.User.DiscordUser?.Username, StringComparison.OrdinalIgnoreCase));
                    AddResult(results, gameUser, gameUser?.User.DiscordUser?.Username == username ? 0.85f : 0.75f, game);
                }
            }

            //By Username (0.5-0.6)
            {
                foreach (var gameUser in game.PlayersList.Where(x => string.Equals(input, x.User.DiscordUser?.Username, StringComparison.OrdinalIgnoreCase)))
                    AddResult(results, gameUser, gameUser.User.DiscordUser?.Username == input ? 0.65f : 0.55f, game);
            }

            //By Nickname (0.5-0.6)
            {
                foreach (var gameUser in game.PlayersList.Where(x => string.Equals(input, x.User?.Username, StringComparison.OrdinalIgnoreCase)))
                    AddResult(results, gameUser, gameUser.User?.Username == input ? 0.65f : 0.55f, game);
            }

            if (results.Count > 0)
                return TypeReaderResult.FromSuccess(results.Values.ToImmutableArray());
            return TypeReaderResult.FromError(CommandError.ObjectNotFound, "User not found.");
        }

        private void AddResult(Dictionary<ulong, TypeReaderValue> results, IUser user, float score, Game game)
        {
            if (user != null)
            {
                AddResult(results, game.GetPlayerInfo(user.Id), score, game);
            }
        }

        private void AddResult(Dictionary<ulong, TypeReaderValue> results, InGamePlayerInfo player, float score, Game game)
        {
            if (player != null && !results.ContainsKey(player.User.Id))
                results.Add(player.User.Id, new TypeReaderValue(player, score));
        }
    }
}
