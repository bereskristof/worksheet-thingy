using System.Collections.ObjectModel;
using System.Security.Cryptography;

namespace Storage.Task;

public class AnswerList : ObservableCollection<Answer>
{
    internal void LoadAll(Question question)
    {
        var queryCommand = Manager.Connection.CreateCommand();
        queryCommand.CommandText = "SELECT Id, Answer, Correct FROM Answers WHERE QuestionId = @questionId;";
        queryCommand.Parameters.AddWithValue("@questionId", question.Id);
        var reader = queryCommand.ExecuteReader();
        while (reader.Read())
        {
            GetEncrypted(reader.GetString(1), out string text, out var corrupted);
            var answer = Answer.Load(question, reader.GetInt64(0), text, reader.GetBoolean(2), corrupted);
            Add(answer);
        }
    }

    private void GetEncrypted(string cipherText, out string decryptedText, out bool corrupted)
    {
        corrupted = false;
        try
        {
            decryptedText = Encryption.DecryptBase64(cipherText);
        }
        catch (Exception e) when (e is CryptographicException or FormatException)
        {
            decryptedText = "[DAMAGED] " + cipherText;
            corrupted = true;
        }
    }
    public Answer Add(Question question)
    {
        var answer = Answer.New(question);
        base.Add(answer);
        return answer;
    }

    public new void Remove(Answer answer)
    {
        base.Remove(answer);
        answer.Delete();
    }
}