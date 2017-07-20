using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DiscordMafia.Roles;

namespace DiscordMafia.DB
{
    [Table("game")]
    public class Game
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("id")]
        public ulong Id { get; set; }

        [Column("started_at"), Required]
        public long StartedAtInt { get { return StartedAt.ToBinary(); } set { StartedAt = DateTime.FromBinary(value); } }

        [Column("finished_at"), Required]
        public long FinishedAtInt { get { return FinishedAt.ToBinary(); } set { FinishedAt = DateTime.FromBinary(value); } }

        [NotMapped]
        public DateTime StartedAt { get; set; }

        [NotMapped]
        public DateTime FinishedAt { get; set; }

        [Column("players_count"), Required]
        public int PlayersCount { get; set; }

        [Column("winner"), Required]
        public Team Winner { get; set; }
        
        [Column("game_mode")]
        public string GameMode { get; set; }

        [InverseProperty("Game")]
        public List<GameUser> Users { get; set; }
    }
}