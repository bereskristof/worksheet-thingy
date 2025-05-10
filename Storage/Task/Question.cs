namespace Storage.Task;

public class Question
{
    private readonly int _rowId;
    public string Text { get; set; } = string.Empty;
    public string Id => _rowId.ToString();
    public string AnswerCount => 0.ToString();

    public static Question NewFromListQuery(int rowId, string text)
    {
        var question = new Question(rowId)
        {
            Text = text
        };
        return question;
    }

    public static Question New()
    {
        var createCommand = Manager.Connection.CreateCommand();
        createCommand.CommandText = """
                                    INSERT INTO Questions (Question) VALUES ('');
                                    SELECT last_insert_rowid();
                                    """;
        int rowId = Convert.ToInt32(createCommand.ExecuteScalar());
        Console.WriteLine($"New question id is {rowId}");
        return new Question(rowId);
    }

    private Question(int rowId)
    {
        _rowId = rowId;
    }

    public void Store()
    {
        var updateCommand = Manager.Connection.CreateCommand();
        updateCommand.CommandText = $"UPDATE Questions SET Question = @Question WHERE rowid = @rowId";
        string encryptedText = Encryption.EncryptBase64(Text);
        updateCommand.Parameters.AddWithValue("@Question", encryptedText);
        updateCommand.Parameters.AddWithValue("@rowId", _rowId);
        updateCommand.ExecuteNonQuery();
    }
}