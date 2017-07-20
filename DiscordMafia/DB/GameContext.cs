using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiscordMafia.DB
{
    public class GameContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<GameUser> GameUsers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(Program.Connection.ConnectionString);
#if DEBUG
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.UseLoggerFactory((new LoggerFactory()).AddConsole());
#endif
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GameUser>()
                .HasKey(gu => new {gu.GameId, gu.UserId});
            
            modelBuilder.Entity<GameUser>()
                .HasOne(u => u.Game)
                .WithMany(g => g.Users)
                .HasForeignKey(gu => gu.GameId);
            
            modelBuilder.Entity<GameUser>()
                .HasOne(u => u.User)
                .WithMany(u => u.Games)
                .HasForeignKey(gu => gu.UserId);
        }
    }
}