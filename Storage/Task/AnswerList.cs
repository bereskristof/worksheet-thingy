using System.Collections.ObjectModel;

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
            Add(Answer.Load(question, reader.GetInt64(0), Encryption.DecryptBase64(reader.GetString(1)), reader.GetBoolean(2)));
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