namespace Storage.Task;

public class Question
{
    private readonly int _rowId;
    public string Text { get; set; } = string.Empty;
    public int Id => _rowId;
    public string AnswerCount => 0.ToString();

    public static Question NewFromListQuery(int rowId, string text)
    {
        var question = new Question(rowId)
        {
            Text = Encryption.DecryptBase64(text)
        };
        return question;
    }

    public static List<Question> LoadAll()
    {
        var result = new List<Question>();
        var queryCommand = Manager.Connection.CreateCommand();
        queryCommand.CommandText = "SELECT rowid, Question FROM Questions ORDER BY rowid ASC;";
        var reader = queryCommand.ExecuteReader();
        while (reader.Read())
        {
            result.Add(NewFromListQuery(reader.GetInt32(0), reader.GetString(1)));
        }
        return result;
    }

    public static Question New()
    {
        var createCommand = Manager.Connection.CreateCommand();
        createCommand.CommandText = """
                                    INSERT INTO Questions (Question) VALUES ('');
                                    SELECT last_insert_rowid();
                                    """;
        int rowId = Convert.ToInt32(createCommand.ExecuteScalar());
        return new Question(rowId);
    }

    public List<Answer> LoadAllAnswers()
    {
        var result = new List<Answer>();
        var queryCommand = Manager.Connection.CreateCommand();
        queryCommand.CommandText = "SELECT rowid, Answer FROM Answers WHERE rowid = @rowId;";
        queryCommand.Parameters.AddWithValue("@rowId", _rowId);
        var reader = queryCommand.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Answer(reader.GetInt32(0), this, reader.GetString(1)));
        }
        return result;
    }

    public Answer AddAnswer()
    {
        var createCommand = Manager.Connection.CreateCommand();
        createCommand.CommandText = """
                                    INSERT INTO Answers (QuestionId, Answer) VALUES (@questionId, '');
                                    SELECT last_insert_rowid();
                                    """;
        createCommand.Parameters.AddWithValue("@questionId", _rowId);
        Console.WriteLine(_rowId);
        int rowId = Convert.ToInt32(createCommand.ExecuteScalar());
        return new Answer(rowId, this, "");
    }

    private Question(int rowId)
    {
        _rowId = rowId;
    }

    public void Store()
    {
        var updateCommand = Manager.Connection.CreateCommand();
        updateCommand.CommandText = $"UPDATE Questions SET Question = @Question WHERE Id = @rowId";
        string encryptedText = Encryption.EncryptBase64(Text);
        updateCommand.Parameters.AddWithValue("@Question", encryptedText);
        updateCommand.Parameters.AddWithValue("@rowId", _rowId);
        updateCommand.ExecuteNonQuery();
        Log.Write($"Question {_rowId} stored");
    }
}