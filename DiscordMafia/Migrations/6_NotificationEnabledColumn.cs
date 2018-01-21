using System;
using SimpleMigrations;

namespace DiscordMafia.Migrations
{
    [Migration(6)]
    class NotificationEnabledColumn : Migration
    {
        protected override void Up()
        {
            Execute(@"ALTER TABLE user ADD COLUMN is_notification_enabled BOOL NOT NULL  DEFAULT 0;");
        }

        protected override void Down()
        {
            throw new NotImplementedException("This migration cannot be reverted");
        }
    }
}
