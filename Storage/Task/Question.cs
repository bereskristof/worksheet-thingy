using System.ComponentModel;

namespace Storage.Task;

public class Question : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    private string _text = string.Empty;
    
    private AnswerList _answers = [];
    
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
    
    public int Points { get; set; }

    public AnswerList Answers
    {
        get
        {
            if (_answers.Count != 0) return _answers;
            _answers.LoadAll(this);
            return _answers;
        }
        private set => _answers = value;
    }
    
    // public Image Image { get; private set; }

    private Question(long id, string text, int points)
    {
        Id = id;
        Text = text;
        Points = points;
    }

    internal static Question Load(long id, string text, int points)
    {
        var question = new Question(id, Encryption.DecryptBase64(text), points);
        return question;
    }

    internal static Question New()
    {
        var createCommand = Manager.Connection.CreateCommand();
        createCommand.CommandText = "INSERT INTO Questions (Id, Points, Question) VALUES (@Id, 0, '');";
        var newId = IdManager.GetQuestionId();
        createCommand.Parameters.AddWithValue("@Id", newId);
        createCommand.ExecuteScalar();
        return new Question(newId, "", 0);
    }

    public void Store()
    {
        var updateCommand = Manager.Connection.CreateCommand();
        updateCommand.CommandText = $"UPDATE Questions SET Question = @Question, Points = @Points WHERE Id = @Id";
        string encryptedText = Encryption.EncryptBase64(Text);
        updateCommand.Parameters.AddWithValue("@Question", encryptedText);
        updateCommand.Parameters.AddWithValue("@Points", Points);
        updateCommand.Parameters.AddWithValue("@Id", Id);
        updateCommand.ExecuteNonQuery();
        foreach (var answer in Answers) answer.Store();
        Log.Write($"Question {Id} stored");
    }

    public void Delete()
    {
        var deleteCommand = Manager.Connection.CreateCommand();
        deleteCommand.CommandText = $"DELETE FROM Questions WHERE Id = @Id";
        deleteCommand.Parameters.AddWithValue("@Id", Id);
        deleteCommand.ExecuteNonQuery();
        Log.Write($"Question {Id} deleted");
    }
}