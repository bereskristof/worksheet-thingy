namespace Storage.Task;

public class Answer
{
    public long Id { get; }
    
    public string Text { get; set; }

    public bool Correct { get; set; }
    
    private readonly Question _question;
    
    public bool FailedToDecrypt { get; private set; }

    internal static Answer Load(Question question, long id, string text, bool correct, bool corrupted) => new(question, id, text, correct, corrupted);

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

    private Answer(Question question, long id, string text, bool correct, bool corrupted)
    {
        _question = question;
        Id = id;
        Text = text;
        Correct = correct;
        FailedToDecrypt = corrupted;
    }

    public void Store()
    {
        var updateCommand = Manager.Connection.CreateCommand();
        updateCommand.CommandText = "UPDATE Answers SET Answer = @Answer, QuestionId = @QuestionId, Correct = @Correct WHERE Id = @Id";
        string encryptedText = Encryption.EncryptBase64(Text);
        updateCommand.Parameters.AddWithValue("@Answer", encryptedText);
        updateCommand.Parameters.AddWithValue("@QuestionId", _question.Id);
        updateCommand.Parameters.AddWithValue("@Correct", Correct);
        updateCommand.Parameters.AddWithValue("@Id", Id);
        updateCommand.ExecuteNonQuery();
        Log.Write($"Answer {Id} stored");
        FailedToDecrypt = false;
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