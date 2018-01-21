using System;
using SimpleMigrations;

namespace DiscordMafia.Migrations
{
    [Migration(1)]
    class CreateUserTable : Migration
    {
        protected override void Up()
        {
            Execute(@"CREATE TABLE IF NOT EXISTS user (
                id INTEGER UNSIGNED PRIMARY KEY  NOT NULL  DEFAULT (null),
                username TEXT NOT NULL,
                first_name TEXT NOT NULL,
                last_name TEXT NOT NULL,
                total_points INTEGER NOT NULL  DEFAULT (0),
                wins INTEGER NOT NULL  DEFAULT (0),
                draws INTEGER NOT NULL  DEFAULT (0),
                rate FLOAT NOT NULL  DEFAULT (0),
                games INTEGER NOT NULL  DEFAULT (0),
                is_registered BOOL NOT NULL  DEFAULT (0),
                survivals INTEGER NOT NULL  DEFAULT (0)
            );");
        }

        protected override void Down()
        {
            Execute("DROP TABLE user");
        }
    }
}
