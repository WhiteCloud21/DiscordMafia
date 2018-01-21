using SimpleMigrations;

namespace DiscordMafia.Migrations
{
    [Migration(4)]
    class CreateGameTable : Migration
    {
        protected override void Up()
        {
            Execute(@"CREATE TABLE game (
                id INTEGER PRIMARY KEY AUTOINCREMENT  NOT NULL,
                started_at INTEGER NOT NULL,
                finished_at INTEGER NOT NULL, 
                players_count INTEGER NOT NULL, 
                winner INTEGER NOT NULL,
                game_mode VARCHAR NULL
            );");
            
            Execute(@"CREATE TABLE game_user (
                game_id INTEGER NOT NULL, 
                user_id INTEGER NOT NULL,
                role VARCHAR NOT NULL, 
                start_role VARCHAR NOT NULL, 
                result INTEGER NOT NULL , 
                score INTEGER NOT NULL  DEFAULT 0,
                rating_after_game INTEGER NOT NULL DEFAULT 0, PRIMARY KEY (game_id, user_id)
            );");
        }

        protected override void Down()
        {
            Execute("DROP TABLE game");
            Execute("DROP TABLE game_user");
        }
    }
}
