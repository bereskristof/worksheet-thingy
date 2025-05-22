namespace Storage.Task;

public class Answer
{
    private readonly int _rowId;
    
    public int Id => _rowId;
    
    public string Text { get; set; }
    
    private readonly Question _question;

    internal Answer(int rowId, Question question, string text)
    {
        _rowId = rowId;
        _question = question;
        Text = text;
    }

    public void Store()
    {
        var updateCommand = Manager.Connection.CreateCommand();
        updateCommand.CommandText = $"UPDATE Answers SET Answer = @Answer, QuestionId = @QuestionId WHERE rowid = @rowId";
        string encryptedText = Encryption.EncryptBase64(Text);
        updateCommand.Parameters.AddWithValue("@Answer", encryptedText);
        updateCommand.Parameters.AddWithValue("@QuestionId", _question);
        updateCommand.ExecuteNonQuery();
        Log.Write($"Answer {_rowId} stored");
    }
}