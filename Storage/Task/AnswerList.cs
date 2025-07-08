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
            var answer = Answer.Load(question, reader.GetInt64(0), reader.GetString(1), reader.GetBoolean(2));
            Add(answer);
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

    public Answer[] GetRandomAnswers(int count)
    {
        var correctAnswers = this.Where(e => e.Correct).ToArray();
        var incorrectAnswers = this.Where(e => !e.Correct)
            .OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue))
            .Take(count - correctAnswers.Length); // Take() never takes more than available
        var selectedAnswers = correctAnswers.Concat(incorrectAnswers).ToArray();
        RandomNumberGenerator.Shuffle<Answer>(selectedAnswers);
        return selectedAnswers;
    }
}