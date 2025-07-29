using System;
using System.Threading;
using Npgsql;
using StackExchange.Redis;
using System.Text.Json;

public class Worker
{
    public static void Main(string[] args)
    {
        var pgHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "db";
        var pgUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
        var pgPass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";
        var pgDb   = Environment.GetEnvironmentVariable("POSTGRES_DB")   ?? "postgres";
        var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "redis";

        var pgConnectionString = $"Host={pgHost};Username={pgUser};Password={pgPass};Database={pgDb}";
        
        ConnectionMultiplexer redis = null;
        NpgsqlConnection db = null;
        IDatabase database = null;

        while (redis == null)
        {
            try
            {
                redis = ConnectionMultiplexer.Connect(redisHost);
                database = redis.GetDatabase();
                Console.WriteLine("Connected to Redis");
            }
            catch (RedisConnectionException ex)
            {
                Console.WriteLine($"Redis not ready yet ({ex.Message}), retrying…");
                Thread.Sleep(2000);
            }
        }

        while (true)
        {
            if (db == null || db.State != System.Data.ConnectionState.Open)
            {
                if (db != null)
                {
                    db.Dispose();
                    db = null;
                }
                
                while (db == null || db.State != System.Data.ConnectionState.Open)
                {
                    try
                    {
                        db = new NpgsqlConnection(pgConnectionString);
                        db.Open();
                        Console.WriteLine("Connected to PostgreSQL");
                    }
                    catch (NpgsqlException ex)
                    {
                        Console.WriteLine($"Postgres not ready or connection failed ({ex.Message}), retrying…");
                        Thread.Sleep(2000);
                    }
                }
            }

            try
            {
                var voteJson = database.ListLeftPop("votes");
                if (voteJson.HasValue)
                {
                    try
                    {
                        var voteData = JsonSerializer.Deserialize<Vote>(voteJson.ToString().Replace("'", "\""));
                        Console.WriteLine($"Processing vote for '{voteData.vote}'");

                        using var command = new NpgsqlCommand("INSERT INTO votes (id, vote) VALUES (@id, @vote)", db);
                        command.Parameters.AddWithValue("@id", voteData.voter_id);
                        command.Parameters.AddWithValue("@vote", voteData.vote);
                        command.ExecuteNonQuery();
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Error deserializing JSON: {ex.Message}. Payload: {voteJson}");
                    }
                    catch (NpgsqlException ex)
                    {
                        Console.WriteLine($"Database error during query: {ex.Message}");
                        db.Close();
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
            catch (RedisConnectionException ex)
            {
                Console.WriteLine($"Redis connection lost: {ex.Message}, attempting to reconnect...");
                redis?.Dispose();
                redis = null;
                database = null;

                while (redis == null)
                {
                    try
                    {
                        redis = ConnectionMultiplexer.Connect(redisHost);
                        database = redis.GetDatabase();
                        Console.WriteLine("Reconnected to Redis");
                    }
                    catch (RedisConnectionException rex)
                    {
                        Console.WriteLine($"Redis re-connection failed ({rex.Message}), retrying...");
                        Thread.Sleep(2000);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                Thread.Sleep(1000);
            }
        }
    }
}

public class Vote
{
    public string voter_id { get; set; }
    public string vote { get; set; }
}
