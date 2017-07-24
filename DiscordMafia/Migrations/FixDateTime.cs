using System;
using SimpleMigrations;

namespace DiscordMafia.Migrations
{
    [Migration(5)]
    class FixDateTime : Migration
    {
        protected override void Up()
        {
            var command = Connection.CreateCommand();
            command.CommandText = "SELECT id, achieved_at FROM user_achievement";
            using (var reader = command.ExecuteReader())
            {
                command = Connection.CreateCommand();
                command.CommandText = "UPDATE user_achievement SET achieved_at = :achievedAt WHERE id = :id";
                while (reader.Read())
                {
                    var dt = DateTime.FromBinary(reader.GetInt64(1));
                    command.Parameters.Clear();
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = ":id";
                    parameter.Value = reader.GetInt64(0);
                    command.Parameters.Add(parameter);
                    parameter = command.CreateParameter();
                    parameter.ParameterName = ":achievedAt";
                    parameter.Value = dt.Ticks;
                    command.Parameters.Add(parameter);
                    command.ExecuteNonQuery();
                }
            }

            command.CommandText = "SELECT id, started_at, finished_at FROM game";
            using (var reader = command.ExecuteReader())
            {
                command = Connection.CreateCommand();
                command.CommandText = "UPDATE game SET started_at = :startedAt, finished_at = :finishedAt WHERE id = :id";
                while (reader.Read())
                {
                    var dt = DateTime.FromBinary(reader.GetInt64(1));
                    command.Parameters.Clear();
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = ":id";
                    parameter.Value = reader.GetInt64(0);
                    command.Parameters.Add(parameter);
                    parameter = command.CreateParameter();
                    parameter.ParameterName = ":startedAt";
                    parameter.Value = dt.Ticks;
                    command.Parameters.Add(parameter);
                    dt = DateTime.FromBinary(reader.GetInt64(2));
                    parameter = command.CreateParameter();
                    parameter.ParameterName = ":finishedAt";
                    parameter.Value = dt.Ticks;
                    command.Parameters.Add(parameter);
                    command.ExecuteNonQuery();
                }
            }
        }

        protected override void Down()
        {
            var command = Connection.CreateCommand();
            command.CommandText = "SELECT id, achieved_at FROM user_achievement";
            using (var reader = command.ExecuteReader())
            {
                command = Connection.CreateCommand();
                command.CommandText = "UPDATE user_achievement SET achieved_at = :achievedAt WHERE id = :id";
                while (reader.Read())
                {
                    var dt = new DateTime(reader.GetInt64(1), DateTimeKind.Utc);
                    command.Parameters.Clear();
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = ":id";
                    parameter.Value = reader.GetInt64(0);
                    command.Parameters.Add(parameter);
                    parameter = command.CreateParameter();
                    parameter.ParameterName = ":achievedAt";
                    parameter.Value = dt.ToBinary();
                    command.Parameters.Add(parameter);
                    command.ExecuteNonQuery();
                }
            }

            command.CommandText = "SELECT id, started_at, finished_at FROM game";
            using (var reader = command.ExecuteReader())
            {
                command = Connection.CreateCommand();
                command.CommandText = "UPDATE game SET started_at = :startedAt, finished_at = :finishedAt WHERE id = :id";
                while (reader.Read())
                {
                    var dt = new DateTime(reader.GetInt64(1), DateTimeKind.Utc);
                    command.Parameters.Clear();
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = ":id";
                    parameter.Value = reader.GetInt64(0);
                    command.Parameters.Add(parameter);
                    parameter = command.CreateParameter();
                    parameter.ParameterName = ":startedAt";
                    parameter.Value = dt.ToBinary();
                    command.Parameters.Add(parameter);
                    dt = new DateTime(reader.GetInt64(2), DateTimeKind.Utc);
                    parameter = command.CreateParameter();
                    parameter.ParameterName = ":finishedAt";
                    parameter.Value = dt.ToBinary();
                    command.Parameters.Add(parameter);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
