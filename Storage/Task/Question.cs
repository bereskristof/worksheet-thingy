using System.ComponentModel;
using System.Security.Cryptography;

namespace Storage.Task;

public class Question : INotifyPropertyChanged
{
    const int DefaultPoints = 1;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    private string _storedText; // The string stored in the database
    private string? _bufferedText; // The string currently being edited in the UI
    
    private int _storedPoints; // The points' amount stored in the database
    private int _bufferedPoints; // The points' amount currently being edited in the UI
    
    private bool _isConfirmedImageless; // Indicates if an image was confirmed to be absent for this question, used to prevent unnecessary refetches from the database
    
    private readonly AnswerList _answers = []; // The list of answers for this question, loaded lazily
    
    public long Id { get; }

    /// The text of the question, fetched and decrypted from the database as needed.
    public string Text
    {
        get => GetText();
        set => SetText(value);
    }

    /// The point amount for this question.
    public int Points
    {
        get => GetPoints(); 
        set => SetPoints(value);
    }
    
    /// Indicates whether the question text failed to decrypt.
    public bool FailedToDecrypt { get; private set; }

    /// The list of answers for this question, loaded lazily.
    public AnswerList Answers => GetAnswers();

    // private set => _answers = value;
    /// Gets the question text, fetches and decrypts it from the database if necessary.
    /// NOTE: Assumes the text is always correct after the first read.
    /// Essentially, `text` is only read once.
    private string GetText()
    {
        if (_bufferedText != null) return _bufferedText; // If the text is already buffered, return it directly
        var readCommand = Manager.Connection.CreateCommand();
        readCommand.CommandText = "SELECT Question FROM Questions WHERE Id = @Id";
        readCommand.Parameters.AddWithValue("@Id", Id);
        using var reader = readCommand.ExecuteReader();
        if (!reader.Read())
        {
            Log.Write($"GetText: Question {Id} not in database", Log.Severity.Error);
            return string.Empty;
        }
        string encryptedText = reader.GetString(0);
        bool corrupted = !Encryption.CanDecryptBase64(encryptedText, out string decryptedText);
        if (corrupted) Log.Write($"GetText: Question {Id} value is corrupted", Log.Severity.Error);
        FailedToDecrypt = corrupted;
        _storedText = decryptedText;
        _bufferedText = decryptedText;
        // PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
        Log.Write($"GetText: Question {Id} text fetched from database");
        return decryptedText;
    }

    /// Updates buffered `text` and stores' it in the database.
    private void SetText(string value)
    {
        _bufferedText = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
        if (_storedText == _bufferedText) { // No change, no need to update the database
            Log.Write($"SetText: Question {Id} text update skipped");
            return;
        }
        var updateCommand = Manager.Connection.CreateCommand();
        updateCommand.CommandText = "UPDATE Questions SET Question = @Question WHERE Id = @Id";
        string encryptedText = Encryption.EncryptBase64(value);
        updateCommand.Parameters.AddWithValue("@Question", encryptedText);
        updateCommand.Parameters.AddWithValue("@Id", Id);
        updateCommand.ExecuteNonQuery();
        _storedText = value;
        Log.Write($"SetText: Question {Id} text stored in the database");
    }

    /// Gets the points' amount, fetches it from the database if necessary.
    /// NOTE: Assumes points from construction are correct, period.
    private int GetPoints()
    {
        return _bufferedPoints; // If the text is already buffered, return it directly
    }

