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

    public void Store()
    {
        var updateCommand = Manager.Connection.CreateCommand();
        updateCommand.CommandText = $"UPDATE Questions SET Question = @Question WHERE Id = @Id";
        string encryptedText = Encryption.EncryptBase64(Text);
        updateCommand.Parameters.AddWithValue("@Question", encryptedText);
        updateCommand.Parameters.AddWithValue("@Id", Id);
        updateCommand.ExecuteNonQuery();
        foreach (var answer in Answers) answer.Store();
        Log.Write($"Question {Id} stored");
    }
}