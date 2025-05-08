using Microsoft.Data.Sqlite;

namespace Storage;

public static class Manager
{
    private static SqliteConnection? _connection;

    public static SqliteConnection Connection
    {
        get => _connection ?? throw new NullReferenceException("Access is invalid, database connection is null"); //TODO: Log
        private set => _connection = value;
    }
    
    public static void CreateDatabase(string filename)
    {
        var connectionString = $"Data Source={filename}";
        Connection = new SqliteConnection(connectionString);
        Connection.Open();

        Tables.Init(Connection);
        Encryption.GetKeyAndIv();
        Log.Write("Opened database");
    }

    public static void UpdateTextRecord(string table, string id, string column, string? value)
    {
        var editCommand = Connection.CreateCommand();
        editCommand.CommandText = "UPDATE @Table SET @Value = '@Column' WHERE rowid = @Id;";
        editCommand.Parameters.AddWithValue("@Table", table);
        editCommand.Parameters.AddWithValue("@Id", id);
        editCommand.Parameters.AddWithValue("@Column", column);
        editCommand.Parameters.AddWithValue("@Value", value);
        editCommand.ExecuteNonQuery();
    }

    public static void CloseDatabase()
    {
        Connection.Close();
        Log.Write("Closed database");
    }
}
