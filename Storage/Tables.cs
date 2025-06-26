using Microsoft.Data.Sqlite;

namespace Storage;

public static class Tables
{
    internal static void Init(SqliteConnection connection)
    {
        var securityTableCommand = connection.CreateCommand();
        securityTableCommand.CommandText = """
                                           CREATE TABLE IF NOT EXISTS Security (
                                               Key TEXT PRIMARY KEY,
                                               Value BLOB NOT NULL
                                           );
                                           """;
        securityTableCommand.ExecuteNonQuery();

        var questionsTableCommand = connection.CreateCommand();
        questionsTableCommand.CommandText = """
                                            CREATE TABLE IF NOT EXISTS Questions (
                                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                Points INTEGER NOT NULL,
                                                Question TEXT
                                            );
                                            """;
        questionsTableCommand.ExecuteNonQuery();
        
        var answersTableCommand = connection.CreateCommand();
        answersTableCommand.CommandText = """
                                          CREATE TABLE IF NOT EXISTS Answers (
                                              Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                              QuestionId INTEGER,
                                              Correct INTEGER,
                                              Answer TEXT,
                                              FOREIGN KEY (QuestionId) REFERENCES Questions(Id)
                                          );
                                          """;
        answersTableCommand.ExecuteNonQuery();
        
        var imagesTableCommand = connection.CreateCommand();
        imagesTableCommand.CommandText = """
                                         CREATE TABLE IF NOT EXISTS Images (
                                             Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                             QuestionId INTEGER,
                                             Image BLOB,
                                             FOREIGN KEY (QuestionId) REFERENCES Questions(Id)
                                         );
                                         """;
        imagesTableCommand.ExecuteNonQuery();
    }
}