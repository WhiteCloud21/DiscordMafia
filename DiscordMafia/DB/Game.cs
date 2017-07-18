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
        public DateTime StartedAt { get; set; }

        [Column("finished_at"), Required]
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