using System;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordMafia.Config;
using DiscordMafia.DB;
using DiscordMafia.Roles;
using Microsoft.Data.Sqlite;

namespace DiscordMafia.Modules
{
    [Group("top"), Alias("топ"), Summary("Команды рейтинга")]
    public class RatingModule : BaseModule
    {
        private const int PerPage = 20;

        private const string PointsField = "total_points as _data";
        private const string RateField = "rate as _data";
        private const string GamesField = "games as _data";
        private const string SurvivabilityField = "(case when games > 0 then (100.0 * survivals / games) else 0.0 end) as _data";

        private Game _game;
        private MainSettings _settings;

        private SqliteConnection _connection;

        public RatingModule(Game game, SqliteConnection connection, MainSettings settings)
        {
            _game = game;
            _settings = settings;
            _connection = connection;
        }

        [Command, Priority(-100), Summary("Топ по умолчанию (может меняться)")]
        public async Task Default([Summary("Страница топа")] int page = 1)
        {
            await DrawTop(PointsField, page);
        }

        [Command("points"), Summary("Топ по очкам"), Alias("очков"), Priority(100)]
        public async Task PointsTop([Summary("Страница топа")] int page = 1)
        {
            await DrawTop(PointsField, page);
        }

        [Command("rate"), Summary("Топ по рейтингу"), Alias("rating", "рейтинга", "рейтинг", "рейтинги"), Priority(100)]
        public async Task RateTop([Summary("Страница топа")] int page = 1)
        {
            await DrawTop(RateField, page);
        }

        [Command("games"), Summary("Топ по играм"), Alias("игра", "игр", "игры"), Priority(100)]
        public async Task GamesTop([Summary("Страница топа")] int page = 1)
        {
            await DrawTop(GamesField, page);
        }

        [Command("survivals"), Summary("Топ по выживаемости"), Alias("выживших", "выживания", "выживающих", "живучих"), Priority(100)]
        public async Task SurvivalsTop([Summary("Страница топа")] int page = 1)
        {
            await DrawTop(SurvivabilityField, page);
        }

        private int GetPlayerCount()
        {
            var command = _connection.CreateCommand();
            command.CommandText = "SELECT count(id) FROM user";
            int rowCount = 0;
            rowCount = Convert.ToInt32(command.ExecuteScalar());
            return rowCount;
        }

        private async Task DrawTop(string field, int page = 1)
        {
            string name = "";
            switch (field)
            {
                default: field = PointsField; name = "очкам"; break;
                case SurvivabilityField: name = "выживаемости"; break;
                case PointsField: name = "очкам"; break;
                case RateField: name = "рейтингу"; break;
                case GamesField: name = "играм"; break;
            }

            var total = this.GetPlayerCount();

            var builder = new EmbedBuilder();

            int width = (int)Math.Log10(total) + 1;
            int maxPage = (total - 1) / PerPage + 1;
            builder.WithColor(new Color(114, 137, 218));
            page = Math.Max(Math.Min(page, maxPage), 1);

            var limit = Math.Min(Math.Max(PerPage, 1), 300);
            var offset = (Math.Max(page, 1) - 1) * PerPage;


            var parameters = new SqliteParameter[] { new SqliteParameter(":limit", limit), new SqliteParameter(":offset", offset) };
            var command = _connection.CreateCommand();
            command.CommandText = $"SELECT id, username, {field} FROM user ORDER BY _data DESC, username ASC LIMIT :limit OFFSET :offset";
            command.Parameters.AddRange(parameters);

            string title = $"Лучшие игроки по {name} (страница {page}/{maxPage})";
            builder.WithTitle(title);

            StringBuilder rating = new StringBuilder();
            var index = (page - 1) * PerPage;

            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                ++index;
                var Id = ulong.Parse(reader.GetValue(0).ToString());
                var Username = reader.GetString(1);
                if (String.IsNullOrWhiteSpace(Username))
                {
                    Username = '(' + Id.ToString() + ')';
                }

                string point = "";
                switch (field)
                {
                    case PointsField: point = reader.GetInt64(2).ToString(); break;
                    case RateField: point = reader.GetDouble(2).ToString("0.00"); break;
                    case GamesField: point = reader.GetInt32(2).ToString(); break;
                    case SurvivabilityField: point = reader.GetDouble(2).ToString("0.00"); break;
                }
                rating.AppendFormat("`{0}. {1} - {2}`", index.ToString().PadLeft(width, '0'), LimitLength(MessageBuilder.Encode(Username), 27), point);
                rating.AppendLine();
            }

            string rateName = "";
            switch (field)
            {
                case SurvivabilityField: rateName = "% выживания"; break;
                case PointsField: rateName = "Очки"; break;
                case RateField: rateName = "Рейтинг"; break;
                case GamesField: rateName = "Игр"; break;
            }

            builder.AddField(x =>
                {
                    x.Name = "".PadLeft(width, '#') + ". Игрок - " + rateName;
                    x.Value = rating.ToString();
                    x.IsInline = true;
                });
            await ReplyAsync("", embed: builder.Build());
        }

        private string LimitLength(string value, int length)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= length ? value.PadRight(length) + ' ' : value.Substring(0, length) + '…';
        }
    }
}