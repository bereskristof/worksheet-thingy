namespace Storage.Task;

public class Answer
{
    private string _storedText; // The string stored in the database
    private string? _bufferedText; // The string currently being edited in the UI
    
    private bool _storedCorrect;
    private bool _bufferedCorrect;
    
    public long Id { get; }

    public string Text
    {
        get => GetText(); 
        set => SetText(value);
    }

    public bool Correct
    {
        get => GetCorrect(); 
        set => SetCorrect(value);
    }
    
    private readonly Question _question;
    
    public bool FailedToDecrypt { get; private set; }
    
    private string GetText()
    {
        if (_bufferedText != null) return _bufferedText; // If the text is already buffered, return it directly
        var readCommand = Manager.Connection.CreateCommand();
        readCommand.CommandText = "SELECT Answer FROM Answers WHERE Id = @Id";
        readCommand.Parameters.AddWithValue("@Id", Id);
        using var reader = readCommand.ExecuteReader();
        if (!reader.Read())
        {
            Log.Write($"GetText: Answer {Id} not in database", Log.Severity.Error);
            return string.Empty;
        }
        string encryptedText = reader.GetString(0);
        bool corrupted = !Encryption.CanDecryptBase64(encryptedText, out string decryptedText);
        if (corrupted) Log.Write($"GetText: Answer {Id} value is corrupted", Log.Severity.Error);
        FailedToDecrypt = corrupted;
        _storedText = decryptedText;
        _bufferedText = decryptedText;
        // PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
        Log.Write($"GetText: Answer {Id} text fetched from database");
        return decryptedText;
    }

    private void SetText(string value)
    {
        _bufferedText = value;
        if (_storedText == _bufferedText) { // No change, no need to update the database
            Log.Write($"SetText: Question {Id} text update skipped");
            return;
        }
        var updateCommand = Manager.Connection.CreateCommand();
        updateCommand.CommandText = "UPDATE Answers SET Answer = @Answer WHERE Id = @Id";
        string encryptedText = Encryption.EncryptBase64(value);
        updateCommand.Parameters.AddWithValue("@Answer", encryptedText);
        updateCommand.Parameters.AddWithValue("@Id", Id);
        updateCommand.ExecuteNonQuery();
        _storedText = value;
        Log.Write($"SetText: Answer {Id} text stored in the database");
    }
    
    private bool GetCorrect()
    {
        return _bufferedCorrect;
    }

    private void SetCorrect(bool value)
    {
        _bufferedCorrect = value;
        if (_storedCorrect == _bufferedCorrect) { // No change, no need to update the database
            Log.Write($"SetPoints: Question {Id} points update skipped");
            return;
        }
        var updateCommand = Manager.Connection.CreateCommand();
        updateCommand.CommandText = "UPDATE Answers SET Correct = @Correct WHERE Id = @Id";
        updateCommand.Parameters.AddWithValue("@Correct", value);
        updateCommand.Parameters.AddWithValue("@Id", Id);
        updateCommand.ExecuteNonQuery();
        _storedCorrect = value;
        Log.Write($"SetPoints: Answer {Id} correctness updated in the database");
    }

    private Answer(Question question, long id, string text, bool correct, bool corrupted)
    {
        Id = id;
        _storedText = text;
        _bufferedText = text;
        _bufferedCorrect = correct;
        _storedCorrect = correct;
        _question = question;
        FailedToDecrypt = corrupted;
    }

    /// Used for batch loading, where `text` is already read, but not yet decrypted.
    internal static Answer Load(Question question, long id, string text, bool correct)
    {
        bool corrupted = !Encryption.CanDecryptBase64(text, out string decryptedText);
        var answer = new Answer(question, id, decryptedText, correct, corrupted);
        return answer;
    }

    /// Creates a new empty answer.
    internal static Answer New(Question question)
    {
        var createCommand = Manager.Connection.CreateCommand();
        createCommand.CommandText = "INSERT INTO Answers (Id, QuestionId, Answer, Correct) VALUES (@Id, @QuestionId, '', 0);";
        var newId = IdManager.GetAnswerId();
        createCommand.Parameters.AddWithValue("Id", newId);
        createCommand.Parameters.AddWithValue("@QuestionId", question.Id);
        createCommand.ExecuteScalar();
        return new Answer(question, newId, "", false, false);
    }

    [Obsolete]
    public void Store()
    {
    //     var updateCommand = Manager.Connection.CreateCommand();
    //     updateCommand.CommandText = "UPDATE Answers SET Answer = @Answer, QuestionId = @QuestionId, Correct = @Correct WHERE Id = @Id";
    //     string encryptedText = Encryption.EncryptBase64(Text);
    //     updateCommand.Parameters.AddWithValue("@Answer", encryptedText);
    //     updateCommand.Parameters.AddWithValue("@QuestionId", _question.Id);
    //     updateCommand.Parameters.AddWithValue("@Correct", Correct);
    //     updateCommand.Parameters.AddWithValue("@Id", Id);
    //     updateCommand.ExecuteNonQuery();
    //     Log.Write($"Answer {Id} stored");
    //     FailedToDecrypt = false;
    }

    public void Delete()
    {
        var deleteCommand = Manager.Connection.CreateCommand();
        deleteCommand.CommandText = "DELETE FROM Answers WHERE Id = @Id";
        deleteCommand.Parameters.AddWithValue("@Id", Id);
        deleteCommand.ExecuteNonQuery();
        Log.Write($"Answer {Id} deleted");
    }
}