    /// Updates buffered `text` and stores' it in the database.
    private void SetPoints(int value)
    {
        _bufferedPoints = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Points)));
        if (_storedPoints == _bufferedPoints) { // No change, no need to update the database
            Log.Write($"SetPoints: Question {Id} points update skipped");
            return;
        }
        var updateCommand = Manager.Connection.CreateCommand();
        updateCommand.CommandText = "UPDATE Questions SET Points = @Points WHERE Id = @Id";
        updateCommand.Parameters.AddWithValue("@Points", value);
        updateCommand.Parameters.AddWithValue("@Id", Id);
        updateCommand.ExecuteNonQuery();
        _storedPoints = value;
        Log.Write($"SetPoints: Question {Id} points stored in the database");
    }
    
    private AnswerList GetAnswers()
    {
        if (_answers.Count != 0) return _answers;
        _answers.LoadAll(this);
        return _answers;
    }

    private Question(long id, string text, int points, bool failedToDecrypt)
    {
        Id = id;
        _bufferedText = text;
        _storedText = text;
        _bufferedPoints = points;
        _storedPoints = points;
        FailedToDecrypt = failedToDecrypt;
    }

    /// Used for batch loading, where `text` is already read, but not yet decrypted.
    internal static Question Load(long id, string text, int points)
    {
        bool corrupted = !Encryption.CanDecryptBase64(text, out string decryptedText);
        var question = new Question(id, decryptedText, points, corrupted);
        return question;
    }

    /// Creates a new empty question, used when adding a new question in the UI, immediately stores it in the database.
    internal static Question New()
    {
        var createCommand = Manager.Connection.CreateCommand();
        createCommand.CommandText = "INSERT INTO Questions (Id, Points, Question) VALUES (@Id, @Points, '');";
        var newId = IdManager.GetQuestionId();
        createCommand.Parameters.AddWithValue("@Id", newId);
        createCommand.Parameters.AddWithValue("@Points", DefaultPoints);
        createCommand.ExecuteScalar();
        return new Question(newId, string.Empty, DefaultPoints, false);
    }

    [Obsolete("Planning to rework this class to save data on it's own, without the need for a separate Store method.")]
    public void Store()
    {
        return;
        // var updateCommand = Manager.Connection.CreateCommand();
        // updateCommand.CommandText = $"UPDATE Questions SET Question = @Question, Points = @Points WHERE Id = @Id";
        // string encryptedText = Encryption.EncryptBase64(Text);
        // updateCommand.Parameters.AddWithValue("@Question", encryptedText);
        // updateCommand.Parameters.AddWithValue("@Points", Points);
        // updateCommand.Parameters.AddWithValue("@Id", Id);
        // updateCommand.ExecuteNonQuery();
        // foreach (var answer in Answers) answer.Store();
        // Log.Write($"Question {Id} stored");
        // FailedToDecrypt = false;
    }

    /// Removes the question from the database, deletes all answers and image associated with it.
    public void Delete()
    {
        DeleteImage();
        foreach (var answer in Answers) 
            answer.Delete();
        var deleteCommand = Manager.Connection.CreateCommand();
        deleteCommand.CommandText = $"DELETE FROM Questions WHERE Id = @Id";
        deleteCommand.Parameters.AddWithValue("@Id", Id);
        deleteCommand.ExecuteNonQuery();
        Log.Write($"Delete: Question {Id} deleted");
    }

    /// Fetches the image associated with this question from the database, decrypts it and returns a MemoryStream.
    public MemoryStream? FetchImageStream()
    {
        if (_isConfirmedImageless) return null;
        var fetchCommand = Manager.Connection.CreateCommand();
        fetchCommand.CommandText = $"SELECT Image FROM Images WHERE QuestionId = @QuestionId";
        fetchCommand.Parameters.AddWithValue("@QuestionId", Id);
        using var reader = fetchCommand.ExecuteReader();
        if (!reader.Read())
        {
            Log.Write($"FetchImageStream: No image found for question {Id}");
            _isConfirmedImageless = true;
            return null;
        }
        using var dataStream = reader.GetStream(0);
        var memoryStream = new MemoryStream();
        dataStream.CopyTo(memoryStream);
        var decryptedData = Encryption.DecryptBlob(memoryStream.ToArray());
        var cleanMemoryStream = new MemoryStream(decryptedData);
        return cleanMemoryStream;
    }

    /// Stores an image from a file path into the database, encrypts it and associates it with this question.
    public void StoreImageFromPath(string path)
    {
        byte[] data;
        try
        {
            data = File.ReadAllBytes(path);
        }
        catch
        {
            Log.Write($"StoreImageFromPath: Failed to add image from path to question {Id}", Log.Severity.Error);
            return;
        }
        byte[] encryptedData = Encryption.EncryptBlob(data);
        var storeCommand = Manager.Connection.CreateCommand();
        storeCommand.CommandText = "INSERT INTO Images (QuestionId, Image) VALUES (@QuestionId, @Image)";
        storeCommand.Parameters.AddWithValue("@QuestionId", Id);
        storeCommand.Parameters.AddWithValue("@Image", encryptedData);
        DeleteImage();
        storeCommand.ExecuteNonQuery();
        Log.Write($"StoreImageFromPath: Image added to question {Id}");
    }
    
    /// Deletes the image associated with this question from the database.
    public void DeleteImage()
    {
        var deleteCommand = Manager.Connection.CreateCommand();
        deleteCommand.CommandText = "DELETE FROM Images WHERE QuestionId = @QuestionId";
        deleteCommand.Parameters.AddWithValue("@QuestionId", Id);
        deleteCommand.ExecuteNonQuery();
        Log.Write($"DeleteImage: Image for question {Id} deleted");
    }
}