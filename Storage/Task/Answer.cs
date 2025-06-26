using System.ComponentModel;

namespace Storage.Task;

public class Answer : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public long Id { get; }
    
    public string Text { get; set; }
    
    private readonly Question _question;

    internal static Answer Load(Question question, long id, string text) => new(question, id, text);

    internal static Answer New(Question question)
    {
        var createCommand = Manager.Connection.CreateCommand();
        createCommand.CommandText = "INSERT INTO Answers (Id, QuestionId, Answer) VALUES (@Id, @QuestionId, '');";
        var newId = IdManager.GetAnswerId();
        createCommand.Parameters.AddWithValue("Id", newId);
        createCommand.Parameters.AddWithValue("@QuestionId", question.Id);
        createCommand.ExecuteScalar();
        return new Answer(question, newId, "");
    }

    private Answer(Question question, long id, string text)
    {
        _question = question;
        Id = id;
        Text = text;
    }

    public void Store()
    {
        var updateCommand = Manager.Connection.CreateCommand();
        updateCommand.CommandText = "UPDATE Answers SET Answer = @Answer, QuestionId = @QuestionId WHERE Id = @Id";
        string encryptedText = Encryption.EncryptBase64(Text);
        updateCommand.Parameters.AddWithValue("@Answer", encryptedText);
        updateCommand.Parameters.AddWithValue("@QuestionId", _question.Id);
        updateCommand.Parameters.AddWithValue("@Id", Id);
        updateCommand.ExecuteNonQuery();
        Log.Write($"Answer {Id} stored");
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