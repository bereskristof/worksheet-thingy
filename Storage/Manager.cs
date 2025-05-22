using Microsoft.Data.Sqlite;

namespace Storage;

public static class Manager
{
    private static SqliteConnection? _connection;

    public static SqliteConnection Connection
    {
        get
        {
            if (_connection != null) return _connection;
            Log.Write("Null reference in Manager.Connection.get", Log.Severity.Error);
            throw new NullReferenceException("Access is invalid, database connection is null");
        }
        private set => _connection = value;
    }

    public static void OpenDatabase(string filename)
    {
        var connectionString = $"Data Source={filename}";
        Connection = new SqliteConnection(connectionString);
        Connection.Open();

        Tables.Init(Connection);
        IdManager.Init();
        Log.Write("Opened database");
    }

    public static void CloseDatabase()
    {
        Connection.Close();
        Log.Write("Closed database");
    }
}
