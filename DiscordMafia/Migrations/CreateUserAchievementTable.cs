using System;
using SimpleMigrations;

namespace DiscordMafia.Migrations
{
    [Migration(2)]
    class CreateUserAchievementTable : Migration
    {
        protected override void Up()
        {
            Execute(@"CREATE TABLE IF NOT EXISTS user_achievement (
                id INTEGER PRIMARY KEY  AUTOINCREMENT  NOT NULL,
                user_id INTEGER NOT NULL,
                achievement_id VARCHAR NOT NULL,
                achieved_at INTEGER NOT NULL
            );");
        }

        protected override void Down()
        {
            Execute("DROP TABLE user_achievement");
        }
    }
}
