using System;
using SimpleMigrations;

namespace DiscordMafia.Migrations
{
    [Migration(8)]
    class MuteEnabledColumn : Migration
    {
        protected override void Up()
        {
            Execute(@"ALTER TABLE user ADD COLUMN is_mute_enabled BOOL NULL DEFAULT NULL;");
        }

        protected override void Down()
        {
            throw new NotImplementedException("This migration cannot be reverted");
        }
    }
}
