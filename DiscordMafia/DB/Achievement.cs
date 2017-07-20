using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscordMafia.DB
{
    [Table("user_achievement")]
    public class Achievement
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("id")]
        public long Id { get; set; }
        [Column("user_id")]
        public ulong UserId { get; set; }
        [Column("achievement_id")]
        public string AchievementId { get; set; }
        [Column("achieved_at")]
        public long AchievedAtInt { get { return AchievedAt.ToBinary(); } set { AchievedAt = DateTime.FromBinary(value); } }

        [NotMapped]
        public DateTime AchievedAt { get; set; }
    }
}