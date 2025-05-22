namespace Storage;

public static class IdManager
{
    private static long _questionId;
    private static long _answerId;
    
    public static void Init()
    {
        _questionId = GetMaxId("Questions");
        _answerId = GetMaxId("Answers");
    }

    /// WARNING: This specific function does NOT sanitize SQL input, since it should only be used by the developer!
    private static long GetMaxId(string table)
    {
        var questionIdCommand = Manager.Connection.CreateCommand();
        questionIdCommand.CommandText = $"SELECT MAX(Id) FROM {table};";
        object? queryResult = questionIdCommand.ExecuteScalar();
        string result = queryResult?.ToString() ?? "";
        return long.TryParse(result, out long id) ? id + 1 : 0;
    }

    public static long GetQuestionId() => _questionId++;

    public static long GetAnswerId() => _answerId++;
}