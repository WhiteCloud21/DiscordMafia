using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscordMafia.DB
{
    [Table("user_achievement")]
    public class Achievement
    {
        [Key, Column("id")]
        public long Id { get; set; }
        [Column("user_id")]
        public ulong UserId  { get; set; }
        [Column("achievement_id")]
        public string AchievementId  { get; set; }
        [Column("achieved_at")]
        public DateTime AchievedAt  { get; set; }
    }
}