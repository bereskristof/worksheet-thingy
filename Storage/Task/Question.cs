using System.ComponentModel;

namespace Storage.Task;

public class Question : INotifyPropertyChanged
{
    private string _text = string.Empty;
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public long Id { get; }

    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
        }
    }

    private List<Answer> Answers { get; set; } = [];
    
    // public Image Image { get; private set; }

    private Question(long id, string text)
    {
        Id = id;
        Text = text;
    }

    internal static Question Load(long id, string text)
    {
        var question = new Question(id, Encryption.DecryptBase64(text));
        return question;
    }

    internal static Question New()
    {
        var createCommand = Manager.Connection.CreateCommand();
        createCommand.CommandText = "INSERT INTO Questions (Id, Question) VALUES (@Id, '');";
        var newId = IdManager.GetQuestionId();
        createCommand.Parameters.AddWithValue("@Id", newId);
        createCommand.ExecuteScalar();
        return new Question(newId, "");
    }

    public List<Answer> GetAnswers()
    {
        if (Answers.Count != 0) return Answers;
        
        var queryCommand = Manager.Connection.CreateCommand();
        queryCommand.CommandText = "SELECT Id, Answer FROM Answers WHERE Id = @Id;";
        queryCommand.Parameters.AddWithValue("@Id", Id);
        var reader = queryCommand.ExecuteReader();
        while (reader.Read())
        {
            Answers.Add(new Answer(reader.GetInt32(0), this, reader.GetString(1)));
        }
        return Answers;
    }

    public Answer AddAnswer()
    {
        var createCommand = Manager.Connection.CreateCommand();
        createCommand.CommandText = "INSERT INTO Answers (Id, QuestionId, Answer) VALUES (@Id, @questionId, '');";
        var newId = IdManager.GetAnswerId();
        createCommand.Parameters.AddWithValue("@Id", newId);
        createCommand.Parameters.AddWithValue("@questionId", Id);
        createCommand.ExecuteScalar();
        Log.Write($"Added new answer to {Id}");
        return new Answer((int)newId, this, ""); // TODO: Fix this being an int
    }

    public void Store()
    {
        var updateCommand = Manager.Connection.CreateCommand();
        updateCommand.CommandText = $"UPDATE Questions SET Question = @Question WHERE Id = @Id";
        string encryptedText = Encryption.EncryptBase64(Text);
        updateCommand.Parameters.AddWithValue("@Question", encryptedText);
        updateCommand.Parameters.AddWithValue("@Id", Id);
        updateCommand.ExecuteNonQuery();
        Log.Write($"Question {Id} stored");
    }
}