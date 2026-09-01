using Microsoft.Data.Sqlite;
using Dapper;
using System.IO;

namespace CCSwitch.Wpf.Services;

public class DatabaseService
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public DatabaseService()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".cc-switch"
        );
        Directory.CreateDirectory(appData);
        _dbPath = Path.Combine(appData, "cc-switch.db");
        _connectionString = $"Data Source={_dbPath};Mode=ReadWriteCreate;";
    }

    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        // Enable WAL mode for high concurrency
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode = WAL; PRAGMA synchronous = NORMAL;";
        cmd.ExecuteNonQuery();

        return connection;
    }

    public void Initialize()
    {
        using var conn = CreateConnection();
        
        // Initialize core tables
        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS providers (
                id TEXT PRIMARY KEY,
                app_type TEXT NOT NULL,
                name TEXT NOT NULL,
                config_json TEXT NOT NULL,
                is_active INTEGER DEFAULT 0,
                sort_index INTEGER DEFAULT 0,
                created_at INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS usage_records (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                app_type TEXT NOT NULL,
                provider_id TEXT NOT NULL,
                model TEXT NOT NULL,
                input_tokens INTEGER DEFAULT 0,
                output_tokens INTEGER DEFAULT 0,
                cost REAL DEFAULT 0.0,
                timestamp INTEGER NOT NULL
            );
        ");
    }
}
