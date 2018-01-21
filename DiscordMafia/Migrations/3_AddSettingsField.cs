using System;
using SimpleMigrations;

namespace DiscordMafia.Migrations
{
    [Migration(3)]
    class AddSettingsField : Migration
    {
        protected override void Up()
        {
            Execute(@"ALTER TABLE user ADD COLUMN settings TEXT;");
        }

        protected override void Down()
        {
            throw new NotImplementedException("This migration cannot be reverted");
        }
    }
}
