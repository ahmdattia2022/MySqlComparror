using System;
using System.Collections.Generic;
using System.IO;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

public class DbConfig
{
    public string Host { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string Port { get; set; } = "3306"; // Default port
    public string SslMode { get; set; } = "none"; // Default SSL mode
    public string Charset { get; set; } = "utf8"; // Default charset
}

class Program
{
    static void Main()
    {
        var config = LoadConfig("dbconfig.json");
        if (config == null)
        {
            Console.WriteLine("Failed to load configuration.");
            return;
        }

        var localTables = GetTablesFromDb(config.LocalConfig);
        var remoteTables = GetTablesFromDb(config.RemoteConfig);

        var tablesOnlyInRemote = new HashSet<string>(remoteTables);
        tablesOnlyInRemote.ExceptWith(localTables);

        Console.WriteLine("Tables in remote database but not in local database:");
        foreach (var table in tablesOnlyInRemote)
        {
            Console.WriteLine(table);
        }
    }

    static List<string> GetTablesFromDb(DbConfig config)
    {
        var tables = new List<string>();
        string connStr = $"server={config.Host};Port={config.Port};Database={config.Database};Uid={config.User};Pwd={config.Password};SslMode={config.SslMode};charset={config.Charset};" ;

        using (var conn = new MySqlConnection(connStr))
        {
            try
            {
                conn.Open();
                string query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @Database";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Database", config.Database);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tables.Add(reader.GetString(0));
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        return tables;
    }

    static (DbConfig LocalConfig, DbConfig RemoteConfig)? LoadConfig(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var config = JsonConvert.DeserializeObject<Config>(json);
            return (config.LocalConfig, config.RemoteConfig);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load config: {ex.Message}");
            return null;
        }
    }
}

public class Config
{
    public DbConfig LocalConfig { get; set; }
    public DbConfig RemoteConfig { get; set; }
}