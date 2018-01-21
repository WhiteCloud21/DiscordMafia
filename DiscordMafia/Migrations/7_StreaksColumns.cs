using System;
using SimpleMigrations;

namespace DiscordMafia.Migrations
{
    [Migration(7)]
    class StreaksColumns : Migration
    {
        protected override void Up()
        {
            Execute(@"ALTER TABLE user ADD COLUMN win_streak INTEGER NOT NULL DEFAULT 0;");
            Execute(@"ALTER TABLE user ADD COLUMN lose_streak INTEGER NOT NULL DEFAULT 0;");
        }

        protected override void Down()
        {
            throw new NotImplementedException("This migration cannot be reverted");
        }
    }
}
