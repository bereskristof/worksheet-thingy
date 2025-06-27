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

    public MemoryStream? FetchImageStream()
    {
        var fetchCommand = Manager.Connection.CreateCommand();
        fetchCommand.CommandText = $"SELECT Image FROM Images WHERE QuestionId = @QuestionId";
        fetchCommand.Parameters.AddWithValue("@QuestionId", Id);
        using var reader = fetchCommand.ExecuteReader();
        if (!reader.Read()) return null;
        using var dataStream = reader.GetStream(0);
        var memoryStream = new MemoryStream();
        dataStream.CopyTo(memoryStream);
        var decryptedData = Encryption.DecryptBlob(memoryStream.ToArray());
        var cleanMemoryStream = new MemoryStream(decryptedData);
        return cleanMemoryStream;
    }

    public void StoreImageFromPath(string path)
    {
        byte[] data;
        try
        {
            data = File.ReadAllBytes(path);
        }
        catch
        {
            Log.Write($"Failed to add image from path: {path}");
            return;
        }
        byte[] encryptedData = Encryption.EncryptBlob(data);
        var storeCommand = Manager.Connection.CreateCommand();
        storeCommand.CommandText = "INSERT INTO Images (QuestionId, Image) VALUES (@QuestionId, @Image)";
        storeCommand.Parameters.AddWithValue("@QuestionId", Id);
        storeCommand.Parameters.AddWithValue("@Image", encryptedData);
        DeleteImage();
        storeCommand.ExecuteNonQuery();
        Log.Write($"Image from path {path} added to question {Id}");
    }
    
    public void DeleteImage()
    {
        var deleteCommand = Manager.Connection.CreateCommand();
        deleteCommand.CommandText = "DELETE FROM Images WHERE QuestionId = @QuestionId";
        deleteCommand.Parameters.AddWithValue("@QuestionId", Id);
        deleteCommand.ExecuteNonQuery();
        Log.Write($"Image for question {Id} deleted");
    }
}