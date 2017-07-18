using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscordMafia.DB
{
    [Table("game_user")]
    public class GameUser
    {
        [Key, Column("game_id")]
        public ulong GameId { get; set; }

        [Key, Column("user_id")]
        public ulong UserId { get; set; }

        [Column("role")]
        public string Role { get; set; }

        [Column("start_role")]
        public string StartRole { get; set; }

        [Column("result")]
        public ResultFlags Result { get; set; }

        [Column("score")]
        public long Score { get; set; }

        [Column("rating_after_game")]
        public double RatingAfterGame { get; set; }

        [ForeignKey("game_id"), Required]
        public Game Game { get; set; }

        [ForeignKey("user_id"), Required]
        public User User { get; set; }

        [Flags]
        public enum ResultFlags
        {
            Win = 0b1,
            Draw = 0b10,
            Survive = 0b100,
        }
    }
}